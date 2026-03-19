using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.Combat;
using Application.Services.Combat.DTOs;
using Application.Services.GameHub;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.ApiControllers.Combat;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game/{gameId:guid}")]
[ApiController]
[Authorize]
public class CombatController(
    ICombatService combatService,
    IHubContext<GameHub, IGameClient> hubContext,
    IGameLockManager gameLockManager) : ControllerBase
{
    [HttpPost("declare-attack")]
    [ProducesResponseType(typeof(DeclareAttackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeclareAttack(Guid gameId, [FromBody] DeclareAttackRequest request)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await combatService.DeclareAttackAsync(gameId, User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        await hubContext.Clients.Group($"game:{gameId}")
            .AttackDeclared(result.Value!);

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
