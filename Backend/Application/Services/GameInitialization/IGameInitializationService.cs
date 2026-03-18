using Application.Services.GameInitialization.DTOs;

namespace Application.Services.GameInitialization;

public interface IGameInitializationService
{
    Task<GameStateDto> InitializeGameAsync(Guid gameId);
    Task<GameStateDto> BuildGameStateSnapshotAsync(Guid gameId);
    Task InitializeKingdomResourcesAsync(Guid gameId);
}
