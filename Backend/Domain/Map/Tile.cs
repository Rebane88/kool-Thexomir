using Base;
using Domain.Buildings;
using Domain.Game;
using Domain.Military;

namespace Domain.Map;

public class Tile : BaseEntity
{
    public int CoordQ { get; set; } // hex coordinate
    public int CoordR { get; set; } // hex coordinate

    public Guid GameId { get; set; } // direct FK for (GameId, CoordQ, CoordR) uniqueness constraint
    public Guid TerrainTypeId { get; set; }
    public Guid? KingdomId { get; set; } // null = unclaimed

    // Navigation
    public Domain.Game.Game? Game { get; set; }
    public TerrainType? TerrainType { get; set; }
    public Kingdom? Kingdom { get; set; }

    public ICollection<Building>? Buildings { get; set; }
    public ICollection<Army>? Armies { get; set; }
}
