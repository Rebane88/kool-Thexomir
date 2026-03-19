using Application.Contracts;
using Application.Services.Combat;
using Application.Services.Combat.DTOs;
using Application.Services.Turn.DTOs;
using Base.Contracts;
using Domain.Buildings;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Domain.Resources;

namespace Application.Services.Turn;

public class TurnService(IUnitOfWork unitOfWork, IGameGuard gameGuard, ICombatService combatService) : ITurnService
{
    public async Task<Result<TurnAdvancedDto>> EndTurnAsync(
        Guid gameId,
        Guid userId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved)
    {
        // 1. Validate game/kingdom access (not ValidateActionAsync -- ending turn is free)
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<TurnAdvancedDto>.Fail(guardResult.Error!);

        var game = guardResult.Value!.Game;
        var kingdom = guardResult.Value!.Kingdom;

        // 2. Validate end-turn rules
        var validationError = TurnRules.ValidateEndTurn(game.CurrentPhase, game.CurrentTurnKingdomId, kingdom.Id);
        if (validationError is not null)
            return Result<TurnAdvancedDto>.Fail(validationError);

        // 3. Check expired turn (log it but still process -- the turn is ending anyway)
        var wasExpired = TurnRules.IsTurnExpired(game.TurnDeadline);

        // 4. Log TurnEnded
        await AddTurnLogAsync(gameId, kingdom.Id, game.RoundNumber, EEventType.TurnEnded,
            wasExpired ? "Turn ended (expired)" : "Turn ended");

        // 5. Get all kingdoms for this game
        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);

        // 6. Find next active kingdom
        var nextKingdom = TurnRules.GetNextActiveKingdom(kingdoms, kingdom.TurnOrder);

        TurnAdvancedDto result;

        if (nextKingdom is not null)
        {
            // Load faction type for AP calculation
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(nextKingdom.FactionTypeId);
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
            // All players done -- advance through phases
            result = await AdvanceToNextPhaseAsync(game, kingdoms, onRoundResolved, onBattleResolved);
        }

        await unitOfWork.CommitAsync();

        return Result<TurnAdvancedDto>.Ok(result);
    }

    private async Task<TurnAdvancedDto> AdvanceToNextPhaseAsync(
        Game game,
        List<Kingdom> kingdoms,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved)
    {
        var previousPhase = game.CurrentPhase;
        Dictionary<string, int>? incomeApplied = null;
        List<BattleResultDto> battleResults = [];

        // Clear turn state during non-Action phases
        game.CurrentTurnKingdomId = null;
        game.RemainingActionPoints = null;
        game.TurnDeadline = null;

        // --- Battle Phase ---
        game.CurrentPhase = EGamePhase.Battle;
        await AddTurnLogAsync(game.Id, null, game.RoundNumber, EEventType.PhaseChanged,
            $"Phase changed: {previousPhase} -> {game.CurrentPhase}");

        // Resolve all declared attacks for this round
        battleResults = await combatService.ResolveBattlesAsync(game);

        // Broadcast rounds individually before committing, so clients animate in real time
        foreach (var battleResult in battleResults)
        {
            var battleIdStr = battleResult.BattleId.ToString();
            foreach (var round in battleResult.Rounds)
            {
                await onRoundResolved(round, battleIdStr);
                await Task.Delay(2500); // 2.5s per round for playback
            }
            await onBattleResolved(battleResult);
            await Task.Delay(1500); // 1.5s pause between battles
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
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(firstKingdom.FactionTypeId);
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
            BattleResults = null, // Battles broadcast individually via BattleRoundResolved/BattleResolved events
            PhaseChanged = true,
            GameOver = gameOver
        };
    }

    private async Task<Dictionary<string, int>> ApplyIncomeAsync(Game game, List<Kingdom> kingdoms)
    {
        var totalIncome = new Dictionary<string, int>();
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
            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId);
            var income = IncomeCalculator.CalculateKingdomIncome(buildingTerrainPairs, factionType!.ResourceProductionModifier);

            if (income.Count == 0) continue;

            // Load mutable resources and apply income
            var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);
            foreach (var (resourceType, amount) in income)
            {
                var resource = resources.FirstOrDefault(r => r.ResourceType == resourceType);
                if (resource is not null)
                {
                    resource.Amount += amount;
                }

                // Accumulate for DTO
                var key = resourceType.ToString();
                totalIncome.TryGetValue(key, out var current);
                totalIncome[key] = current + amount;
            }

            await AddTurnLogAsync(game.Id, kingdom.Id, game.RoundNumber, EEventType.IncomeReceived,
                $"Income received: {string.Join(", ", income.Select(kv => $"{kv.Key}: +{kv.Value}"))}");
        }

        // --- Heal armies ---
        foreach (var kingdom in activeKingdoms)
        {
            var armies = (await unitOfWork.Armies.GetArmiesWithTypeForKingdomAsync(kingdom.Id)).ToList();
            if (armies.Count == 0) continue;

            var factionType = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId);
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

        return totalIncome;
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
