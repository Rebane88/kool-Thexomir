using Base;

namespace Domain.Game;

public class GameEvent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal ResourceEffect { get; set; } // multiplier or flat amount
}
