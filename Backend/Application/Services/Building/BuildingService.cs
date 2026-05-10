using Application.Contracts;
using Application.Services.Building.DTOs.V1;
using Base.Contracts;
using Domain.Buildings;
using Domain.Game;
using Domain.Map;

namespace Application.Services.Building;

public class BuildingService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : IBuildingService
{
    public async Task<IEnumerable<BuildingTypeDto>> GetBuildingTypesAsync(Guid gameId, Guid userId)
    {
        var types = await unitOfWork.BuildingTypes.GetAllWithPrerequisiteAsync();

        // Load the player's kingdom and faction to apply cost modifiers
        var kingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, gameId);
        decimal costModifier = 1m;
        if (kingdom?.FactionTypeId is not null)
        {
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId.Value);
            if (factionType is not null)
                costModifier = factionType.BuildingCostModifier;
        }

        return types.Select(bt => new BuildingTypeDto
        {
            Id = bt.Id,
            Code = bt.Code,
            Name = bt.Name.Translate() ?? string.Empty,
            Tier = bt.Tier,
            Chain = bt.Chain,
            CostGold = BuildingRules.ApplyFactionCostModifier(bt.CostGold, costModifier),
            CostFood = BuildingRules.ApplyFactionCostModifier(bt.CostFood, costModifier),
            CostWood = BuildingRules.ApplyFactionCostModifier(bt.CostWood, costModifier),
            CostStone = BuildingRules.ApplyFactionCostModifier(bt.CostStone, costModifier),
            CostMana = BuildingRules.ApplyFactionCostModifier(bt.CostMana, costModifier),
            BaseYieldGold = bt.BaseYieldGold,
            BaseYieldFood = bt.BaseYieldFood,
            BaseYieldWood = bt.BaseYieldWood,
            BaseYieldStone = bt.BaseYieldStone,
            BaseYieldMana = bt.BaseYieldMana,
            Description = bt.Description.Translate(),
            UnlockedByBuildingTypeId = bt.UnlockedByBuildingTypeId,
            UnlockedByBuildingName = bt.UnlockedByBuildingType?.Name.Translate(),
            ArmyCapacity = bt.ArmyCapacity
        });
    }

    public async Task<Result<BuildingPlacedDto>> PlaceBuildingAsync(
        Guid gameId, Guid userId, PlaceBuildingRequest request)
    {
        // 1. Validate game/kingdom access
        var guardResult = await gameGuard.ValidateActionAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<BuildingPlacedDto>.Fail(guardResult.Error!);

        var game = guardResult.Value!.Game;
        var kingdom = guardResult.Value!.Kingdom;

        // 2. Load building type
        var buildingType = await unitOfWork.BuildingTypes.GetByIdAsync(request.BuildingTypeId);
        if (buildingType is null)
            return Result<BuildingPlacedDto>.Fail("Building type not found.");

        // 3. Load tile
        var tile = await unitOfWork.Tiles.GetByIdAsync(request.TileId);
        if (tile is null)
            return Result<BuildingPlacedDto>.Fail("Tile not found.");
        if (tile.GameId != gameId)
            return Result<BuildingPlacedDto>.Fail("Tile does not belong to this game.");

        // 4. Load existing building on tile
        var kingdomBuildings = await unitOfWork.Buildings.GetBuildingsForKingdomAsync(kingdom.Id);
        var existingBuilding = kingdomBuildings.FirstOrDefault(b => b.TileId == request.TileId);

        // 5. Validate placement
        var validationError = BuildingRules.ValidatePlacement(buildingType, existingBuilding, tile, kingdom.Id);
        if (validationError is not null)
            return Result<BuildingPlacedDto>.Fail(validationError);

        // 6. Load faction type
        var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId!.Value);

        // 7. Load mutable resources (tracked entities)
        var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);

        // 8. Deduct costs
        var deductError = BuildingRules.DeductBuildingCost(resources, buildingType, factionType!.BuildingCostModifier);
        if (deductError is not null)
            return Result<BuildingPlacedDto>.Fail(deductError);

        // 9. Handle building entity
        Guid buildingId;
        bool isUpgrade;

        if (existingBuilding is not null)
        {
            // UPGRADE: update in-place to preserve Army FK references
            existingBuilding.BuildingTypeId = buildingType.Id;
            existingBuilding.BuiltOnRound = game.RoundNumber;
            existingBuilding.BuiltAt = DateTime.UtcNow;
            existingBuilding.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.Buildings.UpdateAsync(existingBuilding);
            buildingId = existingBuilding.Id;
            isUpgrade = true;
        }
        else
        {
            // NEW PLACEMENT
            var newBuilding = new Domain.Buildings.Building
            {
                Id = Guid.NewGuid(),
                TileId = request.TileId,
                BuildingTypeId = buildingType.Id,
                KingdomId = kingdom.Id,
                BuiltOnRound = game.RoundNumber,
                BuiltAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await unitOfWork.Buildings.AddAsync(newBuilding);
            buildingId = newBuilding.Id;
            isUpgrade = false;
        }

        // 10. Tile claiming (tier 1 new placement only)
        var claimedTileIds = new List<Guid>();

        if (buildingType.Tier == 1 && !isUpgrade)
        {
            var neighbors = HexGridHelper.GetNeighbors(tile.CoordQ, tile.CoordR);
            var gameTiles = await unitOfWork.Tiles.GetTilesForGameAsync(gameId);

            foreach (var (q, r) in neighbors)
            {
                var adjacentTile = gameTiles.FirstOrDefault(t => t.CoordQ == q && t.CoordR == r);
                if (adjacentTile is not null && adjacentTile.KingdomId is null)
                {
                    adjacentTile.KingdomId = kingdom.Id;
                    adjacentTile.UpdatedAt = DateTime.UtcNow;
                    await unitOfWork.Tiles.UpdateAsync(adjacentTile);
                    claimedTileIds.Add(adjacentTile.Id);
                }
            }
        }

        // 11. Create TurnLog for building
        var buildTurnLog = new TurnLog
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            KingdomId = kingdom.Id,
            RoundNumber = game.RoundNumber,
            EventType = isUpgrade ? EEventType.BuildingUpgraded : EEventType.BuildingConstructed,
            Description = isUpgrade
                ? $"Upgraded to {buildingType.Name.Translate()}"
                : $"Built {buildingType.Name.Translate()}",
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.TurnLogs.AddAsync(buildTurnLog);

        // 12. Create TurnLog for tile capture if any tiles claimed
        if (claimedTileIds.Count > 0)
        {
            var captureTurnLog = new TurnLog
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                KingdomId = kingdom.Id,
                RoundNumber = game.RoundNumber,
                EventType = EEventType.TileCaptured,
                Description = $"Claimed {claimedTileIds.Count} adjacent tile(s)",
                OccurredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await unitOfWork.TurnLogs.AddAsync(captureTurnLog);
        }

        // 13. Commit
        await unitOfWork.CommitAsync();

        // 14. Return result
        var resourcesAfter = resources.ToDictionary(
            r => r.ResourceType.ToString(),
            r => (int)r.Amount);

        return Result<BuildingPlacedDto>.Ok(new BuildingPlacedDto
        {
            BuildingId = buildingId,
            TileId = request.TileId,
            BuildingTypeId = buildingType.Id,
            BuildingCode = buildingType.Code,
            BuildingName = buildingType.Name.Translate() ?? string.Empty,
            KingdomId = kingdom.Id,
            ResourcesAfter = resourcesAfter,
            ClaimedTileIds = claimedTileIds,
            IsUpgrade = isUpgrade,
            ActionPointsAfter = game.RemainingActionPoints ?? 0
        });
    }
}
