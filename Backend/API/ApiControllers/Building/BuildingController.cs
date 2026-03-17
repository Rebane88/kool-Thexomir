using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.Building;
using Application.Services.Building.DTOs;
using Application.Services.GameHub;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.ApiControllers.Building;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game/{gameId:guid}")]
[ApiController]
[Authorize]
public class BuildingController(
    IBuildingService buildingService,
    IHubContext<GameHub, IGameClient> hubContext,
    IGameLockManager gameLockManager) : ControllerBase
{
    [HttpGet("building-types")]
    [ProducesResponseType(typeof(IEnumerable<BuildingTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBuildingTypes(Guid gameId)
    {
        var types = await buildingService.GetBuildingTypesAsync();
        return Ok(types);
    }

    [HttpPost("build")]
    [ProducesResponseType(typeof(BuildingPlacedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Build(Guid gameId, [FromBody] PlaceBuildingRequest request)
    {
        using var gameLock = await gameLockManager.AcquireAsync(gameId);

        var result = await buildingService.PlaceBuildingAsync(gameId, User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        await hubContext.Clients.Group($"game:{gameId}")
            .BuildingPlaced(result.Value!);

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
