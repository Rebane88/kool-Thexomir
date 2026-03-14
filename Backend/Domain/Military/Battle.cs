using Base;
using Domain.Game;
using Domain.Map;

namespace Domain.Military;

public class Battle : BaseEntity
{
    public Guid AttackerArmyId { get; set; }
    public Guid DefenderArmyId { get; set; }
    public Guid? WinnerId { get; set; } // null = draw
    public Guid TileId { get; set; }
    public Guid GameId { get; set; }
    public int TurnNumber { get; set; }

    // Navigation
    public Army? AttackerArmy { get; set; }
    public Army? DefenderArmy { get; set; }
    public Kingdom? Winner { get; set; }
    public Tile? Tile { get; set; }
    public Domain.Game.Game? Game { get; set; }
}
