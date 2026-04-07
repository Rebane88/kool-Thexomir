using Application.Contracts;
using Application.Services.Combat.DTOs;
using Application.Services.GameInitialization.DTOs;
using Domain.Game;
using Domain.Map;
using Domain.Resources;
using Microsoft.Extensions.Logging;
using DomainBuilding = Domain.Buildings.Building;

namespace Application.Services.GameInitialization;

public class GameInitializationService(IUnitOfWork unitOfWork, ILogger<GameInitializationService> logger) : IGameInitializationService
{
    public async Task<GameStateDto> InitializeGameAsync(Guid gameId)
    {
        var game = await unitOfWork.Games.GetByIdAsync(gameId)
            ?? throw new InvalidOperationException($"Game {gameId} not found.");

        var kingdoms = (await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId))
            .OrderBy(k => k.TurnOrder).ToList();

        logger.LogInformation("[InitGame] Game={GameId} Players={Count} TurnOrder: {TurnOrder}",
            gameId, kingdoms.Count,
            string.Join(", ", kingdoms.Select(k => $"{k.Name}(Order={k.TurnOrder})")));

        var terrainTypes = (await unitOfWork.TerrainTypes.GetAllAsync()).ToList();

        // Determine map size (hexagonal grid centered at origin)
        var mapRadius = HexGridHelper.CalculateRadius(kingdoms.Count);
        game.MapWidth = mapRadius;
        game.MapHeight = 0;

        // Generate hex grid and starting positions
        var coords = HexGridHelper.GenerateHexGrid(mapRadius);
        var startPositions = HexGridHelper.CalculateStartingPositions(mapRadius, kingdoms.Count);

        // Build terrain arrays matching seeder order: Plains, Forest, Mountain, Desert, MagicGrove
        var terrainTypeIds = terrainTypes.Select(t => t.Id).ToArray();
        int[] terrainWeights = [30, 25, 20, 15, 10];
        var plainsId = TerrainType.PlainsId;

        // Assign terrain with anti-clustering
        var terrainMap = MapGenerator.AssignTerrainWithAntiClustering(
            coords, startPositions, terrainTypeIds, terrainWeights, new Random(), plainsId);

        // Ensure starting area variety
        MapGenerator.EnsureStartingAreaVariety(terrainMap, startPositions, terrainTypeIds, new Random());

        // Create all tile entities
        var coordSet = coords.ToHashSet();
        var tilesByCoord = new Dictionary<(int q, int r), Tile>(coords.Count);

