using Base.Contracts;

namespace Domain.Resources;

public interface IKingdomResourceRepository : IBaseRepository<KingdomResource>
{
    Task<List<KingdomResource>> GetResourcesForKingdomAsync(Guid kingdomId);
    Task<List<KingdomResource>> GetResourcesForKingdomTrackedAsync(Guid kingdomId);
}
