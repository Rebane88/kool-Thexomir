using Domain.Factions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Factions;

public class FactionUnitBonusRepository(AppDbContext context)
    : BaseRepository<FactionUnitBonus>(context), IFactionUnitBonusRepository
{
    public async Task<IEnumerable<FactionUnitBonus>> GetBonusesForFactionAsync(Guid factionTypeId)
        => await context.FactionUnitBonuses
            .Where(b => b.FactionTypeId == factionTypeId)
            .ToListAsync();
}
