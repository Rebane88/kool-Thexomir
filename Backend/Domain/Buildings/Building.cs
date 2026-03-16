using Base;
using Domain.Map;

namespace Domain.Buildings;

public class Building : BaseEntity
{
    public Guid TileId { get; set; }
    public Guid BuildingTypeId { get; set; }
    public bool HasTrainedThisTurn { get; set; }

    // Navigation
    public Tile? Tile { get; set; }
    public BuildingType? BuildingType { get; set; }
}
