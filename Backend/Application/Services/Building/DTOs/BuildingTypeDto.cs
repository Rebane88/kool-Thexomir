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
    public int GoldCost { get; set; }
    public int WoodCost { get; set; }
    public int StoneCost { get; set; }
    public int ManaCost { get; set; }
    public int FoodYield { get; set; }
    public int WoodYield { get; set; }
    public int StoneYield { get; set; }
    public int GoldYield { get; set; }
    public int ManaYield { get; set; }
    public string? Description { get; set; }
    public Guid? PrerequisiteBuildingTypeId { get; set; }
    public string? PrerequisiteBuildingName { get; set; }
}
