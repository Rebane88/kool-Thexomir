using Base;
using Domain.Game;
using Domain.Map;

namespace Domain.Military;

public class Army : BaseEntity
{
    public Guid TileId { get; set; }
    public Guid KingdomId { get; set; }
    public bool HasAttackedThisTurn { get; set; }

    // Navigation
    public Tile? Tile { get; set; }
    public Kingdom? Kingdom { get; set; }
}
