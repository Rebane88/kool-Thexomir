using Application.Services.Auth;
using Application.Services.Auth.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Extensions;

namespace API.ApiControllers.Auth;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        if (!result.IsSuccess)
        {
            return Conflict(ProblemDetailsFor(409, result.Error!));
        }
        return CreatedAtAction(nameof(Register), result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(ProblemDetailsFor(401, result.Error!));
        }
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await authService.RefreshAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(ProblemDetailsFor(401, result.Error!));
        }
        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = User.UserId();
        var result = await authService.LogoutAsync(userId, request);
        if (!result.IsSuccess)
        {
            return BadRequest(ProblemDetailsFor(400, result.Error!));
        }
        return NoContent();
    }

    private static ProblemDetails ProblemDetailsFor(int status, string detail) => new()
    {
        Status = status,
        Detail = detail,
        Title = status switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            409 => "Conflict",
            _ => "Error"
        }
    };
}
