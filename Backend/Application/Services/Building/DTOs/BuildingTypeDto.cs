namespace Application.Services.Building.DTOs;

/// <summary>
/// Reference data for a building type. Costs are BASE values;
/// apply the kingdom's faction BuildingCostModifier on the client side.
/// </summary>
public class BuildingTypeDto
{
    public Guid Id { get; set; }
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
