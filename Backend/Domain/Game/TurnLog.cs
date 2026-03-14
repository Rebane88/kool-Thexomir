using Base;

namespace Domain.Game;

public class TurnLog : BaseEntity
{
    public int TurnNumber { get; set; }
    public string Action { get; set; } = string.Empty;

    public Guid GameId { get; set; }
    public Guid? KingdomId { get; set; } // null = system event

    // Navigation
    public Game? Game { get; set; }
    public Kingdom? Kingdom { get; set; }
}
