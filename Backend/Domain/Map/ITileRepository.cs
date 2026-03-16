using Base.Contracts;

namespace Domain.Map;

public interface ITileRepository : IBaseRepository<Tile>
{
    Task<List<Tile>> GetTilesForGameAsync(Guid gameId);
    Task<List<Tile>> GetTilesWithBuildingsAndTerrainForKingdomAsync(Guid kingdomId);
    Task<List<Tile>> GetTilesForKingdomAsync(Guid kingdomId);
    Task<List<Tile>> GetTilesWithBuildingsForGameAsync(Guid gameId);
}
