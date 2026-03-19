using Base.Contracts;

namespace Domain.Military;

public interface IArmyRepository : IBaseRepository<Army>
{
    Task<Army?> GetArmyAtBuildingForKingdomAsync(Guid buildingId, Guid kingdomId);
    Task<Army?> GetArmyWithTypeAsync(Guid armyId);
    Task<IEnumerable<Army>> GetArmiesForKingdomAsync(Guid kingdomId);
    Task<IEnumerable<Army>> GetArmiesWithTypeForKingdomAsync(Guid kingdomId);
    Task<int> GetArmyCountForBuildingAsync(Guid buildingId);
    Task DeleteArmiesForBuildingAsync(Guid buildingId);
}
