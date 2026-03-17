namespace Application.Services.GameInitialization.DTOs;

public class ArmyDto
{
    public Guid Id { get; set; }
    public Guid TileId { get; set; }
    public Guid KingdomId { get; set; }
    public List<ArmyUnitDto> Units { get; set; } = [];
}

public class ArmyUnitDto
{
    public Guid UnitTypeId { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
