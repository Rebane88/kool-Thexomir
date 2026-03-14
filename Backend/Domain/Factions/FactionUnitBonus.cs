using Base;
using Domain.Military;

namespace Domain.Factions;

public class FactionUnitBonus : BaseEntity
{
    public Guid FactionTypeId { get; set; }
    public Guid? UnitTypeId { get; set; } // null = bonus applies to all units
    public decimal Multiplier { get; set; } // e.g. 1.2 = +20%

    // Navigation
    public FactionType? FactionType { get; set; }
    public UnitType? UnitType { get; set; }
}
