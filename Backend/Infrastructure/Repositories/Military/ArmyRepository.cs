using Domain.Military;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Military;

public class ArmyRepository(AppDbContext context)
    : BaseRepository<Army>(context), IArmyRepository
{
    public async Task<Army?> GetArmyOnTileForKingdomAsync(Guid tileId, Guid kingdomId)
        => await context.Armies
            .FirstOrDefaultAsync(a => a.TileId == tileId && a.KingdomId == kingdomId);

    public async Task<Army?> GetArmyWithUnitsAsync(Guid armyId)
        => await context.Armies
            .FirstOrDefaultAsync(a => a.Id == armyId);

    public async Task<IEnumerable<Army>> GetArmiesForKingdomAsync(Guid kingdomId)
        => await context.Armies
            .Where(a => a.KingdomId == kingdomId)
            .ToListAsync();

    public async Task<Army?> GetEnemyArmyOnTileAsync(Guid tileId, Guid excludeKingdomId)
        => await context.Armies
            .FirstOrDefaultAsync(a => a.TileId == tileId && a.KingdomId != excludeKingdomId);

    public async Task<IEnumerable<Army>> GetArmiesWithUnitsForKingdomAsync(Guid kingdomId)
        => await context.Armies
            .Where(a => a.KingdomId == kingdomId)
            .ToListAsync();
}
