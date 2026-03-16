using Domain.Factions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Factions;

public class FactionResourceBonusRepository(AppDbContext context)
    : BaseRepository<FactionResourceBonus>(context), IFactionResourceBonusRepository
{
    public async Task<List<FactionResourceBonus>> GetBonusesForFactionAsync(Guid factionTypeId)
    {
        return await Context.Set<FactionResourceBonus>()
            .Where(b => b.FactionTypeId == factionTypeId)
            .ToListAsync();
    }
}
