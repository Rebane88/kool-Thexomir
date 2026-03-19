using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.GameHub;
using Application.Services.Turn;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.ApiControllers.Turn;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game/{gameId:guid}")]
[ApiController]
[Authorize]
public class TurnController(
    ITurnService turnService,
    IHubContext<GameHub, IGameClient> hubContext,
    IGameLockManager gameLockManager) : ControllerBase
{
    [HttpPost("end-turn")]
    [ProducesResponseType(typeof(Application.Services.Turn.DTOs.TurnAdvancedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EndTurn(Guid gameId)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await turnService.EndTurnAsync(
            gameId,
            User.UserId(),
            onRoundResolved: async (round, battleId) =>
                await hubContext.Clients.Group($"game:{gameId}").BattleRoundResolved(round),
            onBattleResolved: async (battleResult) =>
                await hubContext.Clients.Group($"game:{gameId}").BattleResolved(battleResult));
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        await hubContext.Clients.Group($"game:{gameId}")
            .TurnAdvanced(result.Value!);

        if (result.Value!.PhaseChanged)
        {
            await hubContext.Clients.Group($"game:{gameId}")
                .PhaseChanged(new Application.Services.Turn.DTOs.PhaseChangedDto
                {
                    Phase = result.Value!.CurrentPhase,
                    PreviousPhase = "Action",
                    RoundNumber = result.Value!.RoundNumber
                });
        }

        if (result.Value!.GameOver is not null)
            await hubContext.Clients.Group($"game:{gameId}").GameOver(result.Value!.GameOver);

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
