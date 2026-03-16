namespace Application.Services.GameInitialization.DTOs;

public class TileDto
{
    public Guid Id { get; set; }
    public int CoordQ { get; set; }
    public int CoordR { get; set; }
    public Guid TerrainTypeId { get; set; }
    public string TerrainName { get; set; } = string.Empty;
    public Guid? KingdomId { get; set; }
    public List<BuildingDto> Buildings { get; set; } = [];
}

public class BuildingDto
{
    public Guid Id { get; set; }
    public Guid BuildingTypeId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
}
