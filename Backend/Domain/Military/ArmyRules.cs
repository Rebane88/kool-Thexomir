using Domain.Buildings;
using Domain.Resources;

namespace Domain.Military;

public static class ArmyRules
{
    /// <summary>
    /// Validates whether a building can train a given army type.
    /// Returns null on success, error message on failure.
    /// </summary>
    public static string? ValidateTraining(
        BuildingType buildingType,
        ArmyType armyType,
        int currentArmyCountAtBuilding,
        int requiredBuildingTier)
    {
        if (buildingType.ArmyCapacity == 0)
            return "This building cannot train armies.";

        if (currentArmyCountAtBuilding >= buildingType.ArmyCapacity)
            return $"This building is at full army capacity ({buildingType.ArmyCapacity}).";

        if (!CanBuildingTrainArmyType(buildingType, armyType, requiredBuildingTier))
            return $"This building cannot train {armyType.Name}.";

        return null;
    }

    /// <summary>
    /// Checks if a building type can train a specific army type based on chain and tier.
    /// </summary>
    public static bool CanBuildingTrainArmyType(
        BuildingType buildingType,
        ArmyType armyType,
        int requiredBuildingTier)
    {
        if (buildingType.Chain != "Military")
            return false;

        if (buildingType.Id == armyType.RequiredBuildingTypeId)
            return true;

        return buildingType.Tier >= requiredBuildingTier;
    }

    /// <summary>
    /// Calculates max HP applying faction modifier with int truncation.
    /// </summary>
    public static int CalculateMaxHP(int baseHP, decimal factionHPModifier)
    {
        return (int)(baseHP * factionHPModifier);
    }

    /// <summary>
    /// Calculates effective attack scaled linearly by current HP ratio.
    /// </summary>
    public static int CalculateEffectiveAttack(int baseAttack, int currentHP, int maxHP)
    {
        if (maxHP == 0) return 0;
        return (int)(baseAttack * ((decimal)currentHP / maxHP));
    }

    /// <summary>
    /// Returns modified training costs applying faction training cost modifier.
    /// Uses BuildingRules.ApplyTrainingCostModifier for ceil rounding.
    /// </summary>
    public static Dictionary<EResourceType, int> GetModifiedTrainingCosts(
        ArmyType armyType,
        decimal factionTrainingCostModifier)
    {
        return new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, BuildingRules.ApplyTrainingCostModifier(armyType.TrainingCostGold, factionTrainingCostModifier) },
            { EResourceType.Food, BuildingRules.ApplyTrainingCostModifier(armyType.TrainingCostFood, factionTrainingCostModifier) },
            { EResourceType.Stone, BuildingRules.ApplyTrainingCostModifier(armyType.TrainingCostStone, factionTrainingCostModifier) },
            { EResourceType.Mana, BuildingRules.ApplyTrainingCostModifier(armyType.TrainingCostMana, factionTrainingCostModifier) },
        };
    }
}
