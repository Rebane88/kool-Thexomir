using Application.Contracts;
using Application.Services.GameInitialization.DTOs;

namespace Application.Services.GameInitialization;

public class GameInitializationService(IUnitOfWork unitOfWork) : IGameInitializationService
{
    public Task<GameStateDto> InitializeGameAsync(Guid gameId)
    {
        throw new NotImplementedException("Pending v6.0 rewrite");
    }

    public Task<GameStateDto> BuildGameStateSnapshotAsync(Guid gameId)
    {
        throw new NotImplementedException("Pending v6.0 rewrite");
    }
}
