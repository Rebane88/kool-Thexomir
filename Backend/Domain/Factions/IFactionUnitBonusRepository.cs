using Base.Contracts;

namespace Domain.Factions;

public interface IFactionUnitBonusRepository : IBaseRepository<FactionUnitBonus>
{
    Task<IEnumerable<FactionUnitBonus>> GetBonusesForFactionAsync(Guid factionTypeId);
}
