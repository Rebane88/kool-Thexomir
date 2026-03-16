using Base.Contracts;

namespace Domain.Map;

public interface ITileRepository : IBaseRepository<Tile>
{
    Task<List<Tile>> GetTilesForGameAsync(Guid gameId);
}
