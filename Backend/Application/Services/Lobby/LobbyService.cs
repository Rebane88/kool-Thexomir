using Application.Contracts;
using Application.Services.Lobby.DTOs;
using Base.Contracts;
using Domain.Game;
using Microsoft.Extensions.Logging;

namespace Application.Services.Lobby;

public class LobbyService(IUnitOfWork unitOfWork, IIdentityService identityService, ILogger<LobbyService> logger) : ILobbyService
{
    private static readonly char[] Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    // -------------------------------------------------------------------------
    // Create
    // -------------------------------------------------------------------------

    public async Task<Result<CreateLobbyResponse>> CreateLobbyAsync(Guid userId, CreateLobbyRequest request)
    {
        if (request.MaxPlayers < 2 || request.MaxPlayers > 4)
            return Result<CreateLobbyResponse>.Fail("MaxPlayers must be between 2 and 4.");

        var code = await GenerateUniqueLobbyCodeAsync();

        var game = new Game
        {
            Status = EGameStatus.Lobby,
            MaxPlayers = request.MaxPlayers,
            WinCondition = request.WinCondition,
            LobbyCode = code,
            HostUserId = userId,
            MaxRounds = request.MaxTurnCount ?? 100,
            TurnTimeLimit = 20
        };
        await unitOfWork.Games.AddAsync(game);

        var kingdom = new Kingdom
        {
            GameId = game.Id,
            AppUserId = userId,
            Name = string.Empty
        };
        await unitOfWork.Kingdoms.AddAsync(kingdom);

        await unitOfWork.CommitAsync();

        return Result<CreateLobbyResponse>.Ok(new CreateLobbyResponse
        {
            LobbyId = game.Id,
            InviteCode = code
        });
    }

    // -------------------------------------------------------------------------
    // Join
    // -------------------------------------------------------------------------

    public async Task<Result<LobbyResponse>> JoinLobbyAsync(Guid userId, JoinLobbyRequest request)
    {
        var normalizedCode = request.InviteCode.Trim().ToUpperInvariant();

        var gameByCode = await unitOfWork.Games.GetByLobbyCodeAsync(normalizedCode);
        if (gameByCode is null)
            return Result<LobbyResponse>.Fail("Lobby not found.");

        var game = await unitOfWork.Games.GetByIdWithLockAsync(gameByCode.Id);
        if (game is null)
            return Result<LobbyResponse>.Fail("Lobby not found.");

        if (game.Status != EGameStatus.Lobby)
            return Result<LobbyResponse>.Fail("Lobby is no longer accepting players.");

        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(game.Id);

        if (kingdoms.Any(k => k.AppUserId == userId))
            return Result<LobbyResponse>.Fail("You are already in this lobby.");

        if (kingdoms.Count >= game.MaxPlayers)
            return Result<LobbyResponse>.Fail("Lobby is full.");

        var newKingdom = new Kingdom
        {
            GameId = game.Id,
            AppUserId = userId,
            Name = string.Empty
        };
        await unitOfWork.Kingdoms.AddAsync(newKingdom);

        try
        {
            await unitOfWork.CommitAsync();
        }
        catch (ConcurrencyException)
        {
            return Result<LobbyResponse>.Fail("Lobby was updated concurrently. Please try again.");
        }

        var updatedGame = await unitOfWork.Games.GetGameWithKingdomsAsync(game.Id);
        return Result<LobbyResponse>.Ok(await BuildLobbyResponseAsync(updatedGame!));
    }

    // -------------------------------------------------------------------------
    // Leave
    // -------------------------------------------------------------------------

