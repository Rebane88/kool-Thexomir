namespace Application.Services.Military.DTOs;

public class TroopsTrainedDto
{
    public Guid ArmyId { get; set; }
    public Guid TileId { get; set; }
    public Guid UnitTypeId { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public int QuantityTrained { get; set; }
    public int TotalQuantity { get; set; }
    public Guid KingdomId { get; set; }
    public Dictionary<string, int> ResourcesAfter { get; set; } = new();
}
