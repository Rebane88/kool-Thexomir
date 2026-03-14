using Domain.Map;

namespace Infrastructure.Repositories.Map;

public class TerrainTypeRepository(AppDbContext context)
    : BaseRepository<TerrainType>(context), ITerrainTypeRepository;
