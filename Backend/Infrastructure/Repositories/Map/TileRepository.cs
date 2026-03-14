using Domain.Map;

namespace Infrastructure.Repositories.Map;

public class TileRepository(AppDbContext context)
    : BaseRepository<Tile>(context), ITileRepository;
