namespace Application.Services.GameInitialization.DTOs.V1;

public class TileDto
{
    public Guid Id { get; set; }
    public int CoordQ { get; set; }
    public int CoordR { get; set; }
    public Guid TerrainTypeId { get; set; }
    public string TerrainName { get; set; } = string.Empty;
    /// <summary>Culture-independent slug used by the hex map renderer for color/style/texture lookups (e.g. "plains"). Distinct from TerrainName, which is translated for display.</summary>
    public string TerrainCode { get; set; } = string.Empty;
    public Guid? KingdomId { get; set; }
    public bool IsCastle { get; set; }
    public List<BuildingDto> Buildings { get; set; } = [];
}

public class BuildingDto
{
    public Guid Id { get; set; }
    public Guid BuildingTypeId { get; set; }
    /// <summary>Culture-independent slug used by the frontend for asset lookup (e.g. "farm").</summary>
    public string BuildingCode { get; set; } = string.Empty;
    /// <summary>Localized display name. Use BuildingCode for identity.</summary>
    public string BuildingName { get; set; } = string.Empty;
}