        foreach (var coord in coords)
        {
            var tile = new Tile
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                CoordQ = coord.q,
                CoordR = coord.r,
                TerrainTypeId = terrainMap[coord],
                IsCastle = false
            };
            await unitOfWork.Tiles.AddAsync(tile);
            tilesByCoord[coord] = tile;
        }

        var castleBuildingTypeId = Domain.Buildings.BuildingType.CastleId;

        // Place castles and claim territory for each kingdom
        for (int i = 0; i < kingdoms.Count; i++)
        {
            var kingdom = kingdoms[i];
            var startPos = startPositions[i];

            // Set castle tile
            var castleTile = tilesByCoord[startPos];
            castleTile.KingdomId = kingdom.Id;
            castleTile.IsCastle = true;
            castleTile.TerrainTypeId = plainsId; // Castle always on Plains

            // Create Castle building
            await unitOfWork.Buildings.AddAsync(new DomainBuilding
            {
                Id = Guid.NewGuid(),
                TileId = castleTile.Id,
                BuildingTypeId = castleBuildingTypeId,
                KingdomId = kingdom.Id,
                BuiltOnRound = 1,
                BuiltAt = DateTime.UtcNow
            });

            // Claim 6 adjacent tiles
            var neighbors = HexGridHelper.GetNeighbors(startPos.q, startPos.r);
            foreach (var neighbor in neighbors)
            {
                if (tilesByCoord.TryGetValue(neighbor, out var neighborTile))
                {
                    neighborTile.KingdomId = kingdom.Id;
                }
            }
        }

        // Initialize resources (no armies created - MAPG-06)
        await InitializeKingdomResourcesAsync(gameId);

        // Set game state
        game.Status = EGameStatus.InProgress;
        game.StartedAt = DateTime.UtcNow;
        game.CurrentPhase = EGamePhase.Action;
        game.RoundNumber = 1;

        var firstKingdom = kingdoms.First();
        game.CurrentTurnKingdomId = firstKingdom.Id;

        var firstFaction = await unitOfWork.FactionTypes.GetByIdAsync(firstKingdom.FactionTypeId!.Value);
        game.RemainingActionPoints = game.BaseActionPoints + (firstFaction?.ActionPointModifier ?? 0);
        game.TurnDeadline = game.TurnTimeLimit.HasValue
            ? DateTime.UtcNow.AddSeconds(game.TurnTimeLimit.Value)
            : null;

        logger.LogInformation("[InitGame] Game={GameId} started. FirstTurn={KingdomName} (Id={KingdomId}) AP={AP} Phase={Phase}",
            gameId, firstKingdom.Name, firstKingdom.Id, game.RemainingActionPoints, game.CurrentPhase);

        await unitOfWork.CommitAsync();

        return await BuildGameStateSnapshotAsync(gameId);
    }

    public async Task<GameStateDto> BuildGameStateSnapshotAsync(Guid gameId)
    {
        var game = await unitOfWork.Games.GetByIdAsync(gameId)
            ?? throw new InvalidOperationException($"Game {gameId} not found.");

        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);
        var tiles = await unitOfWork.Tiles.GetTilesWithBuildingsForGameAsync(gameId);

        // Load resources and faction info per kingdom
        var kingdomDtos = new List<KingdomDto>();
        foreach (var kingdom in kingdoms)
        {
            var resources = await unitOfWork.KingdomResources.GetResourcesForKingdomAsync(kingdom.Id);
            var faction = kingdom.FactionTypeId.HasValue
                ? await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId.Value)
                : null;

            kingdomDtos.Add(new KingdomDto
            {
                Id = kingdom.Id,
                Name = kingdom.Name,
                UserId = kingdom.AppUserId,
                FactionTypeId = kingdom.FactionTypeId,
                FactionName = faction?.Name.Translate(),
                Status = kingdom.Status.ToString(),
                Resources = resources.Select(r => new KingdomResourceDto
                {
                    ResourceType = r.ResourceType.ToString(),
                    Amount = r.Amount
                }).ToList()
            });
        }

        // Load armies for all kingdoms
        var armyDtos = new List<ArmyDto>();
        foreach (var kingdom in kingdoms)
        {
            var armies = await unitOfWork.Armies.GetArmiesForKingdomAsync(kingdom.Id);
            armyDtos.AddRange(armies.Select(a => new ArmyDto
            {
                Id = a.Id,
                BuildingId = a.BuildingId,
                KingdomId = a.KingdomId,
                ArmyTypeId = a.ArmyTypeId,
                CurrentHP = a.CurrentHP,
                MaxHP = a.MaxHP
            }));
        }

        // Load declared attacks for current round (for reconnect)
        var declaredAttacks = await unitOfWork.DeclaredAttacks.GetForGameRoundAsync(gameId, game.RoundNumber);
        var declaredAttackDtos = declaredAttacks.Select(da => new DeclareAttackResponse
        {
            AttackId = da.Id,
            TargetTileId = da.TargetTileId,
            RiskedTileId = da.RiskedTileId,
            AttackerKingdomId = da.AttackerKingdomId,
            DefenderKingdomId = da.DefenderKingdomId,
            AttackerArmiesSelected = !string.IsNullOrEmpty(da.AttackerSelectedArmyIds),
            DefenderArmiesSelected = !string.IsNullOrEmpty(da.DefenderSelectedArmyIds),
            AttackerLineupConfirmed = da.AttackerLineupConfirmed,
            DefenderLineupConfirmed = da.DefenderLineupConfirmed
        }).ToList();

        return new GameStateDto
        {
            GameId = game.Id,
            Status = game.Status.ToString(),
            RoundNumber = game.RoundNumber,
            WinCondition = game.WinCondition.ToString(),
            MapWidth = game.MapWidth,
            MapHeight = game.MapHeight,
            MapRadius = game.MapWidth,
            CurrentTurnKingdomId = game.CurrentTurnKingdomId,
            CurrentPhase = game.CurrentPhase.ToString(),
            RemainingActionPoints = game.RemainingActionPoints,
            SpinCostGold = game.SpinCostGold,
            TurnDeadline = game.TurnDeadline,
            DeclaredAttacks = declaredAttackDtos,
            Tiles = tiles.Select(t => new TileDto
            {
                Id = t.Id,
                CoordQ = t.CoordQ,
                CoordR = t.CoordR,
                TerrainTypeId = t.TerrainTypeId,
                TerrainName = t.TerrainType?.Name.Translate() ?? string.Empty,
                TerrainKey = t.TerrainType?.Name.ContainsKey("en") == true ? t.TerrainType.Name["en"] : string.Empty,
                KingdomId = t.KingdomId,
                IsCastle = t.IsCastle,
                Buildings = (t.Buildings ?? []).Select(b => new BuildingDto
                {
                    Id = b.Id,
                    BuildingTypeId = b.BuildingTypeId,
                    BuildingName = b.BuildingType?.Name.Translate() ?? string.Empty
                }).ToList()
            }).ToList(),
            Kingdoms = kingdomDtos,
            Armies = armyDtos
        };
    }

    public async Task InitializeKingdomResourcesAsync(Guid gameId)
    {
        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);

        foreach (var kingdom in kingdoms)
        {
            if (!kingdom.FactionTypeId.HasValue) continue;
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId.Value);
            if (factionType is null) continue;

            var resources = ResourceInitializer.CreateStartingResources(
                kingdom.Id,
                factionType.StartingBonusResource,
                factionType.StartingBonusAmount);

            foreach (var resource in resources)
            {
                await unitOfWork.KingdomResources.AddAsync(resource);
            }
        }

        await unitOfWork.CommitAsync();
    }
}
