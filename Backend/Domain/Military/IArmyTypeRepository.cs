using Base.Contracts;

namespace Domain.Military;

public interface IArmyTypeRepository : IBaseRepository<ArmyType>
{
    Task<IEnumerable<ArmyType>> GetAllWithRequiredBuildingAsync();
}
