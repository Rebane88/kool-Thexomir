using Application.Contracts;
using Application.Services.Army.DTOs;
using Base.Contracts;
using Domain.Buildings;
using Domain.Game;
using Domain.Military;

namespace Application.Services.Army;

public class ArmyService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : IArmyService
{
    public async Task<Result<ArmyTrainedDto>> TrainArmyAsync(
        Guid gameId, Guid userId, TrainArmyRequest request)
    {
        // 1. Validate game/kingdom access (costs 1 AP)
        var guardResult = await gameGuard.ValidateActionAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<ArmyTrainedDto>.Fail(guardResult.Error!);

        var game = guardResult.Value!.Game;
        var kingdom = guardResult.Value!.Kingdom;

        // 2. Load building
        var building = await unitOfWork.Buildings.GetByIdAsync(request.BuildingId);
        if (building is null)
            return Result<ArmyTrainedDto>.Fail("Building not found.");
        if (building.KingdomId != kingdom.Id)
            return Result<ArmyTrainedDto>.Fail("Building does not belong to your kingdom.");

        // 3. Load building type
        var buildingType = await unitOfWork.BuildingTypes.GetByIdAsync(building.BuildingTypeId);

        // 4. Load army type
        var armyType = await unitOfWork.ArmyTypes.GetByIdAsync(request.ArmyTypeId);
        if (armyType is null)
            return Result<ArmyTrainedDto>.Fail("Army type not found.");

        // 5. Load required building type (for tier comparison)
        var requiredBuildingType = await unitOfWork.BuildingTypes.GetByIdAsync(armyType.RequiredBuildingTypeId);

        // 6. Get army count at building for capacity check
        var armyCount = await unitOfWork.Armies.GetArmyCountForBuildingAsync(request.BuildingId);

        // 7. Validate training eligibility
        var validationError = ArmyRules.ValidateTraining(buildingType!, armyType, armyCount, requiredBuildingType!.Tier);
        if (validationError is not null)
            return Result<ArmyTrainedDto>.Fail(validationError);

        // 8. Load faction type for modifiers
        var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId!.Value);

        // 9. Get modified training costs
        var costs = ArmyRules.GetModifiedTrainingCosts(armyType, factionType!.TrainingCostModifier);

        // 10. Load mutable resources (tracked entities)
        var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);

        // 11. Deduct resources
        var deductError = BuildingRules.DeductResourceCost(resources, costs);
        if (deductError is not null)
            return Result<ArmyTrainedDto>.Fail(deductError);

        // 12. Create Army entity
        var maxHP = ArmyRules.CalculateMaxHP(armyType.HP, factionType.HPModifier);
        var army = new Domain.Military.Army
        {
            Id = Guid.NewGuid(),
            ArmyTypeId = armyType.Id,
            KingdomId = kingdom.Id,
            BuildingId = building.Id,
            MaxHP = maxHP,
            CurrentHP = maxHP,
            CreatedOnRound = game.RoundNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.Armies.AddAsync(army);

        // 13. Create TurnLog
        var turnLog = new TurnLog
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            KingdomId = kingdom.Id,
            RoundNumber = game.RoundNumber,
            EventType = EEventType.ArmyTrained,
            Description = $"Trained {armyType.Name.Translate()}",
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.TurnLogs.AddAsync(turnLog);

        // 14. Commit
        await unitOfWork.CommitAsync();

        // 15. Return result
        var resourcesAfter = resources.ToDictionary(
            r => r.ResourceType.ToString(),
            r => (int)r.Amount);

        return Result<ArmyTrainedDto>.Ok(new ArmyTrainedDto
        {
            ArmyId = army.Id,
            BuildingId = building.Id,
            ArmyTypeId = armyType.Id,
            ArmyTypeName = armyType.Name.Translate() ?? string.Empty,
            KingdomId = kingdom.Id,
            CurrentHP = army.CurrentHP,
            MaxHP = army.MaxHP,
            ResourcesAfter = resourcesAfter,
            ActionPointsAfter = game.RemainingActionPoints ?? 0
        });
    }

    public async Task<IEnumerable<ArmyTypeDto>> GetArmyTypesAsync(Guid gameId, Guid userId)
    {
        var armyTypes = await unitOfWork.ArmyTypes.GetAllWithRequiredBuildingAsync();

        // Load the player's kingdom and faction to apply training cost modifiers
        var kingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, gameId);
        decimal trainingCostModifier = 1m;
        if (kingdom?.FactionTypeId is not null)
        {
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId.Value);
            if (factionType is not null)
                trainingCostModifier = factionType.TrainingCostModifier;
        }

        return armyTypes.Select(at => new ArmyTypeDto
        {
            Id = at.Id,
            Name = at.Name.Translate() ?? string.Empty,
            Attack = at.Attack,
            HP = at.HP,
            Initiative = at.Initiative,
            DamageRangeMin = at.DamageRangeMin,
            DamageRangeMax = at.DamageRangeMax,
            ChipDamageRangeMin = at.ChipDamageRangeMin,
            ChipDamageRangeMax = at.ChipDamageRangeMax,
            SituationalBonusStat = at.SituationalBonusStat,
            SituationalBonusValue = at.SituationalBonusValue,
            SituationalBonusCondition = at.SituationalBonusCondition?.ToString(),
            TrainingCostGold = BuildingRules.ApplyTrainingCostModifier(at.TrainingCostGold, trainingCostModifier),
            TrainingCostFood = BuildingRules.ApplyTrainingCostModifier(at.TrainingCostFood, trainingCostModifier),
            TrainingCostStone = BuildingRules.ApplyTrainingCostModifier(at.TrainingCostStone, trainingCostModifier),
            TrainingCostMana = BuildingRules.ApplyTrainingCostModifier(at.TrainingCostMana, trainingCostModifier),
            UpkeepGold = at.UpkeepGold,
            UpkeepFood = at.UpkeepFood,
            UpkeepMana = at.UpkeepMana,
            RequiredBuildingTypeId = at.RequiredBuildingTypeId,
            RequiredBuildingName = at.RequiredBuildingType?.Name.Translate()
        });
    }
}
