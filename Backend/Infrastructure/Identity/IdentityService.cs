using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Contracts;
using Application.Contracts.Identity;
using Base.Contracts;
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
    // User operations
    // ---------------------------------------------------------------

    public async Task<Result<AppUserInfo>> GetByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null
            ? Result<AppUserInfo>.Fail($"User with email '{email}' not found.")
            : Result<AppUserInfo>.Ok(new AppUserInfo(user.Id, user.Email!));
    }

    public async Task<Result<AppUserInfo>> CreateUserAsync(string email, string password)
    {
        var user = new AppUser { Email = email, UserName = email };
        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded
            ? Result<AppUserInfo>.Ok(new AppUserInfo(user.Id, user.Email!))
            : Result<AppUserInfo>.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<bool>> CheckPasswordAsync(Guid userId, string password)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<bool>.Fail("User not found.");
        var valid = await userManager.CheckPasswordAsync(user, password);
        return valid
            ? Result<bool>.Ok(true)
            : Result<bool>.Fail("Invalid password.");
    }

    public async Task<bool> IsLockedOutAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;
        return await userManager.IsLockedOutAsync(user);
    }

    public async Task<Result<bool>> AddToRoleAsync(Guid userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<bool>.Fail("User not found.");
        var result = await userManager.AddToRoleAsync(user, role);
        return result.Succeeded
            ? Result<bool>.Ok(true)
            : Result<bool>.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<IList<string>>> GetRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<IList<string>>.Fail("User not found.");
        var roles = await userManager.GetRolesAsync(user);
        return Result<IList<string>>.Ok(roles);
    }

    // ---------------------------------------------------------------
    // JWT and refresh token methods
    // ---------------------------------------------------------------

    public async Task<Result<string>> GenerateJwtAsync(Guid userId, DateTime expires)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<string>.Fail("User not found.");

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

    public async Task<Result<RefreshTokenInfo>> CreateRefreshTokenAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<RefreshTokenInfo>.Fail("User not found.");

        var token = new AppRefreshToken
        {
            RefreshToken = Guid.NewGuid().ToString(),
            Expiration = DateTime.UtcNow.AddDays(7),
            UserId = userId
        };

        await context.RefreshTokens.AddAsync(token);
        await context.SaveChangesAsync();

        return Result<RefreshTokenInfo>.Ok(new RefreshTokenInfo(userId, user.Email!, token.RefreshToken));
    }

    public async Task<Result<RefreshTokenInfo>> ValidateRefreshTokenAsync(string refreshToken)
    {
        var now = DateTime.UtcNow;
        var graceExpiry = now.AddMinutes(-1);

        var token = await context.RefreshTokens
            .Include(t => t.User)
            .Where(t => (t.RefreshToken == refreshToken && t.Expiration > now) ||
                        (t.PreviousRefreshToken == refreshToken && t.PreviousExpiration > graceExpiry))
            .FirstOrDefaultAsync();

        if (token is null)
            return Result<RefreshTokenInfo>.Fail("Invalid or expired refresh token.");

        return Result<RefreshTokenInfo>.Ok(
            new RefreshTokenInfo(token.UserId, token.User!.Email!, token.RefreshToken));
    }

    public async Task<Result<RefreshTokenInfo>> RotateRefreshTokenAsync(string currentToken)
    {
        var token = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.RefreshToken == currentToken);

        if (token is null)
            return Result<RefreshTokenInfo>.Fail("Refresh token not found.");

        token.PreviousRefreshToken = token.RefreshToken;
        token.PreviousExpiration = DateTime.UtcNow.AddMinutes(1);
        token.RefreshToken = Guid.NewGuid().ToString();
        token.Expiration = DateTime.UtcNow.AddDays(7);

        context.Entry(token).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return Result<RefreshTokenInfo>.Ok(
            new RefreshTokenInfo(token.UserId, token.User!.Email!, token.RefreshToken));
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
    // Email resolution
    // ---------------------------------------------------------------

    public async Task<string?> GetEmailAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user?.Email;
    }

    public async Task<Dictionary<Guid, string>> GetEmailsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        return await context.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? string.Empty);
    }
}
