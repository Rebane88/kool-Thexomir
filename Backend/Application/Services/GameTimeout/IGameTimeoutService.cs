using Application.Services.Combat.DTOs;
using Application.Services.Turn.DTOs;
using Application.Services.WinCondition.DTOs;

namespace Application.Services.GameTimeout;

public interface IGameTimeoutService
{
    Task ProcessExpiredTurnsAsync(
        Func<Guid, TurnAutoSkippedDto, Task> onTurnAutoSkipped,
        Func<Guid, TurnAdvancedDto, Task> onTurnAdvanced,
        Func<Guid, PhaseChangedDto, Task> onPhaseChanged,
        Func<Guid, GameOverDto, Task> onGameOver,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved,
        CancellationToken ct);
}
