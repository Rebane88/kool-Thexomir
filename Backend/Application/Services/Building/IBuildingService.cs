using Application.Services.Building.DTOs;
using Base.Contracts;

namespace Application.Services.Building;

public interface IBuildingService
{
    Task<IEnumerable<BuildingTypeDto>> GetBuildingTypesAsync(Guid gameId, Guid userId);
    Task<Result<BuildingPlacedDto>> PlaceBuildingAsync(Guid gameId, Guid userId, PlaceBuildingRequest request);
}
