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

    /// <summary>
    /// Auto-skips the current turn due to timeout. Increments ConsecutiveMissedTurns.
    /// Eliminates kingdoms at 3 consecutive misses. Ends game as Abandoned if all kingdoms miss simultaneously.
    /// </summary>
    Task<Result<(TurnAdvancedDto TurnAdvanced, TurnAutoSkippedDto AutoSkipped)>> AutoSkipTurnAsync(
        Guid gameId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved);
}
