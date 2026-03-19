using Base.Contracts;

namespace Domain.Military;

public interface IDeclaredAttackRepository : IBaseRepository<DeclaredAttack>
{
    Task<List<DeclaredAttack>> GetForGameRoundAsync(Guid gameId, int roundNumber);
    Task<HashSet<Guid>> GetLockedTileIdsForGameRoundAsync(Guid gameId, int roundNumber);
    Task DeleteForGameRoundAsync(Guid gameId, int roundNumber);
}
