using Application.Contracts;
using Application.Services.Combat;
using Application.Services.Combat.DTOs;
using Application.Services.Turn.DTOs;
using Application.Services.WinCondition.DTOs;
using Base.Contracts;
using Domain.Buildings;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Microsoft.Extensions.Logging;

namespace Application.Services.Turn;

public class TurnService(IUnitOfWork unitOfWork, IGameGuard gameGuard, ICombatService combatService, ILogger<TurnService> logger) : ITurnService
{
    public async Task<Result<TurnAdvancedDto>> EndTurnAsync(
        Guid gameId,
        Guid userId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved)
    {
        logger.LogInformation("[EndTurn] Game={GameId} User={UserId}", gameId, userId);

        // 1. Validate game/kingdom access (not ValidateActionAsync -- ending turn is free)
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
        {
            logger.LogWarning("[EndTurn] Guard failed: {Error}", guardResult.Error);
            return Result<TurnAdvancedDto>.Fail(guardResult.Error!);
        }

        var game = guardResult.Value!.Game;
        var kingdom = guardResult.Value!.Kingdom;

        // Reset consecutive missed turns on manual end turn
        kingdom.ConsecutiveMissedTurns = 0;

        logger.LogInformation("[EndTurn] Kingdom={KingdomName} (Id={KingdomId}, TurnOrder={TurnOrder}) Phase={Phase} Round={Round}",
            kingdom.Name, kingdom.Id, kingdom.TurnOrder, game.CurrentPhase, game.RoundNumber);

        // 2. Validate end-turn rules
        var validationError = TurnRules.ValidateEndTurn(game.CurrentPhase, game.CurrentTurnKingdomId, kingdom.Id);
        if (validationError is not null)
        {
            logger.LogWarning("[EndTurn] Validation failed: {Error}", validationError);
            return Result<TurnAdvancedDto>.Fail(validationError);
        }

        // 3. Check expired turn (log it but still process -- the turn is ending anyway)
        var wasExpired = TurnRules.IsTurnExpired(game.TurnDeadline);

        // 4. Log TurnEnded
        await AddTurnLogAsync(gameId, kingdom.Id, game.RoundNumber, EEventType.TurnEnded,
            wasExpired ? "Turn ended (expired)" : "Turn ended");

        // 5. Get all kingdoms for this game
        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);

        logger.LogDebug("[EndTurn] Kingdoms: {Kingdoms}",
            string.Join(", ", kingdoms.Select(k => $"{k.Name}(Order={k.TurnOrder}, Status={k.Status})")));

        // 6. Find next active kingdom
        var nextKingdom = TurnRules.GetNextActiveKingdom(kingdoms, kingdom.TurnOrder);

        logger.LogInformation("[EndTurn] NextKingdom={NextKingdom}",
            nextKingdom is not null ? $"{nextKingdom.Name} (Order={nextKingdom.TurnOrder})" : "NONE (advancing phase)");

        TurnAdvancedDto result;

        if (nextKingdom is not null)
        {
            // Load faction type for AP calculation
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(nextKingdom.FactionTypeId!.Value);
            var actionPoints = TurnRules.CalculateActionPoints(game.BaseActionPoints, factionType!.ActionPointModifier);

            game.CurrentTurnKingdomId = nextKingdom.Id;
            game.RemainingActionPoints = actionPoints;
            game.TurnDeadline = game.TurnTimeLimit.HasValue
                ? DateTime.UtcNow.AddSeconds(game.TurnTimeLimit.Value)
                : null;

            await AddTurnLogAsync(gameId, nextKingdom.Id, game.RoundNumber, EEventType.TurnStarted,
                $"Turn started for {nextKingdom.Name}");
            await AddTurnLogAsync(gameId, nextKingdom.Id, game.RoundNumber, EEventType.ActionPointsReceived,
                $"Received {actionPoints} action points");

            result = new TurnAdvancedDto
            {
                NextKingdomId = nextKingdom.Id,
                RoundNumber = game.RoundNumber,
                CurrentPhase = game.CurrentPhase.ToString(),
                ActionPoints = actionPoints,
                TurnDeadline = game.TurnDeadline,
                PhaseChanged = false
            };
        }
        else
        {
            // All players done -- check if there are declared attacks
            var declaredAttacks = await unitOfWork.DeclaredAttacks
                .GetForGameRoundAsync(gameId, game.RoundNumber);

            if (declaredAttacks.Count > 0)
            {
                // Enter Battle phase and STOP — wait for players to select armies & lineups
                result = await EnterBattlePhaseAsync(game);
            }
            else
            {
                // No attacks — skip Battle, go straight through Income → RoundEnd → Action
                result = await AdvanceFromBattleAsync(game, kingdoms, onRoundResolved, onBattleResolved);
            }
        }

