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

    /// <summary>
    /// Calculates healing with faction heal rate modifier, capped at maxHP.
    /// Uses int truncation for heal amount.
    /// </summary>
    public static int CalculateHealing(int currentHP, int maxHP, decimal healPercent, decimal factionHealRateModifier)
    {
        var healAmount = (int)(maxHP * healPercent * factionHealRateModifier);
        return Math.Min(currentHP + healAmount, maxHP);
    }

    /// <summary>
    /// Calculates total upkeep across all armies (Gold, Food, Mana).
    /// </summary>
    public static Dictionary<EResourceType, int> CalculateTotalUpkeep(
        List<(Army Army, ArmyType ArmyType)> armies)
    {
        return new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, armies.Sum(a => a.ArmyType.UpkeepGold) },
            { EResourceType.Food, armies.Sum(a => a.ArmyType.UpkeepFood) },
            { EResourceType.Mana, armies.Sum(a => a.ArmyType.UpkeepMana) },
        };
    }

    /// <summary>
    /// Determines which armies to disband when upkeep is unaffordable.
    /// Removes most expensive armies first (by combined Gold+Food+Mana upkeep).
    /// </summary>
    public static List<Army> GetArmiesToDisband(
        List<(Army Army, ArmyType ArmyType)> armies,
        List<KingdomResource> resources)
    {
        var toDisbandList = new List<Army>();

        if (CanAffordUpkeep(armies, resources))
            return toDisbandList;

        // Sort by most expensive first (combined upkeep)
        var sorted = armies
            .OrderByDescending(a => a.ArmyType.UpkeepGold + a.ArmyType.UpkeepFood + a.ArmyType.UpkeepMana)
            .ToList();

        var remaining = new List<(Army Army, ArmyType ArmyType)>(sorted);

        foreach (var army in sorted)
        {
            remaining.Remove(army);
            toDisbandList.Add(army.Army);

            if (CanAffordUpkeep(remaining, resources))
                break;
        }

        return toDisbandList;
    }

    /// <summary>
    /// Checks whether kingdom resources can cover total upkeep of given armies.
    /// </summary>
    public static bool CanAffordUpkeep(
        List<(Army Army, ArmyType ArmyType)> armies,
        List<KingdomResource> resources)
    {
        var upkeep = CalculateTotalUpkeep(armies);

        foreach (var (resourceType, cost) in upkeep)
        {
            if (cost == 0) continue;
            var resource = resources.FirstOrDefault(r => r.ResourceType == resourceType);
            if (resource is null || resource.Amount < cost)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Combat stats record returned by ApplyFactionModifiers.
    /// </summary>
    public record ArmyCombatStats(int Attack, int Initiative, decimal ChipDamageRangeMin, decimal ChipDamageRangeMax);

    /// <summary>
    /// Applies situational bonus based on attacker/defender role.
    /// Returns (stat name, multiplier) if bonus applies, null otherwise.
    /// </summary>
    public static (string stat, decimal multiplier)? ApplySituationalBonus(ArmyType armyType, bool isAttacker)
    {
        if (armyType.SituationalBonusCondition is null ||
            armyType.SituationalBonusStat is null ||
            armyType.SituationalBonusValue is null)
            return null;

        var conditionMatches = armyType.SituationalBonusCondition == ESituationalBonusCondition.Attacking
            ? isAttacker
            : !isAttacker;

        if (!conditionMatches)
            return null;

        return (armyType.SituationalBonusStat, 1 + armyType.SituationalBonusValue.Value);
    }

    /// <summary>
    /// Applies faction modifiers multiplicatively to combat stats.
    /// HP modifier is NOT applied here (applied once at training time).
    /// </summary>
    public static ArmyCombatStats ApplyFactionModifiers(
        int baseAttack, int baseInitiative,
        decimal chipDmgMin, decimal chipDmgMax,
        decimal atkMod, decimal initMod, decimal chipMod)
    {
        return new ArmyCombatStats(
            Attack: (int)(baseAttack * atkMod),
            Initiative: (int)(baseInitiative * initMod),
            ChipDamageRangeMin: chipDmgMin * chipMod,
            ChipDamageRangeMax: chipDmgMax * chipMod);
    }
}
