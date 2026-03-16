using Base.Contracts;

namespace Domain.Buildings;

public interface IBuildingRepository : IBaseRepository<Building>
{
    Task<List<Building>> GetBuildingsForKingdomAsync(Guid kingdomId);
}
