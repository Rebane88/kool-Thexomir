using Base;
using Domain.Game;

namespace Domain.Resources;

public class KingdomResource : BaseEntity
{
    public Guid KingdomId { get; set; }
    public ResourceType ResourceType { get; set; }
    public decimal Amount { get; set; } // decimal(18,2) configured in Fluent API

    // Navigation
    public Kingdom? Kingdom { get; set; }
}
