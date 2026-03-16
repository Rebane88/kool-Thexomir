using Application.Contracts;
using Application.Services.Turn.DTOs;
using Application.Services.WinCondition.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.Turn;

public class TurnService(IUnitOfWork unitOfWork, IGameGuard gameGuard, IServiceProvider serviceProvider) : ITurnService
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

        // Reset per-turn flags for the kingdom whose turn just ended
        var endingKingdomArmies = await unitOfWork.Armies.GetArmiesForKingdomAsync(currentKingdom.Id);
        foreach (var army in endingKingdomArmies)
        {
            if (army.HasAttackedThisTurn)
            {
                army.HasAttackedThisTurn = false;
                await unitOfWork.Armies.UpdateAsync(army);
            }
        }

        var endingKingdomBuildings = await unitOfWork.Buildings.GetBuildingsForKingdomAsync(currentKingdom.Id);
        foreach (var building in endingKingdomBuildings)
        {
            if (building.HasTrainedThisTurn)
            {
                building.HasTrainedThisTurn = false;
                await unitOfWork.Buildings.UpdateAsync(building);
            }
        }

        // Domain does the turn advancement
        var nextKingdom = game.AdvanceTurn();

        await unitOfWork.Games.UpdateAsync(game);

        // --- Phase 13: Score win condition check ---
        GameOverDto? gameOverDto = null;
        var income = new Dictionary<EResourceType, int>();

        if (game.WinCondition == EWinCondition.Score
            && game.MaxTurnCount.HasValue
            && game.TurnNumber > game.MaxTurnCount.Value)
        {
            var checker = serviceProvider.GetRequiredKeyedService<IWinConditionChecker>(EWinCondition.Score);
            var allKingdoms = game.Kingdoms!.ToList();
            var allTiles = await unitOfWork.Tiles.GetTilesWithBuildingsForGameAsync(game.Id);

            // Load armies with units for each kingdom (needed for score calculation)
            foreach (var k in allKingdoms.Where(k => !k.IsEliminated))
            {
                k.Armies = (await unitOfWork.Armies.GetArmiesWithUnitsForKingdomAsync(k.Id)).ToList();
            }

            var winResult = game.CheckWinCondition(checker, allKingdoms, allTiles);

            if (winResult?.GameOver == true)
            {
                await unitOfWork.Games.UpdateAsync(game);
                gameOverDto = new GameOverDto
                {
                    GameId = game.Id,
                    WinnerKingdomId = winResult.WinnerKingdomId,
                    WinConditionType = winResult.WinConditionType.ToString(),
                    FinalScores = allKingdoms.Select(k => new KingdomScoreDto
                    {
                        KingdomId = k.Id,
                        KingdomName = k.Name,
                        Score = ScoreChecker.CalculateScore(k, allTiles),
                        TilesOwned = allTiles.Count(t => t.KingdomId == k.Id),
                        IsEliminated = k.IsEliminated
                    }).OrderByDescending(s => s.Score).ToList(),
                    EliminationOrder = allKingdoms
                        .Where(k => k.IsEliminated)
                        .OrderBy(k => k.UpdatedAt)
                        .Select(k => k.Id)
                        .ToList()
                };
            }
        }

        if (gameOverDto is null)
        {
            // Calculate and apply income for the next player (only if game continues)
            var tiles = await unitOfWork.Tiles.GetTilesWithBuildingsAndTerrainForKingdomAsync(nextKingdom.Id);
            var factionBonuses = await unitOfWork.FactionResourceBonuses
                .GetBonusesForFactionAsync(nextKingdom.FactionTypeId!.Value);
            var bonusLookup = factionBonuses.ToDictionary(b => b.ResourceType, b => b.Multiplier);

            income = Game.CalculateIncome(tiles, bonusLookup);

            var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(nextKingdom.Id);
            foreach (var (type, amount) in income.Where(i => i.Value > 0))
            {
                var resource = resources.Single(r => r.ResourceType == type);
                resource.Amount += amount;
                await unitOfWork.KingdomResources.UpdateAsync(resource);
            }
        }

        await unitOfWork.CommitAsync();

        return Result<TurnAdvancedDto>.Ok(new TurnAdvancedDto
        {
            NewKingdomId = nextKingdom.Id,
            TurnNumber = game.TurnNumber,
            IncomeApplied = gameOverDto is null
                ? income.Where(i => i.Value > 0).ToDictionary(i => i.Key.ToString(), i => i.Value)
                : new Dictionary<string, int>(),
            GameOver = gameOverDto
        });
    }
}
