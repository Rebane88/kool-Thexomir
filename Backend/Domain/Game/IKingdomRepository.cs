using Base.Contracts;

namespace Domain.Game;

public interface IKingdomRepository : IBaseRepository<Kingdom>
{
    Task<List<Kingdom>> GetKingdomsForGameAsync(Guid gameId);
    Task<Kingdom?> GetKingdomByUserAndGameAsync(Guid userId, Guid gameId);
    Task<Guid?> GetActiveGameIdForUserAsync(Guid userId);
}
