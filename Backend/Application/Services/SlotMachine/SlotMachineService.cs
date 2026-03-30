using System.Text.Json;
using Application.Contracts;
using Application.Services.SlotMachine.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Resources;

namespace Application.Services.SlotMachine;

public class SlotMachineService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : ISlotMachineService
{
    public async Task<Result<SpinResultDto>> SpinAsync(Guid gameId, Guid userId)
    {
        // 1. Validate game/kingdom access (NOT ValidateActionAsync -- spin does NOT cost AP)
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<SpinResultDto>.Fail(guardResult.Error!);

        var game = guardResult.Value!.Game;
        var kingdom = guardResult.Value!.Kingdom;

        // 2. Load mutable gold resource
        var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);
        var gold = resources.First(r => r.ResourceType == EResourceType.Gold);

        // 3. Validate spin eligibility
        var validationError = SlotMachineRules.ValidateSpin(game.CurrentPhase, gold.Amount, game.SpinCostGold);
        if (validationError is not null)
            return Result<SpinResultDto>.Fail(validationError);

        // 4. Deduct gold
        gold.Amount -= game.SpinCostGold;
        gold.UpdatedAt = DateTime.UtcNow;

        // 5. Determine outcome
        var outcome = SlotMachineRules.DetermineOutcome(game.SlotOutcomeWeights, Random.Shared);

        // 6. Apply outcome to AP
        game.RemainingActionPoints = SlotMachineRules.ApplyOutcome(game.RemainingActionPoints ?? 0, outcome);

        // 7. Create TurnLog
        var turnLog = new TurnLog
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            KingdomId = kingdom.Id,
            RoundNumber = game.RoundNumber,
            EventType = EEventType.SlotMachineSpin,
            Description = $"Slot machine: {(outcome >= 0 ? "+" : "")}{outcome} AP",
            Metadata = JsonSerializer.Serialize(new { goldSpent = game.SpinCostGold, outcome, actionsAfter = game.RemainingActionPoints }),
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.TurnLogs.AddAsync(turnLog);

        // 8. Commit
        await unitOfWork.CommitAsync();

        // 9. Return result
        return Result<SpinResultDto>.Ok(new SpinResultDto
        {
            KingdomId = kingdom.Id,
            Outcome = outcome,
            ActionPointsAfter = game.RemainingActionPoints ?? 0,
            GoldAfter = (int)gold.Amount,
            GoldSpent = game.SpinCostGold
        });
    }
}
