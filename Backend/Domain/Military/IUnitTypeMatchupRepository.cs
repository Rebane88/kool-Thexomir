using Base.Contracts;

namespace Domain.Military;

public interface IUnitTypeMatchupRepository : IBaseRepository<UnitTypeMatchup>
{
    Task<IEnumerable<UnitTypeMatchup>> GetAllMatchupsAsync();
}
