using Application.Contracts;
using Application.Services.GameInitialization.DTOs;
using Domain.Resources;

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

    public async Task InitializeKingdomResourcesAsync(Guid gameId)
    {
        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);

        foreach (var kingdom in kingdoms)
        {
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId);
            if (factionType is null) continue;

            var resources = ResourceInitializer.CreateStartingResources(
                kingdom.Id,
                factionType.StartingBonusResource,
                factionType.StartingBonusAmount);

            foreach (var resource in resources)
            {
                await unitOfWork.KingdomResources.AddAsync(resource);
            }
        }

        await unitOfWork.CommitAsync();
    }
}
