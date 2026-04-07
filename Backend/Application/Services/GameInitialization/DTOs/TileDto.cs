namespace Application.Services.GameInitialization.DTOs;

public class TileDto
{
    public Guid Id { get; set; }
    public int CoordQ { get; set; }
    public int CoordR { get; set; }
    public Guid TerrainTypeId { get; set; }
    public string TerrainName { get; set; } = string.Empty;
    /// <summary>Language-neutral English identifier used by the hex map renderer for color/style switch expressions. Distinct from TerrainName, which is translated for display.</summary>
    public string TerrainKey { get; set; } = string.Empty;
    public Guid? KingdomId { get; set; }
    public bool IsCastle { get; set; }
    public List<BuildingDto> Buildings { get; set; } = [];
}

public class BuildingDto
{
    public Guid Id { get; set; }
    public Guid BuildingTypeId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
}
