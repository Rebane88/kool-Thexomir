using Base;
using Domain.Resources;

namespace Domain.Factions;

public class FactionResourceBonus : BaseEntity
{
    public Guid FactionTypeId { get; set; }
    public EResourceType ResourceType { get; set; }
    public decimal Multiplier { get; set; } // e.g. 1.3 = +30%

    // Navigation
    public FactionType? FactionType { get; set; }
}
