using Base.Contracts;

namespace Domain.Game;

public interface IGameRepository : IBaseRepository<Game>
{
    Task<Game?> GetByLobbyCodeAsync(string code);
    Task<Game?> GetLobbyWithPlayersAsync(Guid gameId);
    Task<Game?> GetByIdForUpdateAsync(Guid id);
    Task<bool> ExistsByLobbyCodeAsync(string code);
}
