using Domain.Game;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Game;

public class KingdomRepository(AppDbContext context)
    : BaseRepository<Kingdom>(context), IKingdomRepository
{
    public async Task<List<Kingdom>> GetKingdomsForGameAsync(Guid gameId)
    {
        return await Context.Kingdoms
            .Where(k => k.GameId == gameId)
            .Include(k => k.FactionType)
            .ToListAsync();
    }

    public async Task<Kingdom?> GetKingdomByUserAndGameAsync(Guid userId, Guid gameId)
    {
        return await Context.Kingdoms
            .AsTracking()
            .FirstOrDefaultAsync(k => k.AppUserId == userId && k.GameId == gameId);
    }
}
