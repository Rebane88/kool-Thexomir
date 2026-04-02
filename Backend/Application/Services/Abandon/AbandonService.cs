using Application.Contracts;
using Application.Services.WinCondition.DTOs;
using Base.Contracts;
using Domain.Game;
using Microsoft.Extensions.Logging;

namespace Application.Services.Abandon;

public class AbandonService(
    IUnitOfWork unitOfWork,
    IGameGuard gameGuard,
    ILogger<AbandonService> logger) : IAbandonService
{
    public async Task<Result<GameOverDto?>> AbandonGameAsync(Guid gameId, Guid userId)
    {
        logger.LogInformation("[Abandon] Game={GameId} User={UserId}", gameId, userId);

        // Validate using guard (validate that user is in the game and game is in progress)
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
        {
            // If guard fails because it's not our turn, we still allow abandon
            // So let's try a simpler check
            var game = await unitOfWork.Games.GetByIdWithLockAsync(gameId);
            if (game is null)
                return Result<GameOverDto?>.Fail("Game not found.");

            if (game.Status != EGameStatus.InProgress)
                return Result<GameOverDto?>.Fail("Game is not in progress.");

            var userKingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, gameId);
            if (userKingdom is null)
                return Result<GameOverDto?>.Fail("You are not in this game.");

            if (userKingdom.Status == EKingdomStatus.Defeated)
                return Result<GameOverDto?>.Fail("Your kingdom has already been eliminated.");

            return await ProcessAbandonAsync(game, userKingdom, gameId);
        }

        return await ProcessAbandonAsync(guardResult.Value!.Game, guardResult.Value!.Kingdom, gameId);
    }

    private async Task<Result<GameOverDto?>> ProcessAbandonAsync(Domain.Game.Game game, Kingdom kingdom, Guid gameId)
    {
        logger.LogInformation("[Abandon] Kingdom={KingdomName} abandoning game", kingdom.Name);

        // Mark kingdom as Defeated
        kingdom.Status = EKingdomStatus.Defeated;
        kingdom.DefeatedAt = DateTime.UtcNow;

        // Get all kingdoms to check remaining active
        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);

        var remainingActive = kingdoms
            .Where(k => k.Id != kingdom.Id && k.Status == EKingdomStatus.Active)
            .ToList();

        GameOverDto? gameOverDto = null;

        if (remainingActive.Count <= 1)
        {
            var winner = remainingActive.FirstOrDefault();
            logger.LogInformation("[Abandon] Game over. Winner={Winner}", winner?.Name ?? "none");

            game.Status = EGameStatus.Completed;
            game.FinishedAt = DateTime.UtcNow;
            game.CurrentTurnKingdomId = null;
            game.TurnDeadline = null;
            game.RemainingActionPoints = null;

            // Build final standings
            var allKingdoms = kingdoms.ToList();
            // Update the kingdom object in the list to reflect the defeated status
            var kingdomInList = allKingdoms.FirstOrDefault(k => k.Id == kingdom.Id);
            if (kingdomInList is not null)
            {
                kingdomInList.Status = EKingdomStatus.Defeated;
                kingdomInList.DefeatedAt = kingdom.DefeatedAt;
            }

            var finalStandings = new List<KingdomResultDto>();
            foreach (var k in allKingdoms)
            {
                var tiles = await unitOfWork.Tiles.GetTilesWithBuildingsAndTerrainForKingdomAsync(k.Id);
                finalStandings.Add(new KingdomResultDto
                {
                    KingdomId = k.Id,
                    KingdomName = k.Name,
                    TilesOwned = tiles.Count,
                    Status = k.Status.ToString()
                });
            }

            gameOverDto = new GameOverDto
            {
                GameId = gameId,
                WinnerKingdomId = winner?.Id,
                WinConditionType = "Elimination",
                FinalStandings = finalStandings,
                EliminationOrder = []
            };
        }

        await unitOfWork.CommitAsync();

        return Result<GameOverDto?>.Ok(gameOverDto);
    }
}
