using Domain.Military;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Military;

public class BuildingUnitTypeRepository(AppDbContext context)
    : BaseRepository<BuildingUnitType>(context), IBuildingUnitTypeRepository
{
    public async Task<BuildingUnitType?> GetByBuildingAndUnitTypeAsync(Guid buildingTypeId, Guid unitTypeId)
        => await context.BuildingUnitTypes
            .FirstOrDefaultAsync(x => x.BuildingTypeId == buildingTypeId && x.UnitTypeId == unitTypeId);

    public async Task<IEnumerable<BuildingUnitType>> GetByBuildingTypeAsync(Guid buildingTypeId)
        => await context.BuildingUnitTypes
            .Where(x => x.BuildingTypeId == buildingTypeId)
            .Include(x => x.UnitType)
            .ToListAsync();
}
