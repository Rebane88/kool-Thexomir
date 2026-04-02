using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.Abandon;
using Application.Services.GameHub;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.ApiControllers.Game;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game")]
[ApiController]
[Authorize]
public class GameSessionController(
    IUnitOfWork unitOfWork,
    IAbandonService abandonService,
    IHubContext<GameHub, IGameClient> hubContext,
    IGameLockManager gameLockManager) : ControllerBase
{
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActiveGame()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || !Guid.TryParse(userId, out var parsedUserId))
            return Unauthorized();

        var gameId = await unitOfWork.Kingdoms.GetActiveGameIdForUserAsync(parsedUserId);
        if (gameId is null)
            return NoContent();

        return Ok(new { gameId });
    }

    [HttpPost("{gameId:guid}/abandon")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AbandonGame(Guid gameId)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await abandonService.AbandonGameAsync(gameId, User.UserId());
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        if (result.Value is not null)
        {
            await hubContext.Clients.Group($"game:{gameId}").GameOver(result.Value);
        }

        return Ok();
    }

    private static ProblemDetails ProblemDetailsFor(int status, string detail) => new()
    {
        Status = status,
        Detail = detail,
        Title = status switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            _ => "Error"
        }
    };
}
