using Base.Contracts;

namespace Domain.Factions;

public interface IFactionResourceBonusRepository : IBaseRepository<FactionResourceBonus>
{
    Task<List<FactionResourceBonus>> GetBonusesForFactionAsync(Guid factionTypeId);
}
