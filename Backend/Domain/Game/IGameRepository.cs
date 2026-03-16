using Base.Contracts;

namespace Domain.Game;

public interface IGameRepository : IBaseRepository<Game>
{
    Task<Game?> GetByLobbyCodeAsync(string code);
    Task<Game?> GetGameWithKingdomsAsync(Guid gameId);
    Task<Game?> GetByIdWithLockAsync(Guid id);
    Task<bool> ExistsByLobbyCodeAsync(string code);
}
