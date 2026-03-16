using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.Military;
using Application.Services.Military.DTOs;
using Application.Services.GameHub;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.ApiControllers.Military;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game/{gameId:guid}")]
[ApiController]
[Authorize]
public class MilitaryController(
    IMilitaryService militaryService,
    IHubContext<GameHub, IGameClient> hubContext,
    IGameLockManager gameLockManager) : ControllerBase
{
    [HttpPost("train")]
    [ProducesResponseType(typeof(TroopsTrainedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Train(Guid gameId, [FromBody] TrainTroopsRequest request)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);
        var result = await militaryService.TrainTroopsAsync(gameId, User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));
        await hubContext.Clients.Group($"game:{gameId}").TroopsTrained(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("move")]
    [ProducesResponseType(typeof(ArmyMovedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Move(Guid gameId, [FromBody] MoveArmyRequest request)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);
        var result = await militaryService.MoveArmyAsync(gameId, User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));
        await hubContext.Clients.Group($"game:{gameId}").ArmyMoved(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("attack")]
    [ProducesResponseType(typeof(CombatResolvedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Attack(Guid gameId, [FromBody] AttackRequest request)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);
        var result = await militaryService.AttackAsync(gameId, User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));
        await hubContext.Clients.Group($"game:{gameId}").CombatResolved(result.Value!);
        return Ok(result.Value);
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
