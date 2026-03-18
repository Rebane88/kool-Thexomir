using System.ComponentModel.DataAnnotations.Schema;
using Base;

namespace Domain.Game;

public class TurnLog : BaseEntity
{
    public int RoundNumber { get; set; }
    public EEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    public DateTime OccurredAt { get; set; }

    public Guid GameId { get; set; }
    public Guid? KingdomId { get; set; }

    // Navigation
    public Game? Game { get; set; }
    public Kingdom? Kingdom { get; set; }
}
