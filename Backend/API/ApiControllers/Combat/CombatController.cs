using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.Combat;
using Application.Services.Combat.DTOs;
using Application.Services.GameHub;
using Application.Services.Turn;
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
    ITurnService turnService,
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

    [HttpPost("select-armies")]
    [ProducesResponseType(typeof(BattleSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SelectArmies(Guid gameId, [FromBody] SelectArmiesRequest request)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await combatService.SelectArmiesAsync(gameId, User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        await hubContext.Clients.Group($"game:{gameId}")
            .ArmiesSelected(result.Value!);

        return Ok(result.Value);
    }

    [HttpGet("reveal-armies/{declaredAttackId:guid}")]
    [ProducesResponseType(typeof(ArmyRevealDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevealArmies(Guid gameId, Guid declaredAttackId)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await combatService.GetArmyRevealAsync(gameId, User.UserId(), declaredAttackId);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        return Ok(result.Value);
    }

    [HttpPost("set-lineup")]
    [ProducesResponseType(typeof(BattleSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetLineup(Guid gameId, [FromBody] SetLineupRequest request)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await combatService.SetLineupAsync(gameId, User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        await hubContext.Clients.Group($"game:{gameId}")
            .LineupSet(result.Value!);

        // Check if all lineups are set — if so, resolve battles and advance phases
        var allReady = await combatService.AreAllLineupsSetAsync(gameId);
        if (allReady)
        {
            var advanceResult = await turnService.ResolveAndAdvanceAsync(
                gameId,
                onRoundResolved: async (round, battleId) =>
                    await hubContext.Clients.Group($"game:{gameId}").BattleRoundResolved(round),
                onBattleResolved: async (battleResult) =>
                    await hubContext.Clients.Group($"game:{gameId}").BattleResolved(battleResult));

            if (advanceResult.IsSuccess)
            {
                await hubContext.Clients.Group($"game:{gameId}")
                    .TurnAdvanced(advanceResult.Value!);

                if (advanceResult.Value!.PhaseChanged)
                {
                    await hubContext.Clients.Group($"game:{gameId}")
                        .PhaseChanged(new Application.Services.Turn.DTOs.PhaseChangedDto
                        {
                            Phase = advanceResult.Value!.CurrentPhase,
                            PreviousPhase = "Battle",
                            RoundNumber = advanceResult.Value!.RoundNumber
                        });
                }

                if (advanceResult.Value!.GameOver is not null)
                    await hubContext.Clients.Group($"game:{gameId}").GameOver(advanceResult.Value!.GameOver);
            }
        }

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
