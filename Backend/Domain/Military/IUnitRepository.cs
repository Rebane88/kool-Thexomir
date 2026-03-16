using Base.Contracts;

namespace Domain.Military;

public interface IUnitRepository : IBaseRepository<Unit>
{
    Task<IEnumerable<Unit>> GetUnitsForArmyAsync(Guid armyId);
}
