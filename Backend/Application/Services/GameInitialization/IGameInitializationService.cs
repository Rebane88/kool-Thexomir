using Application.Services.GameInitialization.DTOs.V1;

namespace Application.Services.GameInitialization;

public interface IGameInitializationService
{
    Task<GameStateDto> InitializeGameAsync(Guid gameId);
    Task<GameStateDto> BuildGameStateSnapshotAsync(Guid gameId);
    Task InitializeKingdomResourcesAsync(Guid gameId);
}
