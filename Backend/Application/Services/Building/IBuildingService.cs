using Application.Services.Building.DTOs;
using Base.Contracts;

namespace Application.Services.Building;

public interface IBuildingService
{
    Task<IEnumerable<BuildingTypeDto>> GetBuildingTypesAsync();
    Task<Result<BuildingPlacedDto>> PlaceBuildingAsync(Guid gameId, Guid userId, PlaceBuildingRequest request);
}
