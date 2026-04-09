namespace Application.Services.Building.DTOs;

/// <summary>
/// Reference data for a building type. Costs are faction-modified values
/// for the requesting player's kingdom. Base costs are adjusted server-side
/// using the player's faction BuildingCostModifier.
/// </summary>
public class BuildingTypeDto
{
    public Guid Id { get; set; }
    /// <summary>Culture-independent slug used by the frontend for asset lookup (e.g. "farm").</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Tier { get; set; }
    public string Chain { get; set; } = string.Empty;
    public int CostGold { get; set; }
    public int CostFood { get; set; }
    public int CostWood { get; set; }
    public int CostStone { get; set; }
    public int CostMana { get; set; }
    public int BaseYieldGold { get; set; }
    public int BaseYieldFood { get; set; }
    public int BaseYieldWood { get; set; }
    public int BaseYieldStone { get; set; }
    public int BaseYieldMana { get; set; }
    public string? Description { get; set; }
    public Guid? UnlockedByBuildingTypeId { get; set; }
    public string? UnlockedByBuildingName { get; set; }
    public int ArmyCapacity { get; set; }
}
