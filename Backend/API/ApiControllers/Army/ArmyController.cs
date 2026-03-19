using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.Army;
using Application.Services.Army.DTOs;
using Application.Services.GameHub;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.ApiControllers.Army;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game/{gameId:guid}")]
[ApiController]
[Authorize]
public class ArmyController(
    IArmyService armyService,
    IHubContext<GameHub, IGameClient> hubContext,
    IGameLockManager gameLockManager) : ControllerBase
{
    [HttpPost("train")]
    [ProducesResponseType(typeof(ArmyTrainedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TrainArmy(Guid gameId, [FromBody] TrainArmyRequest request)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await armyService.TrainArmyAsync(gameId, User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        await hubContext.Clients.Group($"game:{gameId}")
            .ArmyTrained(result.Value!);

        return Ok(result.Value);
    }

    [HttpGet("army-types")]
    [ProducesResponseType(typeof(IEnumerable<ArmyTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArmyTypes(Guid gameId)
    {
        var types = await armyService.GetArmyTypesAsync();
        return Ok(types);
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
