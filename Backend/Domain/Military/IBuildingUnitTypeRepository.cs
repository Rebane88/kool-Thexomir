using Base.Contracts;

namespace Domain.Military;

public interface IBuildingUnitTypeRepository : IBaseRepository<BuildingUnitType>
{
    Task<BuildingUnitType?> GetByBuildingAndUnitTypeAsync(Guid buildingTypeId, Guid unitTypeId);
    Task<IEnumerable<BuildingUnitType>> GetByBuildingTypeAsync(Guid buildingTypeId);
}
