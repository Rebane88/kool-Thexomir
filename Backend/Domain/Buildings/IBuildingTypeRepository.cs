using Base.Contracts;

namespace Domain.Buildings;

public interface IBuildingTypeRepository : IBaseRepository<BuildingType>
{
    Task<IEnumerable<BuildingType>> GetAllWithPrerequisiteAsync();
}