    public async Task<Result<bool>> LeaveLobbyAsync(Guid userId, Guid lobbyId)
    {
        var game = await unitOfWork.Games.GetByIdWithLockAsync(lobbyId);
        if (game is null)
            return Result<bool>.Fail("Lobby not found.");

        if (game.Status != EGameStatus.Lobby)
            return Result<bool>.Fail("Cannot leave a game that has already started.");

        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(game.Id);

        var kingdom = kingdoms.FirstOrDefault(k => k.AppUserId == userId);
        if (kingdom is null)
            return Result<bool>.Fail("You are not in this lobby.");

        await unitOfWork.Kingdoms.DeleteAsync(kingdom.Id);

        var remaining = kingdoms.Where(k => k.AppUserId != userId).ToList();
        if (remaining.Count == 0)
        {
            game.Status = EGameStatus.Completed;
        }
        else if (game.HostUserId == userId)
        {
            var newHost = remaining.OrderBy(k => k.CreatedAt).ThenBy(k => k.Id).First();
            game.HostUserId = newHost.AppUserId;
        }

        await unitOfWork.Games.UpdateAsync(game);
        await unitOfWork.CommitAsync();

        return Result<bool>.Ok(true);
    }

    // -------------------------------------------------------------------------
    // Select Faction
    // -------------------------------------------------------------------------

    public async Task<Result<bool>> SelectFactionAsync(Guid userId, Guid lobbyId, Guid factionTypeId)
    {
        var game = await unitOfWork.Games.GetByIdWithLockAsync(lobbyId);
        if (game is null)
            return Result<bool>.Fail("Lobby not found.");

        if (game.Status != EGameStatus.Lobby)
            return Result<bool>.Fail("Lobby is no longer accepting changes.");

        var factionExists = await unitOfWork.FactionTypes.ExistsAsync(factionTypeId);
        if (!factionExists)
            return Result<bool>.Fail("Faction type not found.");

        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(game.Id);

        var playerKingdom = kingdoms.FirstOrDefault(k => k.AppUserId == userId);
        if (playerKingdom is null)
            return Result<bool>.Fail("You are not in this lobby.");

        var takenByOther = kingdoms.Any(k => k.AppUserId != userId && k.FactionTypeId.HasValue && k.FactionTypeId.Value == factionTypeId);
        if (takenByOther)
            return Result<bool>.Fail("That faction is already taken by another player.");

        // Load tracked kingdom for mutation
        var trackedKingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, game.Id);
        if (trackedKingdom is null)
            return Result<bool>.Fail("You are not in this lobby.");

        trackedKingdom.FactionTypeId = factionTypeId;
        await unitOfWork.Kingdoms.UpdateAsync(trackedKingdom);
        await unitOfWork.CommitAsync();

