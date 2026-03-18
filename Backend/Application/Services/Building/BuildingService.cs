using Application.Contracts;
using Application.Services.Building.DTOs;
using Base.Contracts;

namespace Application.Services.Building;

public class BuildingService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : IBuildingService
{
    public Task<IEnumerable<BuildingTypeDto>> GetBuildingTypesAsync()
    {
        throw new NotImplementedException("Pending v6.0 rewrite");
    }

    public Task<Result<BuildingPlacedDto>> PlaceBuildingAsync(Guid gameId, Guid userId, PlaceBuildingRequest request)
    {
        throw new NotImplementedException("Pending v6.0 rewrite");
    }
}
