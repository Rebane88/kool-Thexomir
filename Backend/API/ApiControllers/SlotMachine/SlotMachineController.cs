using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.SlotMachine;
using Application.Services.SlotMachine.DTOs;
using Application.Services.GameHub;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.ApiControllers.SlotMachine;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game/{gameId:guid}")]
[ApiController]
[Authorize]
public class SlotMachineController(
    ISlotMachineService slotMachineService,
    IHubContext<GameHub, IGameClient> hubContext,
    IGameLockManager gameLockManager) : ControllerBase
{
    [HttpPost("spin")]
    [ProducesResponseType(typeof(SpinResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Spin(Guid gameId)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await slotMachineService.SpinAsync(gameId, User.UserId());
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        await hubContext.Clients.Group($"game:{gameId}")
            .SlotMachineSpun(result.Value!);

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
