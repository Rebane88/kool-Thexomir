using Application.Services.Combat.DTOs;
using Application.Services.Turn.DTOs;
using Base.Contracts;

namespace Application.Services.Turn;

public interface ITurnService
{
    Task<Result<TurnAdvancedDto>> EndTurnAsync(
        Guid gameId,
        Guid userId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved);

    /// <summary>
    /// Called after all lineups are set during Battle phase.
    /// Resolves battles (broadcasting rounds for animation), then advances through Income → RoundEnd → Action.
    /// </summary>
    Task<Result<TurnAdvancedDto>> ResolveAndAdvanceAsync(
        Guid gameId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved);
}
