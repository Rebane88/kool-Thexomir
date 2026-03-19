using Base;
using Domain.Game;
using Domain.Map;

namespace Domain.Military;

public class DeclaredAttack : BaseEntity
{
    public Guid GameId { get; set; }
    public int RoundNumber { get; set; }
    public Guid AttackerKingdomId { get; set; }
    public Guid DefenderKingdomId { get; set; }
    public Guid TargetTileId { get; set; }
    public Guid RiskedTileId { get; set; }

    // Army selection/lineup storage (comma-separated GUIDs)
    public string? AttackerSelectedArmyIds { get; set; }
    public string? DefenderSelectedArmyIds { get; set; }

    // Navigation
    public Domain.Game.Game? Game { get; set; }
    public Kingdom? AttackerKingdom { get; set; }
    public Kingdom? DefenderKingdom { get; set; }
    public Tile? TargetTile { get; set; }
    public Tile? RiskedTile { get; set; }
}
