using Application.Contracts;
using Application.Services.WinCondition.DTOs;
using Domain.Game;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.ApiControllers.Game;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game/{gameId:guid}")]
[ApiController]
[Authorize]
public class GameController(IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet("results")]
    [ProducesResponseType(typeof(GameResultsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResults(Guid gameId)
    {
        var game = await unitOfWork.Games.GetByIdAsync(gameId);
        if (game is null)
            return NotFound(ProblemDetailsFor(404, "Game not found."));

        if (game.Status != EGameStatus.Completed)
            return BadRequest(ProblemDetailsFor(400, "Game is not yet completed."));

        var kingdoms = (await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId)).ToList();
        var tiles = await unitOfWork.Tiles.GetTilesWithBuildingsForGameAsync(gameId);

        var results = new GameResultsDto
        {
            GameId = game.Id,
            Status = game.Status.ToString(),
            WinnerKingdomId = game.WinnerKingdomId,
            WinConditionType = game.WinCondition.ToString(),
            TotalTurns = game.RoundNumber,
            FinalStandings = kingdoms.Select(k => new KingdomResultDto
            {
                KingdomId = k.Id,
                KingdomName = k.Name,
                TilesOwned = tiles.Count(t => t.KingdomId == k.Id),
                Status = k.Status.ToString()
            }).OrderByDescending(s => s.Status == "Active" ? 1 : 0).ThenByDescending(s => s.TilesOwned).ToList(),
            EliminationOrder = kingdoms
                .Where(k => k.Status == EKingdomStatus.Defeated)
                .OrderBy(k => k.DefeatedAt ?? k.UpdatedAt)
                .Select(k => k.Id)
                .ToList()
        };

        return Ok(results);
    }

    private static ProblemDetails ProblemDetailsFor(int status, string detail) => new()
    {
        Status = status,
        Detail = detail,
        Title = status switch
        {
            400 => "Bad Request",
            404 => "Not Found",
            _ => "Error"
        }
    };
}