        return Result<bool>.Ok(true);
    }

    // -------------------------------------------------------------------------
    // Start Game
    // -------------------------------------------------------------------------

    public async Task<Result<bool>> StartGameAsync(Guid userId, Guid lobbyId)
    {
        var game = await unitOfWork.Games.GetByIdWithLockAsync(lobbyId);
        if (game is null)
            return Result<bool>.Fail("Lobby not found.");

        if (game.Status != EGameStatus.Lobby)
            return Result<bool>.Fail("Lobby is not in a startable state.");

        if (game.HostUserId != userId)
            return Result<bool>.Fail("Only the host can start the game.");

        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(game.Id);

        if (kingdoms.Count < 2)
            return Result<bool>.Fail("At least 2 players are required to start.");

        if (kingdoms.Any(k => k.FactionTypeId == null))
            return Result<bool>.Fail("All players must select a faction before starting.");

        // Assign turn order and kingdom names before transitioning to InProgress
        var orderedKingdoms = kingdoms.OrderBy(k => k.CreatedAt).ThenBy(k => k.Id).ToList();
        for (int i = 0; i < orderedKingdoms.Count; i++)
        {
            orderedKingdoms[i].TurnOrder = i + 1;
            if (string.IsNullOrEmpty(orderedKingdoms[i].Name))
            {
                orderedKingdoms[i].Name = orderedKingdoms[i].FactionType?.Name.ToString() ?? $"Kingdom {i + 1}";
            }
            await unitOfWork.Kingdoms.UpdateAsync(orderedKingdoms[i]);
        }

        game.Status = EGameStatus.InProgress;
        await unitOfWork.Games.UpdateAsync(game);
        await unitOfWork.CommitAsync();

        logger.LogInformation("[StartGame] Game={GameId} started by User={UserId} with {PlayerCount} players. TurnOrder: {TurnOrder}",
            lobbyId, userId, kingdoms.Count,
            string.Join(", ", orderedKingdoms.Select(k => $"{k.Name}(Order={k.TurnOrder})")));

        return Result<bool>.Ok(true);
    }

    // -------------------------------------------------------------------------
    // Get Lobby
    // -------------------------------------------------------------------------

    public async Task<Result<LobbyResponse>> GetLobbyAsync(Guid lobbyId)
    {
        var game = await unitOfWork.Games.GetGameWithKingdomsAsync(lobbyId);
        if (game is null)
            return Result<LobbyResponse>.Fail("Lobby not found.");

        return Result<LobbyResponse>.Ok(await BuildLobbyResponseAsync(game));
    }

    // -------------------------------------------------------------------------
    // Get Open Lobbies (Phase 36-02, MVCLOBBY-01)
    // -------------------------------------------------------------------------

    public async Task<Result<List<LobbyResponse>>> GetOpenLobbiesAsync()
    {
        var allGames = await unitOfWork.Games.GetAllAsync();
        var openGameIds = allGames
            .Where(g => g.Status == EGameStatus.Lobby)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => g.Id)
            .ToList();

        var responses = new List<LobbyResponse>();
        foreach (var id in openGameIds)
        {
            var fullGame = await unitOfWork.Games.GetGameWithKingdomsAsync(id);
            if (fullGame != null)
            {
                responses.Add(await BuildLobbyResponseAsync(fullGame));
            }
        }

        return Result<List<LobbyResponse>>.Ok(responses);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<string> GenerateUniqueLobbyCodeAsync()
    {
        string code;
        do { code = GenerateCode(); }
        while (await unitOfWork.Games.ExistsByLobbyCodeAsync(code));
        return code;
    }

    private static string GenerateCode() =>
        new(Enumerable.Range(0, 6).Select(_ => Alphabet[Random.Shared.Next(Alphabet.Length)]).ToArray());

    private async Task<LobbyResponse> BuildLobbyResponseAsync(Game game)
    {
        var allFactions = (await unitOfWork.FactionTypes.GetAllAsync()).ToList();
        var kingdoms = game.Kingdoms?.ToList() ?? [];

        // Batch-resolve user emails via IIdentityService (no AppUser nav prop needed)
        var userIds = kingdoms
            .Where(k => k.AppUserId.HasValue)
            .Select(k => k.AppUserId!.Value)
            .ToList();
        var emailMap = await identityService.GetEmailsAsync(userIds);

        var players = kingdoms
            .Where(k => k.AppUserId.HasValue)
            .Select(k => new PlayerInLobbyDto
            {
                KingdomId = k.Id,
                UserId = k.AppUserId!.Value,
                UserEmail = emailMap.GetValueOrDefault(k.AppUserId!.Value, string.Empty),
                FactionTypeId = k.FactionTypeId,
                FactionName = k.FactionType?.Name.Translate(),
                IsHost = game.HostUserId == k.AppUserId
            })
            .ToList();

        var takenFactionIds = kingdoms
            .Where(k => k.FactionTypeId.HasValue)
            .Select(k => k.FactionTypeId!.Value)
            .ToHashSet();

        var factions = allFactions
            .Select(f => new FactionAvailabilityDto
            {
                FactionTypeId = f.Id,
                Name = f.Name.Translate() ?? string.Empty,
                IsAvailable = !takenFactionIds.Contains(f.Id),
                Description = f.Description.Translate(),
                AttackModifier = f.AttackModifier,
                HPModifier = f.HPModifier,
                InitiativeModifier = f.InitiativeModifier,
                ChipDamageModifier = f.ChipDamageModifier,
                ResourceProductionModifier = f.ResourceProductionModifier,
                BuildingCostModifier = f.BuildingCostModifier,
                TrainingCostModifier = f.TrainingCostModifier,
                ActionPointModifier = f.ActionPointModifier,
                HealRateModifier = f.HealRateModifier
            })
            .ToList();

        return new LobbyResponse
        {
            Id = game.Id,
            LobbyCode = game.LobbyCode,
            Status = game.Status,
            MaxPlayers = game.MaxPlayers,
            WinCondition = game.WinCondition,
            HostUserId = game.HostUserId,
            PlayerCount = players.Count,
            Players = players,
            Factions = factions
        };
    }
}
