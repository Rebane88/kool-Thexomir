using Application.Contracts;
using Application.Services.Military.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Military;

namespace Application.Services.Military;

public class MilitaryService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : IMilitaryService
{
    public async Task<Result<TroopsTrainedDto>> TrainTroopsAsync(Guid gameId, Guid userId, TrainTroopsRequest request)
    {
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<TroopsTrainedDto>.Fail(guardResult.Error!);

        var (game, kingdom) = guardResult.Value!;

        // Load building
        var building = await unitOfWork.Buildings.GetByIdAsync(request.BuildingId);
        if (building is null)
            return Result<TroopsTrainedDto>.Fail("Building not found.");

        // Load the tile the building is on
        var tile = await unitOfWork.Tiles.GetByIdAsync(building.TileId);
        if (tile is null)
            return Result<TroopsTrainedDto>.Fail("Tile not found.");

        // Load building-unit-type mapping
        var buildingUnitType = await unitOfWork.BuildingUnitTypes
            .GetByBuildingAndUnitTypeAsync(building.BuildingTypeId, request.UnitTypeId);

        // Load unit type
        var unitType = await unitOfWork.UnitTypes.GetByIdAsync(request.UnitTypeId);
        if (unitType is null)
            return Result<TroopsTrainedDto>.Fail("Unit type not found.");

        // Load resources and faction cost modifier
        var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);
        var faction = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId!.Value);
        var costModifier = faction?.BuildingCostModifier ?? 1.0m;

        // Load existing army on tile
        var existingArmy = await unitOfWork.Armies.GetArmyOnTileForKingdomAsync(tile.Id, kingdom.Id);

        // Set up game aggregate
        game.Kingdoms = [kingdom];

        // Domain method
        var result = game.TrainTroops(building, buildingUnitType, unitType, request.Quantity,
            existingArmy, tile, resources, costModifier);
        if (!result.IsSuccess)
            return Result<TroopsTrainedDto>.Fail(result.Error!);

        var (army, units) = result.Value!;

        // Persist
        if (existingArmy is null)
            await unitOfWork.Armies.AddAsync(army);

        foreach (var unit in units)
        {
            if (unit.Id == Guid.Empty)
                await unitOfWork.Units.AddAsync(unit);
            else
                await unitOfWork.Units.UpdateAsync(unit);
        }

        await unitOfWork.Buildings.UpdateAsync(building);
        foreach (var resource in resources)
            await unitOfWork.KingdomResources.UpdateAsync(resource);

        // TurnLog
        await unitOfWork.TurnLogs.AddAsync(new TurnLog
        {
            GameId = gameId,
            KingdomId = kingdom.Id,
            TurnNumber = game.TurnNumber,
            Action = "Train"
        });

        await unitOfWork.CommitAsync();

        var trainedUnit = units.First(u => u.UnitTypeId == request.UnitTypeId);
        return Result<TroopsTrainedDto>.Ok(new TroopsTrainedDto
        {
            ArmyId = army.Id,
            TileId = tile.Id,
            UnitTypeId = request.UnitTypeId,
            UnitTypeName = unitType.Name.Translate() ?? string.Empty,
            QuantityTrained = request.Quantity,
            TotalQuantity = trainedUnit.Quantity,
            KingdomId = kingdom.Id,
            ResourcesAfter = resources.ToDictionary(r => r.ResourceType.ToString(), r => (int)r.Amount)
        });
    }

    public async Task<Result<ArmyMovedDto>> MoveArmyAsync(Guid gameId, Guid userId, MoveArmyRequest request)
    {
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<ArmyMovedDto>.Fail(guardResult.Error!);

        var (game, kingdom) = guardResult.Value!;

        // Load army with units
        var army = await unitOfWork.Armies.GetArmyWithUnitsAsync(request.ArmyId);
        if (army is null)
            return Result<ArmyMovedDto>.Fail("Army not found.");

        // Load source and target tiles
        var sourceTile = await unitOfWork.Tiles.GetByIdAsync(army.TileId);
        var targetTile = await unitOfWork.Tiles.GetByIdAsync(request.TargetTileId);
        if (sourceTile is null || targetTile is null)
            return Result<ArmyMovedDto>.Fail("Tile not found.");

        // Check for existing armies on target tile
        var existingFriendlyArmy = await unitOfWork.Armies.GetArmyOnTileForKingdomAsync(request.TargetTileId, kingdom.Id);
        var existingEnemyArmy = await unitOfWork.Armies.GetEnemyArmyOnTileAsync(request.TargetTileId, kingdom.Id);

        // Set up game
        game.Kingdoms = [kingdom];
        var fromTileId = army.TileId;

        var result = game.MoveArmy(army, sourceTile, targetTile, existingFriendlyArmy, existingEnemyArmy);
        if (!result.IsSuccess)
            return Result<ArmyMovedDto>.Fail(result.Error!);

        var (tileClaimed, armyMerged, mergedIntoArmy) = result.Value!;

        // Persist
        if (armyMerged && mergedIntoArmy != null)
        {
            // Update merged army's units
            foreach (var unit in mergedIntoArmy.Units!)
            {
                if (unit.Id == Guid.Empty)
                    await unitOfWork.Units.AddAsync(unit);
                else
                    await unitOfWork.Units.UpdateAsync(unit);
            }
            // Delete the original (moving) army's units and army
            foreach (var unit in army.Units!)
                await unitOfWork.Units.DeleteAsync(unit.Id);
            await unitOfWork.Armies.DeleteAsync(army.Id);
        }
        else
        {
            await unitOfWork.Armies.UpdateAsync(army);
        }

        if (tileClaimed)
            await unitOfWork.Tiles.UpdateAsync(targetTile);

        // TurnLog
        await unitOfWork.TurnLogs.AddAsync(new TurnLog
        {
            GameId = gameId,
            KingdomId = kingdom.Id,
            TurnNumber = game.TurnNumber,
            Action = "Move"
        });

        await unitOfWork.CommitAsync();

        return Result<ArmyMovedDto>.Ok(new ArmyMovedDto
        {
            ArmyId = armyMerged ? mergedIntoArmy!.Id : army.Id,
            FromTileId = fromTileId,
            ToTileId = request.TargetTileId,
            KingdomId = kingdom.Id,
            TileClaimed = tileClaimed,
            ArmyMerged = armyMerged,
            MergedIntoArmyId = mergedIntoArmy?.Id
        });
    }

    public Task<Result<CombatResolvedDto>> AttackAsync(Guid gameId, Guid userId, AttackRequest request)
    {
        // Implemented in Plan 03
        throw new NotImplementedException();
    }
}
