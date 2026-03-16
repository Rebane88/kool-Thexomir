using Base;
using Domain.Buildings;

namespace Domain.Military;

public class BuildingUnitType : BaseEntity
{
    public Guid BuildingTypeId { get; set; }
    public Guid UnitTypeId { get; set; }

    // Navigation
    public BuildingType? BuildingType { get; set; }
    public UnitType? UnitType { get; set; }
}
