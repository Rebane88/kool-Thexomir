using API.Extensions;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.ApiControllers.Lobby;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lobby")]
[ApiController]
[Authorize]
public class LobbyController(ILobbyService lobbyService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateLobbyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLobbyRequest request)
    {
        var result = await lobbyService.CreateLobbyAsync(User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        return CreatedAtAction(nameof(GetLobby), new { id = result.Value!.LobbyId }, result.Value);
    }

    [HttpPost("join")]
    [ProducesResponseType(typeof(LobbyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Join([FromBody] JoinLobbyRequest request)
    {
        var result = await lobbyService.JoinLobbyAsync(User.UserId(), request);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Leave(Guid id)
    {
        var result = await lobbyService.LeaveLobbyAsync(User.UserId(), id);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        return NoContent();
    }

    [HttpPost("{id:guid}/faction/{factionTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SelectFaction(Guid id, Guid factionTypeId)
    {
        var result = await lobbyService.SelectFactionAsync(User.UserId(), id, factionTypeId);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        return Ok();
    }

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start(Guid id)
    {
        var result = await lobbyService.StartGameAsync(User.UserId(), id);
        if (!result.IsSuccess)
            return BadRequest(ProblemDetailsFor(400, result.Error!));

        return Ok();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LobbyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLobby(Guid id)
    {
        var result = await lobbyService.GetLobbyAsync(id);
        if (!result.IsSuccess)
            return NotFound(ProblemDetailsFor(404, result.Error!));

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
            409 => "Conflict",
            _ => "Error"
        }
    };
}
