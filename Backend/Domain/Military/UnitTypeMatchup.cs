using Base;

namespace Domain.Military;

public class UnitTypeMatchup : BaseEntity
{
    public Guid AttackerTypeId { get; set; }
    public Guid DefenderTypeId { get; set; }
    public decimal Multiplier { get; set; }

    // Navigation
    public UnitType? AttackerType { get; set; }
    public UnitType? DefenderType { get; set; }
}
