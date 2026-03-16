using Domain.Resources;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Resources;

public class KingdomResourceRepository(AppDbContext context)
    : BaseRepository<KingdomResource>(context), IKingdomResourceRepository
{
    public async Task<List<KingdomResource>> GetResourcesForKingdomAsync(Guid kingdomId)
    {
        return await Context.Set<KingdomResource>()
            .Where(kr => kr.KingdomId == kingdomId)
            .ToListAsync();
    }

    public async Task<List<KingdomResource>> GetResourcesForKingdomTrackedAsync(Guid kingdomId)
    {
        return await Context.Set<KingdomResource>()
            .Where(kr => kr.KingdomId == kingdomId)
            .AsTracking()
            .ToListAsync();
    }
}
