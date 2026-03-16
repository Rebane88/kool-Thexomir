using Domain.Buildings;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Buildings;

public class BuildingRepository(AppDbContext context)
    : BaseRepository<Building>(context), IBuildingRepository
{
    public async Task<List<Building>> GetBuildingsForKingdomAsync(Guid kingdomId)
    {
        return await Context.Set<Building>()
            .Where(b => b.Tile != null && b.Tile.KingdomId == kingdomId)
            .Include(b => b.BuildingType)
            .ToListAsync();
    }
}
