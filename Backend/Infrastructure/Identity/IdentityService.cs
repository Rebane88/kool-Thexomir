using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Contracts;
using Base.Contracts;
using Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

public class IdentityService(
    UserManager<AppUser> userManager,
    IConfiguration configuration,
    AppDbContext context) : IIdentityService
{
    private static readonly JwtSecurityTokenHandler JwtHandler = new();

    // ---------------------------------------------------------------
    // Existing methods
    // ---------------------------------------------------------------

    public async Task<Result<AppUser>> GetByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null
            ? Result<AppUser>.Fail($"User with email '{email}' not found.")
            : Result<AppUser>.Ok(user);
    }

    public async Task<Result<AppUser>> CreateUserAsync(string email, string password)
    {
        var user = new AppUser { Email = email, UserName = email };
        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded
            ? Result<AppUser>.Ok(user)
            : Result<AppUser>.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<bool>> CheckPasswordAsync(AppUser user, string password)
    {
        var valid = await userManager.CheckPasswordAsync(user, password);
        return valid
            ? Result<bool>.Ok(true)
            : Result<bool>.Fail("Invalid password.");
    }

    public async Task<Result<bool>> AddToRoleAsync(AppUser user, string role)
    {
        var result = await userManager.AddToRoleAsync(user, role);
        return result.Succeeded
            ? Result<bool>.Ok(true)
            : Result<bool>.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<IList<string>>> GetRolesAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return Result<IList<string>>.Ok(roles);
    }

    // ---------------------------------------------------------------
    // JWT and refresh token methods
    // ---------------------------------------------------------------

    public async Task<Result<string>> GenerateJwtAsync(AppUser user, DateTime expires)
    {
        var key = configuration["JWT:Key"]!;
        var issuer = configuration["JWT:Issuer"]!;
        var audience = configuration["JWT:Audience"]!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!)
        };

        var userClaims = await userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha512);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: signingCredentials
        );

        return Result<string>.Ok(JwtHandler.WriteToken(token));
    }

    public async Task<Result<AppRefreshToken>> CreateRefreshTokenAsync(Guid userId)
    {
        var token = new AppRefreshToken
        {
            RefreshToken = Guid.NewGuid().ToString(),
            Expiration = DateTime.UtcNow.AddDays(7),
            UserId = userId
        };

        await context.RefreshTokens.AddAsync(token);
        await context.SaveChangesAsync();

        return Result<AppRefreshToken>.Ok(token);
    }

    public async Task<Result<AppRefreshToken>> ValidateRefreshTokenAsync(string expiredJwt, string refreshToken)
    {
        Guid userId;
        try
        {
            var jwtToken = JwtHandler.ReadJwtToken(expiredJwt);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                              ?? jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out userId))
                return Result<AppRefreshToken>.Fail("Invalid access token.");
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or SecurityTokenMalformedException)
        {
            return Result<AppRefreshToken>.Fail("Malformed access token.");
        }

        if (!ValidateJwtSignatureOnly(expiredJwt))
            return Result<AppRefreshToken>.Fail("Invalid access token signature.");

        var now = DateTime.UtcNow;
        var graceExpiry = now.AddMinutes(-1);

        // Match either the current refresh token (not expired) or the previous token (within grace period)
        var token = await context.RefreshTokens
            .Where(t => t.UserId == userId &&
                        ((t.RefreshToken == refreshToken && t.Expiration > now) ||
                         (t.PreviousRefreshToken == refreshToken && t.PreviousExpiration > graceExpiry)))
            .FirstOrDefaultAsync();

        if (token is null)
            return Result<AppRefreshToken>.Fail("Invalid or expired refresh token.");

        return Result<AppRefreshToken>.Ok(token);
    }

    public async Task<Result<AppRefreshToken>> RotateRefreshTokenAsync(AppRefreshToken token)
    {
        token.PreviousRefreshToken = token.RefreshToken;
        token.PreviousExpiration = DateTime.UtcNow.AddMinutes(1);
        token.RefreshToken = Guid.NewGuid().ToString();
        token.Expiration = DateTime.UtcNow.AddDays(7);

        context.Entry(token).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return Result<AppRefreshToken>.Ok(token);
    }

    public async Task<Result<bool>> RevokeRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var token = await context.RefreshTokens
            .Where(t => t.UserId == userId &&
                        (t.RefreshToken == refreshToken || t.PreviousRefreshToken == refreshToken))
            .FirstOrDefaultAsync();

        if (token is null)
            return Result<bool>.Fail("Refresh token not found.");

        context.RefreshTokens.Remove(token);
        await context.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }

    // ---------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------

    private bool ValidateJwtSignatureOnly(string jwt)
    {
        var key = configuration["JWT:Key"]!;
        var issuer = configuration["JWT:Issuer"]!;
        var audience = configuration["JWT:Audience"]!;

        var validationParams = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidateIssuer = true,
            ValidAudience = audience,
            ValidateAudience = true,
            ValidateLifetime = false
        };

        try
        {
            JwtHandler.ValidateToken(jwt, validationParams, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
