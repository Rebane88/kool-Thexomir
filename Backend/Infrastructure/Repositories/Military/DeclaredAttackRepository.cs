using Domain.Military;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Military;

public class DeclaredAttackRepository(AppDbContext context)
    : BaseRepository<DeclaredAttack>(context), IDeclaredAttackRepository
{
    public async Task<List<DeclaredAttack>> GetForGameRoundAsync(Guid gameId, int roundNumber)
    {
        return await Context.DeclaredAttacks
            .AsTracking()
            .Include(d => d.TargetTile)
            .Include(d => d.RiskedTile)
            .Where(d => d.GameId == gameId && d.RoundNumber == roundNumber)
            .ToListAsync();
    }

    public async Task<HashSet<Guid>> GetLockedTileIdsForGameRoundAsync(Guid gameId, int roundNumber)
    {
        var attacks = await Context.DeclaredAttacks
            .Where(d => d.GameId == gameId && d.RoundNumber == roundNumber)
            .Select(d => new { d.TargetTileId, d.RiskedTileId })
            .ToListAsync();

        var lockedIds = new HashSet<Guid>();
        foreach (var attack in attacks)
        {
            lockedIds.Add(attack.TargetTileId);
            lockedIds.Add(attack.RiskedTileId);
        }

        return lockedIds;
    }

    public async Task DeleteForGameRoundAsync(Guid gameId, int roundNumber)
    {
        await Context.DeclaredAttacks
            .Where(d => d.GameId == gameId && d.RoundNumber == roundNumber)
            .ExecuteDeleteAsync();
    }
}
