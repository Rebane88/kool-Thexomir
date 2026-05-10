using Application.Services.WinCondition.DTOs.V1;
using Base.Contracts;

namespace Application.Services.Abandon;

public interface IAbandonService
{
    /// <summary>
    /// Explicitly abandons a game for the given user.
    /// Marks the kingdom as Defeated. If only one or zero active kingdoms remain, ends the game.
    /// Returns GameOverDto if the game ended, null if game continues.
    /// </summary>
    Task<Result<GameOverDto?>> AbandonGameAsync(Guid gameId, Guid userId);
}
