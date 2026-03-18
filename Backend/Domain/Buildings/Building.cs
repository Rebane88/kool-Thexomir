using Base;
using Domain.Game;
using Domain.Map;
using Domain.Military;

namespace Domain.Buildings;

public class Building : BaseEntity
{
    public Guid TileId { get; set; }
    public Guid BuildingTypeId { get; set; }
    public Guid KingdomId { get; set; }
    public int BuiltOnRound { get; set; }
    public DateTime BuiltAt { get; set; }

    // Navigation
    public Tile? Tile { get; set; }
    public BuildingType? BuildingType { get; set; }
    public Kingdom? Kingdom { get; set; }
    public ICollection<Army>? Armies { get; set; }
}
