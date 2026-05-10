using Application.Services.Combat.DTOs.V1;
using Application.Services.Turn.DTOs.V1;
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
    /// <param name="roundDelay">
    /// Delay between <see cref="BattleRoundResultDto"/> broadcasts. Null = default 9s (paces the React
    /// slot-reel + HP-bar animation). Pass <see cref="TimeSpan.Zero"/> for instant resolution (MVC clients
    /// that don't animate and just want the final state as fast as possible).
    /// </param>
    /// <param name="battleDelay">
    /// Dwell time after each <see cref="BattleResultDto"/> broadcast. Null = default 5s. Zero for instant.
    /// </param>
    Task<Result<TurnAdvancedDto>> ResolveAndAdvanceAsync(
        Guid gameId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved,
        TimeSpan? roundDelay = null,
        TimeSpan? battleDelay = null);

    /// <summary>
    /// Auto-skips the current turn due to timeout. Increments ConsecutiveMissedTurns.
    /// Eliminates kingdoms at 3 consecutive misses. Ends game as Abandoned if all kingdoms miss simultaneously.
    /// </summary>
    Task<Result<(TurnAdvancedDto TurnAdvanced, TurnAutoSkippedDto AutoSkipped)>> AutoSkipTurnAsync(
        Guid gameId,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved);
}
