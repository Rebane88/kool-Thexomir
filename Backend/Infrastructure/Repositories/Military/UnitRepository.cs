using Domain.Military;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Military;

public class UnitRepository(AppDbContext context)
    : BaseRepository<Unit>(context), IUnitRepository
{
    public async Task<IEnumerable<Unit>> GetUnitsForArmyAsync(Guid armyId)
        => await context.Units
            .Where(u => u.ArmyId == armyId)
            .Include(u => u.UnitType)
            .ToListAsync();
}