        await unitOfWork.CommitAsync();

        logger.LogInformation("[EndTurn] Result: NextKingdom={NextKingdomId} Phase={Phase} Round={Round} PhaseChanged={PhaseChanged}",
            result.NextKingdomId, result.CurrentPhase, result.RoundNumber, result.PhaseChanged);

        return Result<TurnAdvancedDto>.Ok(result);
    }

    /// <summary>
    /// Enters Battle phase and stops — players must select armies and lineups interactively.
    /// </summary>
    private async Task<TurnAdvancedDto> EnterBattlePhaseAsync(Game game)
    {
        var previousPhase = game.CurrentPhase;

        // Clear turn state during non-Action phases
        game.CurrentTurnKingdomId = null;
        game.RemainingActionPoints = null;
        game.TurnDeadline = null;

        game.CurrentPhase = EGamePhase.Battle;
        await AddTurnLogAsync(game.Id, null, game.RoundNumber, EEventType.PhaseChanged,
            $"Phase changed: {previousPhase} -> {game.CurrentPhase}");

        return new TurnAdvancedDto
        {
            NextKingdomId = null,
            RoundNumber = game.RoundNumber,
            CurrentPhase = game.CurrentPhase.ToString(),
            PhaseChanged = true
        };
    }

    public async Task<Result<TurnAdvancedDto>> ResolveAndAdvanceAsync(
        Guid gameId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved)
    {
        logger.LogInformation("[ResolveAndAdvance] Game={GameId}", gameId);

        var game = await unitOfWork.Games.GetByIdWithLockAsync(gameId);
        if (game is null)
            return Result<TurnAdvancedDto>.Fail("Game not found.");
        if (game.CurrentPhase != EGamePhase.Battle)
            return Result<TurnAdvancedDto>.Fail("Game is not in Battle phase.");

        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);

        var result = await AdvanceFromBattleAsync(game, kingdoms, onRoundResolved, onBattleResolved);

        await unitOfWork.CommitAsync();

        logger.LogInformation("[ResolveAndAdvance] Result: NextKingdom={NextKingdomId} Phase={Phase} Round={Round}",
            result.NextKingdomId, result.CurrentPhase, result.RoundNumber);

        return Result<TurnAdvancedDto>.Ok(result);
    }

    /// <summary>
    /// Resolves battles (if any), then advances through Income → RoundEnd → Action.
    /// Called either directly (no attacks) or after all lineups are set (attacks exist).
    /// </summary>
    private async Task<TurnAdvancedDto> AdvanceFromBattleAsync(
        Game game,
        List<Kingdom> kingdoms,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved)
    {
        Dictionary<Guid, Dictionary<string, int>>? incomeApplied = null;

        // If we haven't entered Battle phase yet (no-attack path), set it briefly for logging
        if (game.CurrentPhase != EGamePhase.Battle)
        {
            var previousPhase = game.CurrentPhase;
            game.CurrentTurnKingdomId = null;
            game.RemainingActionPoints = null;
            game.TurnDeadline = null;
            game.CurrentPhase = EGamePhase.Battle;
            await AddTurnLogAsync(game.Id, null, game.RoundNumber, EEventType.PhaseChanged,
                $"Phase changed: {previousPhase} -> {game.CurrentPhase}");
        }

        // Resolve all declared attacks for this round
        var battleResults = await combatService.ResolveBattlesAsync(game);

        // Broadcast rounds individually so clients animate in real time
        foreach (var battleResult in battleResults)
        {
            var battleIdStr = battleResult.BattleId.ToString();
            foreach (var round in battleResult.Rounds)
            {
                await onRoundResolved(round, battleIdStr);
                await Task.Delay(9000); // 9s per round — 3 reels (~1.7s each) + pauses + HP bar + destruction animations
            }
            await onBattleResolved(battleResult);
            await Task.Delay(5000); // 5s pause between battles for summary screen
        }

        // Check if any kingdom was eliminated -- refresh kingdom statuses
        if (battleResults.Any())
        {
            kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(game.Id);

            // Check if game ended via elimination
            var activeKingdoms = kingdoms.Where(k => k.Status == EKingdomStatus.Active).ToList();
            if (activeKingdoms.Count <= 1)
            {
                game.Status = EGameStatus.Completed;
                game.FinishedAt = DateTime.UtcNow;
                var winner = activeKingdoms.FirstOrDefault();
                return new TurnAdvancedDto
                {
                    NextKingdomId = null,
                    RoundNumber = game.RoundNumber,
                    CurrentPhase = game.CurrentPhase.ToString(),
                    PhaseChanged = true,
                    BattleResults = null,
                    GameOver = new WinCondition.DTOs.GameOverDto
                    {
                        GameId = game.Id,
                        WinnerKingdomId = winner?.Id,
                        WinConditionType = "Elimination"
                    }
                };
            }
        }

        // --- Income Phase ---
        game.CurrentPhase = EGamePhase.Income;
        await AddTurnLogAsync(game.Id, null, game.RoundNumber, EEventType.PhaseChanged,
            $"Phase changed: Battle -> {game.CurrentPhase}");

        incomeApplied = await ApplyIncomeAsync(game, kingdoms);

        // --- RoundEnd Phase ---
        game.CurrentPhase = EGamePhase.RoundEnd;
        await AddTurnLogAsync(game.Id, null, game.RoundNumber, EEventType.PhaseChanged,
            $"Phase changed: Income -> {game.CurrentPhase}");

        await AddTurnLogAsync(game.Id, null, game.RoundNumber, EEventType.RoundEnded,
            $"Round {game.RoundNumber} ended");

        // Increment round
        game.RoundNumber++;

        // Check MaxRounds for draw
        WinCondition.DTOs.GameOverDto? gameOver = null;
        if (game.RoundNumber > game.MaxRounds)
        {
            game.Status = EGameStatus.Completed;
            game.FinishedAt = DateTime.UtcNow;
            gameOver = new WinCondition.DTOs.GameOverDto
            {
                GameId = game.Id,
                WinnerKingdomId = null,
                WinConditionType = "MaxRoundsReached"
            };
        }

        // --- Back to Action Phase ---
        game.CurrentPhase = EGamePhase.Action;
        await AddTurnLogAsync(game.Id, null, game.RoundNumber, EEventType.PhaseChanged,
            $"Phase changed: RoundEnd -> {game.CurrentPhase}");

        await AddTurnLogAsync(game.Id, null, game.RoundNumber, EEventType.RoundStarted,
            $"Round {game.RoundNumber} started");

        // Find first active kingdom for new round
        var firstKingdom = kingdoms
            .Where(k => k.Status == EKingdomStatus.Active)
            .OrderBy(k => k.TurnOrder)
            .FirstOrDefault();

        int? actionPoints = null;

        if (firstKingdom is not null && gameOver is null)
        {
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(firstKingdom.FactionTypeId!.Value);
            actionPoints = TurnRules.CalculateActionPoints(game.BaseActionPoints, factionType!.ActionPointModifier);

            game.CurrentTurnKingdomId = firstKingdom.Id;
            game.RemainingActionPoints = actionPoints;
            game.TurnDeadline = game.TurnTimeLimit.HasValue
                ? DateTime.UtcNow.AddSeconds(game.TurnTimeLimit.Value)
                : null;

            await AddTurnLogAsync(game.Id, firstKingdom.Id, game.RoundNumber, EEventType.TurnStarted,
                $"Turn started for {firstKingdom.Name}");
            await AddTurnLogAsync(game.Id, firstKingdom.Id, game.RoundNumber, EEventType.ActionPointsReceived,
                $"Received {actionPoints} action points");
        }

        return new TurnAdvancedDto
        {
            NextKingdomId = firstKingdom?.Id,
            RoundNumber = game.RoundNumber,
            CurrentPhase = game.CurrentPhase.ToString(),
            ActionPoints = actionPoints,
            TurnDeadline = game.TurnDeadline,
            IncomeApplied = incomeApplied,
            BattleResults = null,
            PhaseChanged = true,
            GameOver = gameOver
        };
    }

    private async Task<Dictionary<Guid, Dictionary<string, int>>> ApplyIncomeAsync(Game game, List<Kingdom> kingdoms)
    {
        var perKingdomIncome = new Dictionary<Guid, Dictionary<string, int>>();
        var activeKingdoms = kingdoms.Where(k => k.Status == EKingdomStatus.Active).ToList();

        foreach (var kingdom in activeKingdoms)
        {
            // Load tiles with buildings and terrain for this kingdom
            var tiles = await unitOfWork.Tiles.GetTilesWithBuildingsAndTerrainForKingdomAsync(kingdom.Id);

            // Build the (BuildingType, TerrainType) pairs for IncomeCalculator
            var buildingTerrainPairs = new List<(BuildingType, TerrainType)>();
            foreach (var tile in tiles)
            {
                if (tile.Buildings is null || tile.TerrainType is null) continue;
                foreach (var building in tile.Buildings)
                {
                    if (building.BuildingType is null) continue;
                    buildingTerrainPairs.Add((building.BuildingType, tile.TerrainType));
                }
            }

            if (buildingTerrainPairs.Count == 0) continue;

            // Load faction type for modifier
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId!.Value);
            var income = IncomeCalculator.CalculateKingdomIncome(buildingTerrainPairs, factionType!.ResourceProductionModifier);

            if (income.Count == 0) continue;

            // Load mutable resources and apply income
            var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);
            var kingdomIncome = new Dictionary<string, int>();
            foreach (var (resourceType, amount) in income)
            {
                var resource = resources.FirstOrDefault(r => r.ResourceType == resourceType);
                if (resource is not null)
                {
                    resource.Amount += amount;
                }

                kingdomIncome[resourceType.ToString()] = amount;
            }

            perKingdomIncome[kingdom.Id] = kingdomIncome;

            await AddTurnLogAsync(game.Id, kingdom.Id, game.RoundNumber, EEventType.IncomeReceived,
                $"Income received: {string.Join(", ", income.Select(kv => $"{kv.Key}: +{kv.Value}"))}");
        }

        // --- Heal armies ---
        foreach (var kingdom in activeKingdoms)
        {
            var armies = (await unitOfWork.Armies.GetArmiesWithTypeForKingdomAsync(kingdom.Id)).ToList();
            if (armies.Count == 0) continue;

            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId!.Value);
            var healedCount = 0;

            foreach (var army in armies.Where(a => a.CurrentHP < a.MaxHP))
            {
                var newHP = ArmyRules.CalculateHealing(
                    army.CurrentHP, army.MaxHP, game.HealPercent, factionType!.HealRateModifier);
                if (newHP > army.CurrentHP)
                {
                    army.CurrentHP = newHP;
                    army.UpdatedAt = DateTime.UtcNow;
                    await unitOfWork.Armies.UpdateAsync(army);
                    healedCount++;
                }
            }

            if (healedCount > 0)
            {
                await AddTurnLogAsync(game.Id, kingdom.Id, game.RoundNumber, EEventType.ArmyHealed,
                    $"{healedCount} army/armies healed");
            }
        }

        // --- Deduct upkeep and disband ---
        foreach (var kingdom in activeKingdoms)
        {
            var armies = (await unitOfWork.Armies.GetArmiesWithTypeForKingdomAsync(kingdom.Id)).ToList();
            if (armies.Count == 0) continue;

            var armiesWithType = armies.Select(a => (a, a.ArmyType!)).ToList();
            var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);

            // Check if we need to disband
            var toDisband = ArmyRules.GetArmiesToDisband(armiesWithType, resources);

            foreach (var army in toDisband)
            {
                await unitOfWork.Armies.DeleteAsync(army.Id);
                await AddTurnLogAsync(game.Id, kingdom.Id, game.RoundNumber, EEventType.ArmyDisbanded,
                    $"Army disbanded due to insufficient upkeep");
            }

            // Deduct upkeep for remaining armies
            var remaining = armiesWithType.Where(a => !toDisband.Contains(a.a)).ToList();
            if (remaining.Count > 0)
            {
                var upkeepCosts = ArmyRules.CalculateTotalUpkeep(remaining);
                var deductError = BuildingRules.DeductResourceCost(resources, upkeepCosts);
                // deductError should be null since we disbanded enough armies
                if (deductError is null)
                {
                    await AddTurnLogAsync(game.Id, kingdom.Id, game.RoundNumber, EEventType.UpkeepPaid,
                        $"Upkeep paid for {remaining.Count} army/armies");
                }
            }
        }

        return perKingdomIncome;
    }

    public async Task<Result<(TurnAdvancedDto TurnAdvanced, TurnAutoSkippedDto AutoSkipped)>> AutoSkipTurnAsync(
        Guid gameId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved)
    {
        logger.LogInformation("[AutoSkip] Game={GameId}", gameId);

        var game = await unitOfWork.Games.GetByIdWithLockAsync(gameId);
        if (game is null)
            return Result<(TurnAdvancedDto, TurnAutoSkippedDto)>.Fail("Game not found.");

        if (game.Status != EGameStatus.InProgress)
            return Result<(TurnAdvancedDto, TurnAutoSkippedDto)>.Fail("Game is not in progress.");

        if (!TurnRules.IsTurnExpired(game.TurnDeadline))
            return Result<(TurnAdvancedDto, TurnAutoSkippedDto)>.Fail("Turn has not expired yet.");

        if (game.CurrentTurnKingdomId is null)
            return Result<(TurnAdvancedDto, TurnAutoSkippedDto)>.Fail("No current turn kingdom.");

        // Get all kingdoms for this game
        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);
        var currentKingdom = kingdoms.FirstOrDefault(k => k.Id == game.CurrentTurnKingdomId);

        if (currentKingdom is null)
            return Result<(TurnAdvancedDto, TurnAutoSkippedDto)>.Fail("Current turn kingdom not found.");

        // Increment missed turns
        currentKingdom.ConsecutiveMissedTurns++;

        logger.LogInformation("[AutoSkip] Kingdom={KingdomName} missed turn {Count}/3",
            currentKingdom.Name, currentKingdom.ConsecutiveMissedTurns);

        await AddTurnLogAsync(gameId, currentKingdom.Id, game.RoundNumber, EEventType.TurnEnded,
            $"Turn auto-skipped (timeout) for {currentKingdom.Name} — missed {currentKingdom.ConsecutiveMissedTurns}/3");

        var autoSkippedDto = new TurnAutoSkippedDto
        {
            SkippedKingdomId = currentKingdom.Id,
            SkippedKingdomName = currentKingdom.Name,
            ConsecutiveMissedTurns = currentKingdom.ConsecutiveMissedTurns
        };

        // Check elimination threshold (3 consecutive misses)
        const int MissedTurnThreshold = 3;

        var activeKingdoms = kingdoms.Where(k => k.Status == EKingdomStatus.Active).ToList();

        // Check if ALL active kingdoms have hit the threshold
        var allMissed = activeKingdoms.All(k =>
            k.Id == currentKingdom.Id
                ? k.ConsecutiveMissedTurns >= MissedTurnThreshold
                : k.ConsecutiveMissedTurns >= MissedTurnThreshold);

        if (allMissed && activeKingdoms.Count > 0)
        {
            // All active kingdoms inactive — end game as Abandoned
            logger.LogInformation("[AutoSkip] All kingdoms inactive — ending game as Abandoned");
            game.Status = EGameStatus.Completed;
            game.FinishedAt = DateTime.UtcNow;
            game.CurrentTurnKingdomId = null;
            game.TurnDeadline = null;
            game.RemainingActionPoints = null;

            var finalStandings = await BuildFinalStandingsAsync(gameId, kingdoms);
            var gameOverDto = new GameOverDto
            {
                GameId = gameId,
                WinnerKingdomId = null,
                WinConditionType = "Abandoned",
                FinalStandings = finalStandings,
                EliminationOrder = []
            };

            await unitOfWork.CommitAsync();

            var abandonedTurnAdvanced = new TurnAdvancedDto
            {
                NextKingdomId = null,
                RoundNumber = game.RoundNumber,
                CurrentPhase = game.CurrentPhase.ToString(),
                PhaseChanged = false,
                GameOver = gameOverDto
            };

            return Result<(TurnAdvancedDto, TurnAutoSkippedDto)>.Ok((abandonedTurnAdvanced, autoSkippedDto));
        }

        // Eliminate kingdoms that hit the threshold
        var toEliminate = activeKingdoms
            .Where(k => k.ConsecutiveMissedTurns >= MissedTurnThreshold)
            .ToList();

        foreach (var kingdom in toEliminate)
        {
            logger.LogInformation("[AutoSkip] Eliminating kingdom {KingdomName} (3 consecutive misses)", kingdom.Name);
            kingdom.Status = EKingdomStatus.Defeated;
            kingdom.DefeatedAt = DateTime.UtcNow;
            await AddTurnLogAsync(gameId, kingdom.Id, game.RoundNumber, EEventType.TurnEnded,
                $"{kingdom.Name} eliminated after 3 consecutive missed turns");
        }

        // Refresh active kingdoms after eliminations
        var remainingActive = kingdoms.Where(k => k.Status == EKingdomStatus.Active).ToList();

        if (remainingActive.Count <= 1)
        {
            // Game over — last active kingdom wins
            var winner = remainingActive.FirstOrDefault();
            logger.LogInformation("[AutoSkip] Game over. Winner={Winner}", winner?.Name ?? "none");

            game.Status = EGameStatus.Completed;
            game.FinishedAt = DateTime.UtcNow;
            game.CurrentTurnKingdomId = null;
            game.TurnDeadline = null;
            game.RemainingActionPoints = null;

            var finalStandings = await BuildFinalStandingsAsync(gameId, kingdoms);
            var gameOverDto = new GameOverDto
            {
                GameId = gameId,
                WinnerKingdomId = winner?.Id,
                WinConditionType = "Elimination",
                FinalStandings = finalStandings,
                EliminationOrder = []
            };

            await unitOfWork.CommitAsync();

            var gameOverTurnAdvanced = new TurnAdvancedDto
            {
                NextKingdomId = null,
                RoundNumber = game.RoundNumber,
                CurrentPhase = game.CurrentPhase.ToString(),
                PhaseChanged = false,
                GameOver = gameOverDto
            };

            return Result<(TurnAdvancedDto, TurnAutoSkippedDto)>.Ok((gameOverTurnAdvanced, autoSkippedDto));
        }

        // Find next active kingdom in turn order
        var nextKingdom = TurnRules.GetNextActiveKingdom(kingdoms, currentKingdom.TurnOrder);

        TurnAdvancedDto result;

        if (nextKingdom is not null)
        {
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(nextKingdom.FactionTypeId!.Value);
            var actionPoints = TurnRules.CalculateActionPoints(game.BaseActionPoints, factionType!.ActionPointModifier);

            game.CurrentTurnKingdomId = nextKingdom.Id;
            game.RemainingActionPoints = actionPoints;
            game.TurnDeadline = game.TurnTimeLimit.HasValue
                ? DateTime.UtcNow.AddSeconds(game.TurnTimeLimit.Value)
                : null;

            await AddTurnLogAsync(gameId, nextKingdom.Id, game.RoundNumber, EEventType.TurnStarted,
                $"Turn started for {nextKingdom.Name}");
            await AddTurnLogAsync(gameId, nextKingdom.Id, game.RoundNumber, EEventType.ActionPointsReceived,
                $"Received {actionPoints} action points");

            result = new TurnAdvancedDto
            {
                NextKingdomId = nextKingdom.Id,
                RoundNumber = game.RoundNumber,
                CurrentPhase = game.CurrentPhase.ToString(),
                ActionPoints = actionPoints,
                TurnDeadline = game.TurnDeadline,
                PhaseChanged = false
            };
        }
        else
        {
            // All players done this round — advance phase
            var declaredAttacks = await unitOfWork.DeclaredAttacks
                .GetForGameRoundAsync(gameId, game.RoundNumber);

            if (declaredAttacks.Count > 0)
            {
                result = await EnterBattlePhaseAsync(game);
            }
            else
            {
                result = await AdvanceFromBattleAsync(game, kingdoms, onRoundResolved, onBattleResolved);
            }
        }

        await unitOfWork.CommitAsync();

        return Result<(TurnAdvancedDto, TurnAutoSkippedDto)>.Ok((result, autoSkippedDto));
    }

    private async Task<List<KingdomResultDto>> BuildFinalStandingsAsync(Guid gameId, List<Kingdom> kingdoms)
    {
        var standings = new List<KingdomResultDto>();
        foreach (var kingdom in kingdoms)
        {
            var tiles = await unitOfWork.Tiles.GetTilesWithBuildingsAndTerrainForKingdomAsync(kingdom.Id);
            standings.Add(new KingdomResultDto
            {
                KingdomId = kingdom.Id,
                KingdomName = kingdom.Name,
                TilesOwned = tiles.Count,
                Status = kingdom.Status.ToString()
            });
        }
        return standings;
    }

    private async Task AddTurnLogAsync(Guid gameId, Guid? kingdomId, int roundNumber, EEventType eventType, string description)
    {
        var turnLog = new TurnLog
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            KingdomId = kingdomId,
            RoundNumber = roundNumber,
            EventType = eventType,
            Description = description,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.TurnLogs.AddAsync(turnLog);
    }
}
