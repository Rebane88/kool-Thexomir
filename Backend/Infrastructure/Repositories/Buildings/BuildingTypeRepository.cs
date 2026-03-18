using Domain.Buildings;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Buildings;

public class BuildingTypeRepository(AppDbContext context)
    : BaseRepository<BuildingType>(context), IBuildingTypeRepository
{
    public async Task<IEnumerable<BuildingType>> GetAllWithPrerequisiteAsync()
    {
        return await Context.Set<BuildingType>()
            .Include(bt => bt.UnlockedByBuildingType)
            .OrderBy(bt => bt.Chain)
            .ThenBy(bt => bt.Tier)
            .ToListAsync();
    }
}
