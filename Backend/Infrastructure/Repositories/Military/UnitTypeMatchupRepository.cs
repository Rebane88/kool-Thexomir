using Domain.Military;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Military;

public class UnitTypeMatchupRepository(AppDbContext context)
    : BaseRepository<UnitTypeMatchup>(context), IUnitTypeMatchupRepository
{
    public async Task<IEnumerable<UnitTypeMatchup>> GetAllMatchupsAsync()
        => await context.UnitTypeMatchups.ToListAsync();
}
