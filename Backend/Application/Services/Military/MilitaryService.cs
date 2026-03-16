using Application.Contracts;
using Application.Services.Military.DTOs;
using Application.Services.WinCondition.DTOs;
using Base.Contracts;
using Domain.Factions;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.Military;

public class MilitaryService(IUnitOfWork unitOfWork, IGameGuard gameGuard, IServiceProvider serviceProvider) : IMilitaryService
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

    public async Task<Result<CombatResolvedDto>> AttackAsync(Guid gameId, Guid userId, AttackRequest request)
    {
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<CombatResolvedDto>.Fail(guardResult.Error!);

        var (game, kingdom) = guardResult.Value!;

        // Load attacker army with units
        var attackerArmy = await unitOfWork.Armies.GetArmyWithUnitsAsync(request.AttackerArmyId);
        if (attackerArmy is null)
            return Result<CombatResolvedDto>.Fail("Attacker army not found.");

        // Load tiles
        var attackerTile = await unitOfWork.Tiles.GetByIdAsync(attackerArmy.TileId);
        var defenderTile = await unitOfWork.Tiles.GetByIdAsync(request.DefenderTileId);
        if (attackerTile is null || defenderTile is null)
            return Result<CombatResolvedDto>.Fail("Tile not found.");

        // Load defender army on target tile (any army not owned by attacker)
        var defenderArmy = await unitOfWork.Armies.GetEnemyArmyOnTileAsync(request.DefenderTileId, kingdom.Id);
        if (defenderArmy is null)
            return Result<CombatResolvedDto>.Fail("No enemy army on target tile.");

        // Load matchups
        var matchups = (await unitOfWork.UnitTypeMatchups.GetAllMatchupsAsync()).ToList();

        // Load faction bonuses for both sides
        var attackerKingdom = await unitOfWork.Kingdoms.GetByIdAsync(kingdom.Id);
        var defenderKingdom = await unitOfWork.Kingdoms.GetByIdAsync(defenderArmy.KingdomId);

        var attackerFactionBonuses = attackerKingdom?.FactionTypeId.HasValue == true
            ? (await unitOfWork.FactionUnitBonuses.GetBonusesForFactionAsync(attackerKingdom.FactionTypeId!.Value)).ToList()
            : new List<FactionUnitBonus>();
        var defenderFactionBonuses = defenderKingdom?.FactionTypeId.HasValue == true
            ? (await unitOfWork.FactionUnitBonuses.GetBonusesForFactionAsync(defenderKingdom.FactionTypeId!.Value)).ToList()
            : new List<FactionUnitBonus>();

        // Load terrain
        var defenderTerrain = await unitOfWork.TerrainTypes.GetByIdAsync(defenderTile.TerrainTypeId);
        if (defenderTerrain is null)
            return Result<CombatResolvedDto>.Fail("Terrain type not found.");

        // Set up game aggregate
        game.Kingdoms = [kingdom];

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            matchups, attackerFactionBonuses, defenderFactionBonuses, defenderTerrain);
        if (!result.IsSuccess)
            return Result<CombatResolvedDto>.Fail(result.Error!);

        var combat = result.Value!;

        // Persist battle record
        await unitOfWork.Battles.AddAsync(combat.Battle);

        // Persist attacker army changes
        if (combat.AttackerArmyDestroyed)
        {
            foreach (var unit in attackerArmy.Units!)
                await unitOfWork.Units.DeleteAsync(unit.Id);
            await unitOfWork.Armies.DeleteAsync(attackerArmy.Id);
        }
        else
        {
            foreach (var unit in attackerArmy.Units!.Where(u => u.Quantity > 0))
                await unitOfWork.Units.UpdateAsync(unit);
            await unitOfWork.Armies.UpdateAsync(attackerArmy);
        }

        // Persist defender army changes
        if (combat.DefenderArmyDestroyed)
        {
            foreach (var unit in defenderArmy.Units!)
                await unitOfWork.Units.DeleteAsync(unit.Id);
            await unitOfWork.Armies.DeleteAsync(defenderArmy.Id);
        }
        else
        {
            foreach (var unit in defenderArmy.Units!.Where(u => u.Quantity > 0))
                await unitOfWork.Units.UpdateAsync(unit);
        }

        // Persist tile changes if captured
        if (combat.TileCaptured)
            await unitOfWork.Tiles.UpdateAsync(defenderTile);

        // TurnLog
        await unitOfWork.TurnLogs.AddAsync(new TurnLog
        {
            GameId = gameId,
            KingdomId = kingdom.Id,
            TurnNumber = game.TurnNumber,
            Action = "Attack"
        });

        // --- Phase 13: Elimination cleanup + win condition check ---
        GameOverDto? gameOverDto = null;

        if (combat.TileCaptured && defenderTile.IsCapital)
        {
            // Mark defender kingdom eliminated
            defenderKingdom!.IsEliminated = true;
            await unitOfWork.Kingdoms.UpdateAsync(defenderKingdom);

            // Nullify all remaining defender tiles
            var defenderTiles = await unitOfWork.Tiles.GetTilesForKingdomAsync(defenderKingdom.Id);
            foreach (var tile in defenderTiles)
            {
                tile.KingdomId = null;
                await unitOfWork.Tiles.UpdateAsync(tile);
            }

            // Hard-delete all defender armies and their units
            var defenderArmies = await unitOfWork.Armies.GetArmiesWithUnitsForKingdomAsync(defenderKingdom.Id);
            foreach (var army in defenderArmies)
            {
                foreach (var unit in army.Units ?? [])
                    await unitOfWork.Units.DeleteAsync(unit.Id);
                await unitOfWork.Armies.DeleteAsync(army.Id);
            }

            // Check win condition
            var checker = serviceProvider.GetRequiredKeyedService<IWinConditionChecker>(game.WinCondition);
            var allKingdoms = (await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId)).ToList();
            var allTiles = await unitOfWork.Tiles.GetTilesWithBuildingsForGameAsync(gameId);
            var winResult = game.CheckWinCondition(checker, allKingdoms, allTiles);

            if (winResult?.GameOver == true)
            {
                await unitOfWork.Games.UpdateAsync(game);
                gameOverDto = BuildGameOverDto(game, winResult, allKingdoms, allTiles);
            }
        }

        await unitOfWork.CommitAsync();

        return Result<CombatResolvedDto>.Ok(new CombatResolvedDto
        {
            BattleId = combat.Battle.Id,
            TileId = defenderTile.Id,
            AttackerKingdomId = kingdom.Id,
            DefenderKingdomId = defenderArmy.KingdomId,
            WinnerKingdomId = combat.WinnerKingdomId,
            TileCaptured = combat.TileCaptured,
            AttackerStrength = combat.AttackerStrength,
            DefenderStrength = combat.DefenderStrength,
            AttackerCasualties = combat.AttackerCasualties.Select(c => new CasualtyDto
            {
                UnitTypeId = c.UnitTypeId,
                UnitTypeName = c.UnitTypeName,
                Before = c.Before,
                Lost = c.Lost,
                After = c.After
            }).ToList(),
            DefenderCasualties = combat.DefenderCasualties.Select(c => new CasualtyDto
            {
                UnitTypeId = c.UnitTypeId,
                UnitTypeName = c.UnitTypeName,
                Before = c.Before,
                Lost = c.Lost,
                After = c.After
            }).ToList(),
            GameOver = gameOverDto
        });
    }

    private static GameOverDto BuildGameOverDto(
        Game game,
        WinCheckResult winResult,
        List<Kingdom> allKingdoms,
        List<Tile> allTiles)
    {
        var tiles = allTiles.AsReadOnly();
        return new GameOverDto
        {
            GameId = game.Id,
            WinnerKingdomId = winResult.WinnerKingdomId,
            WinConditionType = winResult.WinConditionType.ToString(),
            FinalScores = allKingdoms.Select(k => new KingdomScoreDto
            {
                KingdomId = k.Id,
                KingdomName = k.Name,
                Score = ScoreChecker.CalculateScore(k, tiles),
                TilesOwned = allTiles.Count(t => t.KingdomId == k.Id),
                IsEliminated = k.IsEliminated
            }).OrderByDescending(s => s.Score).ToList(),
            EliminationOrder = allKingdoms
                .Where(k => k.IsEliminated)
                .OrderBy(k => k.UpdatedAt)
                .Select(k => k.Id)
                .ToList()
        };
    }
}
