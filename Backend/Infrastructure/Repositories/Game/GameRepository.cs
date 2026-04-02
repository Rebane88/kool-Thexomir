using Domain.Game;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Game;

public class GameRepository(AppDbContext context)
    : BaseRepository<Domain.Game.Game>(context), IGameRepository
{
    public async Task<Domain.Game.Game?> GetByLobbyCodeAsync(string code)
    {
        return await Context.Games
            .FirstOrDefaultAsync(g => g.LobbyCode == code);
    }

    public async Task<Domain.Game.Game?> GetGameWithKingdomsAsync(Guid gameId)
    {
        return await Context.Games
            .Include(g => g.Kingdoms!)
                .ThenInclude(k => k.FactionType)
            .FirstOrDefaultAsync(g => g.Id == gameId);
    }

    public async Task<Domain.Game.Game?> GetByIdWithLockAsync(Guid id)
    {
        return await Context.Games
            .AsTracking()
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<bool> ExistsByLobbyCodeAsync(string code)
    {
        return await Context.Games
            .AnyAsync(g => g.LobbyCode == code);
    }

    public async Task<List<Domain.Game.Game>> GetInProgressGamesWithExpiredTurnsAsync()
    {
        return await Context.Games
            .AsTracking()
            .Include(g => g.Kingdoms!)
            .Where(g => g.Status == EGameStatus.InProgress
                        && g.TurnDeadline != null
                        && g.TurnDeadline < DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<List<Domain.Game.Game>> GetStaleLobbiesAsync(DateTime olderThan)
    {
        return await Context.Games
            .AsTracking()
            .Where(g => g.Status == EGameStatus.Lobby && g.CreatedAt < olderThan)
            .ToListAsync();
    }
}
