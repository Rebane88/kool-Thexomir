using Domain.Military;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Military;

public class ArmyTypeRepository(AppDbContext context)
    : BaseRepository<ArmyType>(context), IArmyTypeRepository
{
    public async Task<IEnumerable<ArmyType>> GetAllWithRequiredBuildingAsync()
    {
        return await context.ArmyTypes
            .Include(at => at.RequiredBuildingType)
            .ToListAsync();
    }
}
