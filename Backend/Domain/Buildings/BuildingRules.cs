using Domain.Map;
using Domain.Resources;

namespace Domain.Buildings;

public static class BuildingRules
{
    /// <summary>
    /// Validates whether a building can be placed/upgraded on a tile.
    /// Returns null on success, error message on failure.
    /// </summary>
    public static string? ValidatePlacement(
        BuildingType requestedType,
        Building? existingBuilding,
        Tile tile,
        Guid kingdomId)
    {
        if (tile.KingdomId != kingdomId)
            return "Tile is not owned by your kingdom.";

        if (tile.IsCastle)
            return "Cannot build on castle tile.";

        if (requestedType.Tier <= 1)
        {
            if (existingBuilding is not null)
                return "Tile already has a building. Use upgrade instead.";
        }
        else
        {
            if (existingBuilding is null)
                return "Tile has no building to upgrade.";

            if (existingBuilding.BuildingTypeId != requestedType.UnlockedByBuildingTypeId)
                return "Existing building is not the prerequisite for this upgrade.";
        }

        return null;
    }

    /// <summary>
    /// Applies faction building cost modifier with ceiling rounding.
    /// Returns 0 if baseCost is 0.
    /// </summary>
    public static int ApplyFactionCostModifier(int baseCost, decimal factionModifier)
    {
        if (baseCost == 0) return 0;
        return (int)Math.Ceiling(baseCost * factionModifier);
    }

    /// <summary>
    /// Applies faction training cost modifier with ceiling rounding.
    /// Same formula as ApplyFactionCostModifier -- exists for semantic clarity.
    /// </summary>
    public static int ApplyTrainingCostModifier(int baseCost, decimal factionTrainingCostModifier)
    {
        if (baseCost == 0) return 0;
        return (int)Math.Ceiling(baseCost * factionTrainingCostModifier);
    }

    /// <summary>
    /// Checks whether kingdom resources can cover the modified building cost.
    /// </summary>
    public static bool CanAfford(
        List<KingdomResource> resources,
        BuildingType buildingType,
        decimal factionBuildingCostModifier)
    {
        var costs = GetModifiedBuildingCosts(buildingType, factionBuildingCostModifier);

        foreach (var (resourceType, cost) in costs)
        {
            if (cost == 0) continue;
            var resource = resources.FirstOrDefault(r => r.ResourceType == resourceType);
            if (resource is null || resource.Amount < cost)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Deducts building cost from kingdom resources (all-or-nothing).
    /// Returns null on success, error message on failure.
    /// </summary>
    public static string? DeductBuildingCost(
        List<KingdomResource> resources,
        BuildingType buildingType,
        decimal factionBuildingCostModifier)
    {
        var costs = GetModifiedBuildingCosts(buildingType, factionBuildingCostModifier);
        return DeductResourceCost(resources, costs);
    }

    /// <summary>
    /// Generic resource deduction (all-or-nothing). Floors each resource at 0.
    /// Returns null on success, error message on failure.
    /// </summary>
    public static string? DeductResourceCost(
        List<KingdomResource> resources,
        Dictionary<EResourceType, int> costs)
    {
        // Validate all-or-nothing: check every cost before deducting
        foreach (var (resourceType, cost) in costs)
        {
            if (cost == 0) continue;
            var resource = resources.FirstOrDefault(r => r.ResourceType == resourceType);
            if (resource is null || resource.Amount < cost)
                return $"Insufficient {resourceType}. Need {cost}, have {resource?.Amount ?? 0}.";
        }

        // Deduct
        foreach (var (resourceType, cost) in costs)
        {
            if (cost == 0) continue;
            var resource = resources.First(r => r.ResourceType == resourceType);
            resource.Amount = Math.Max(0, resource.Amount - cost);
            resource.UpdatedAt = DateTime.UtcNow;
        }

        return null;
    }

    // --- Private helpers ---

    private static Dictionary<EResourceType, int> GetModifiedBuildingCosts(
        BuildingType buildingType,
        decimal factionModifier)
    {
        return new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, ApplyFactionCostModifier(buildingType.CostGold, factionModifier) },
            { EResourceType.Food, ApplyFactionCostModifier(buildingType.CostFood, factionModifier) },
            { EResourceType.Wood, ApplyFactionCostModifier(buildingType.CostWood, factionModifier) },
            { EResourceType.Stone, ApplyFactionCostModifier(buildingType.CostStone, factionModifier) },
            { EResourceType.Mana, ApplyFactionCostModifier(buildingType.CostMana, factionModifier) },
        };
    }
}
