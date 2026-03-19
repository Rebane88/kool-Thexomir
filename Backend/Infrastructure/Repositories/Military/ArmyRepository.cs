using Domain.Military;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Military;

public class ArmyRepository(AppDbContext context)
    : BaseRepository<Army>(context), IArmyRepository
{
    public async Task<Army?> GetArmyAtBuildingForKingdomAsync(Guid buildingId, Guid kingdomId)
        => await context.Armies
            .FirstOrDefaultAsync(a => a.BuildingId == buildingId && a.KingdomId == kingdomId);

    public async Task<Army?> GetArmyWithTypeAsync(Guid armyId)
        => await context.Armies
            .Include(a => a.ArmyType)
            .FirstOrDefaultAsync(a => a.Id == armyId);

    public async Task<IEnumerable<Army>> GetArmiesForKingdomAsync(Guid kingdomId)
        => await context.Armies
            .Where(a => a.KingdomId == kingdomId)
            .ToListAsync();

    public async Task<IEnumerable<Army>> GetArmiesWithTypeForKingdomAsync(Guid kingdomId)
        => await context.Armies
            .Include(a => a.ArmyType)
            .Where(a => a.KingdomId == kingdomId)
            .ToListAsync();

    public async Task<int> GetArmyCountForBuildingAsync(Guid buildingId)
        => await context.Armies.CountAsync(a => a.BuildingId == buildingId);

    public async Task DeleteArmiesForBuildingAsync(Guid buildingId)
    {
        var armies = await context.Armies.Where(a => a.BuildingId == buildingId).ToListAsync();
        context.Armies.RemoveRange(armies);
    }
}
