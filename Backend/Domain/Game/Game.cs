using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base;
using Domain.Map;
using Domain.Military;

namespace Domain.Game;

public class Game : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public EGameStatus Status { get; set; }
    public int RoundNumber { get; set; }
    public EGamePhase CurrentPhase { get; set; }
    public EWinCondition WinCondition { get; set; } = EWinCondition.Elimination;
    [Range(2, 4)]
    public int MaxPlayers { get; set; }
    public string LobbyCode { get; set; } = string.Empty;
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    public Guid? HostUserId { get; set; }
    public Guid? CurrentTurnKingdomId { get; set; }
    public int MaxRounds { get; set; } = 100;
    public int BaseActionPoints { get; set; } = 4;
    public decimal HealPercent { get; set; } = 0.10m;
    public int SpinCostGold { get; set; } = 30;
    [Column(TypeName = "jsonb")]
    public string SlotOutcomeWeights { get; set; } = "[5,25,30,25,15]";
    public int? TurnTimeLimit { get; set; }
    public DateTime? TurnDeadline { get; set; }
    public int? RemainingActionPoints { get; set; }
    public Guid? WinnerKingdomId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public uint xmin { get; set; }

    // Navigation
    public Kingdom? CurrentTurnKingdom { get; set; }
    public Kingdom? WinnerKingdom { get; set; }
    public ICollection<Kingdom>? Kingdoms { get; set; }
    public ICollection<Tile>? Tiles { get; set; }
    public ICollection<TurnLog>? TurnLogs { get; set; }
    public ICollection<Battle>? Battles { get; set; }
}
