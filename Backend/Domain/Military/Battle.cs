using Base;
using Domain.Game;
using Domain.Map;

namespace Domain.Military;

public class Battle : BaseEntity
{
    public Guid GameId { get; set; }
    public int RoundNumber { get; set; }
    public Guid AttackerKingdomId { get; set; }
    public Guid DefenderKingdomId { get; set; }
    public Guid AttackerTileId { get; set; }
    public Guid DefenderTileId { get; set; }
    public EBattleOutcome Outcome { get; set; }
    public Guid TileCapturedId { get; set; }
    public Guid TileCapturedFromKingdomId { get; set; }
    public DateTime OccurredAt { get; set; }

    // Navigation
    public Domain.Game.Game? Game { get; set; }
    public Kingdom? AttackerKingdom { get; set; }
    public Kingdom? DefenderKingdom { get; set; }
    public Tile? AttackerTile { get; set; }
    public Tile? DefenderTile { get; set; }
    public Tile? TileCaptured { get; set; }
    public Kingdom? TileCapturedFromKingdom { get; set; }
    public ICollection<BattleRound>? BattleRounds { get; set; }
}
