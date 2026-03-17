namespace Application.Services.Military.DTOs;

public class UnitTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BaseStrength { get; set; }
    public int GoldCost { get; set; }
    public int FoodCost { get; set; }
    public int WoodCost { get; set; }
    public int StoneCost { get; set; }
    public int ManaCost { get; set; }
    public int Upkeep { get; set; }
    public string? Description { get; set; }
    public List<Guid> ProducedByBuildingTypeIds { get; set; } = [];
}
