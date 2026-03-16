using Application.Contracts;
using Application.Services.Turn.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Resources;

namespace Application.Services.Turn;

public class TurnService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : ITurnService
{
    public async Task<Result<TurnAdvancedDto>> EndTurnAsync(Guid gameId, Guid userId)
    {
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<TurnAdvancedDto>.Fail(guardResult.Error!);

        var (game, currentKingdom) = guardResult.Value!;

        // Log the end-turn action with CURRENT turn number before any changes
        await unitOfWork.TurnLogs.AddAsync(new TurnLog
        {
            GameId = gameId,
            KingdomId = currentKingdom.Id,
            TurnNumber = game.TurnNumber,
            Action = "EndTurn"
        });

        // Load kingdoms for domain method
        game.Kingdoms = (await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId)).ToList();

        // Domain does the turn advancement
        var nextKingdom = game.AdvanceTurn();

        await unitOfWork.Games.UpdateAsync(game);

        // Calculate and apply income for the next player
        var tiles = await unitOfWork.Tiles.GetTilesWithBuildingsAndTerrainForKingdomAsync(nextKingdom.Id);
        var factionBonuses = await unitOfWork.FactionResourceBonuses
            .GetBonusesForFactionAsync(nextKingdom.FactionTypeId!.Value);
        var bonusLookup = factionBonuses.ToDictionary(b => b.ResourceType, b => b.Multiplier);

        var income = Game.CalculateIncome(tiles, bonusLookup);

        var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(nextKingdom.Id);
        foreach (var (type, amount) in income.Where(i => i.Value > 0))
        {
            var resource = resources.Single(r => r.ResourceType == type);
            resource.Amount += amount;
            await unitOfWork.KingdomResources.UpdateAsync(resource);
        }

        await unitOfWork.CommitAsync();

        return Result<TurnAdvancedDto>.Ok(new TurnAdvancedDto
        {
            NewKingdomId = nextKingdom.Id,
            TurnNumber = game.TurnNumber,
            IncomeApplied = income.Where(i => i.Value > 0).ToDictionary(i => i.Key.ToString(), i => i.Value)
        });
    }
}
