using Domain.Map;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Map;

public class TileRepository(AppDbContext context)
    : BaseRepository<Tile>(context), ITileRepository
{
    public async Task<List<Tile>> GetTilesForGameAsync(Guid gameId)
    {
        return await Context.Set<Tile>()
            .Where(t => t.GameId == gameId)
            .Include(t => t.Buildings!).ThenInclude(b => b.BuildingType)
            .Include(t => t.TerrainType)
            .AsTracking()
            .ToListAsync();
    }

    public async Task<List<Tile>> GetTilesWithBuildingsAndTerrainForKingdomAsync(Guid kingdomId)
    {
        return await Context.Set<Tile>()
            .Where(t => t.KingdomId == kingdomId)
            .Include(t => t.Buildings!).ThenInclude(b => b.BuildingType)
            .Include(t => t.TerrainType)
            .AsTracking()
            .ToListAsync();
    }

    public async Task<List<Tile>> GetTilesForKingdomAsync(Guid kingdomId)
    {
        return await Context.Set<Tile>()
            .Where(t => t.KingdomId == kingdomId)
            .ToListAsync();
    }

    public async Task<List<Tile>> GetTilesWithBuildingsForGameAsync(Guid gameId)
    {
        return await Context.Set<Tile>()
            .Where(t => t.GameId == gameId)
            .Include(t => t.TerrainType)
            .Include(t => t.Buildings!)
                .ThenInclude(b => b.BuildingType)
            .ToListAsync();
    }
}
