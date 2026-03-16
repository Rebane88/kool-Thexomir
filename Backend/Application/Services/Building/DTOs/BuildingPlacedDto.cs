namespace Application.Services.Building.DTOs;

public class BuildingPlacedDto
{
    public Guid BuildingId { get; set; }
    public Guid TileId { get; set; }
    public Guid BuildingTypeId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public Guid KingdomId { get; set; }
    public Dictionary<string, int> ResourcesAfter { get; set; } = new();
}
