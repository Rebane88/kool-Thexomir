using Base;

namespace Domain.Military;

public class Unit : BaseEntity
{
    public Guid ArmyId { get; set; }
    public Guid UnitTypeId { get; set; }
    public int Quantity { get; set; }

    // Navigation
    public Army? Army { get; set; }
    public UnitType? UnitType { get; set; }
}
