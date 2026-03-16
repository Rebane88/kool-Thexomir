using Base;
using Domain.Factions;
using Domain.Map;
using Domain.Military;
using Domain.Resources;

namespace Domain.Game;

public class Kingdom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsEliminated { get; set; }

    public Guid GameId { get; set; }
    public Guid? AppUserId { get; set; } // null = barbarian kingdom
    public Guid? FactionTypeId { get; set; } // null = barbarians have no faction

    // Navigation
    public Game? Game { get; set; }
    public FactionType? FactionType { get; set; }

    public ICollection<Tile>? Tiles { get; set; }
    public ICollection<Army>? Armies { get; set; }
    public ICollection<KingdomResource>? Resources { get; set; }
    public ICollection<TurnLog>? TurnLogs { get; set; }
}
