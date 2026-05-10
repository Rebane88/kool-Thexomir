using Application.Services.Auth;
using Application.Services.Auth.DTOs.V1;
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(ProblemDetailsFor(401, result.Error!));
        }
        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(new
        {
            result.Value!.UserId,
            result.Value!.Email,
            result.Value!.Roles,
            result.Value!.AccessToken
        });
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ProblemDetailsFor(401, "No refresh token provided."));
        }
        var result = await authService.RefreshAsync(refreshToken);
        if (!result.IsSuccess)
        {
            return Unauthorized(ProblemDetailsFor(401, result.Error!));
        }
        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(new
        {
            result.Value!.UserId,
            result.Value!.Email,
            result.Value!.Roles,
            result.Value!.AccessToken
        });
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.UserId();
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(ProblemDetailsFor(400, "No refresh token provided."));
        }
        var result = await authService.LogoutAsync(userId, refreshToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ProblemDetailsFor(400, result.Error!));
        }
        ClearRefreshTokenCookie();
        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,    // false for local dev (HTTP); set true in production
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            MaxAge = TimeSpan.FromDays(7)
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth"
        });
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
