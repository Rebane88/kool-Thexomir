using Base;
using Domain.Buildings;
using Domain.Factions;
using Domain.Map;
using Domain.Military;
using Domain.Resources;

namespace Domain.Game;

public class Kingdom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public EKingdomStatus Status { get; set; }
    public int TurnOrder { get; set; }

    public Guid GameId { get; set; }
    public Guid? AppUserId { get; set; }
    public Guid FactionTypeId { get; set; }

    public DateTime? DefeatedAt { get; set; }
    public DateTime JoinedAt { get; set; }

    // Navigation
    public Game? Game { get; set; }
    public FactionType? FactionType { get; set; }
    public ICollection<Tile>? Tiles { get; set; }
    public ICollection<Building>? Buildings { get; set; }
    public ICollection<Army>? Armies { get; set; }
    public ICollection<KingdomResource>? Resources { get; set; }
    public ICollection<TurnLog>? TurnLogs { get; set; }
}
