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
}
