using Application.Contracts;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.ApiControllers.Game;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game")]
[ApiController]
[Authorize]
public class GameSessionController(IUnitOfWork unitOfWork) : ControllerBase
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
}
