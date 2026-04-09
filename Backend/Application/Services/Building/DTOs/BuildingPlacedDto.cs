namespace Application.Services.Building.DTOs;

public class BuildingPlacedDto
{
    public Guid BuildingId { get; set; }
    public Guid TileId { get; set; }
    public Guid BuildingTypeId { get; set; }
    /// <summary>Culture-independent slug used by the frontend for asset lookup (e.g. "farm").</summary>
    public string BuildingCode { get; set; } = string.Empty;
    /// <summary>Localized display name (depends on the placing request's culture). Use BuildingCode for identity.</summary>
    public string BuildingName { get; set; } = string.Empty;
    public Guid KingdomId { get; set; }
    public Dictionary<string, int> ResourcesAfter { get; set; } = new();
    public List<Guid> ClaimedTileIds { get; set; } = new();
    public bool IsUpgrade { get; set; }
    public int ActionPointsAfter { get; set; }
}
