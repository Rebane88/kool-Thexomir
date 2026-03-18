using Domain.Buildings;
using Domain.Map;

namespace Domain.Resources;

public static class IncomeCalculator
{
    /// <summary>
    /// Calculates income from a single building on a terrain tile.
    /// Formula: floor(baseYield * terrainMultiplier * factionModifier)
    /// Terrain multiplier only applies when terrain ResourceBonusType matches the yielded resource.
    /// </summary>
    public static Dictionary<EResourceType, int> CalculateBuildingIncome(
        BuildingType buildingType,
        TerrainType terrainType,
        decimal factionResourceProductionModifier)
    {
        var income = new Dictionary<EResourceType, int>();

        AddIfPositive(income, EResourceType.Gold, buildingType.BaseYieldGold, terrainType, factionResourceProductionModifier);
        AddIfPositive(income, EResourceType.Food, buildingType.BaseYieldFood, terrainType, factionResourceProductionModifier);
        AddIfPositive(income, EResourceType.Wood, buildingType.BaseYieldWood, terrainType, factionResourceProductionModifier);
        AddIfPositive(income, EResourceType.Stone, buildingType.BaseYieldStone, terrainType, factionResourceProductionModifier);
        AddIfPositive(income, EResourceType.Mana, buildingType.BaseYieldMana, terrainType, factionResourceProductionModifier);

        return income;
    }

    /// <summary>
    /// Calculates total kingdom income by aggregating all building incomes.
    /// </summary>
    public static Dictionary<EResourceType, int> CalculateKingdomIncome(
        List<(BuildingType BuildingType, TerrainType TerrainType)> buildings,
        decimal factionResourceProductionModifier)
    {
        var total = new Dictionary<EResourceType, int>();

        foreach (var (buildingType, terrainType) in buildings)
        {
            var buildingIncome = CalculateBuildingIncome(buildingType, terrainType, factionResourceProductionModifier);
            foreach (var (resourceType, amount) in buildingIncome)
            {
                total.TryGetValue(resourceType, out var current);
                total[resourceType] = current + amount;
            }
        }

        return total;
    }

    // --- Private helpers ---

    private static void AddIfPositive(
        Dictionary<EResourceType, int> income,
        EResourceType resourceType,
        int baseYield,
        TerrainType terrainType,
        decimal factionModifier)
    {
        if (baseYield == 0) return;

        var terrainMultiplier = DoesTerrainBoost(terrainType, resourceType)
            ? terrainType.ResourceMultiplier
            : 1.0m;

        var amount = (int)(baseYield * terrainMultiplier * factionModifier);
        if (amount > 0)
            income[resourceType] = amount;
    }

    private static bool DoesTerrainBoost(TerrainType terrain, EResourceType resource) =>
        terrain.ResourceBonusType switch
        {
            ETerrainResourceBonus.Gold => resource == EResourceType.Gold,
            ETerrainResourceBonus.Food => resource == EResourceType.Food,
            ETerrainResourceBonus.Wood => resource == EResourceType.Wood,
            ETerrainResourceBonus.Stone => resource == EResourceType.Stone,
            ETerrainResourceBonus.Mana => resource == EResourceType.Mana,
            _ => false,
        };
}
