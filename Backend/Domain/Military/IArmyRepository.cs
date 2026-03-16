using Base.Contracts;

namespace Domain.Military;

public interface IArmyRepository : IBaseRepository<Army>
{
    Task<Army?> GetArmyOnTileForKingdomAsync(Guid tileId, Guid kingdomId);
    Task<Army?> GetArmyWithUnitsAsync(Guid armyId);
    Task<IEnumerable<Army>> GetArmiesForKingdomAsync(Guid kingdomId);
    Task<Army?> GetEnemyArmyOnTileAsync(Guid tileId, Guid excludeKingdomId);
}
