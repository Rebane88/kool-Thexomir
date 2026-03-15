using Base.Contracts;
using Domain.Identity;

namespace Application.Contracts;

public interface IIdentityService
{
    Task<Result<AppUser>> GetByEmailAsync(string email);
    Task<Result<AppUser>> CreateUserAsync(string email, string password);
    Task<Result<bool>> CheckPasswordAsync(AppUser user, string password);
    Task<bool> IsLockedOutAsync(AppUser user);
    Task<Result<bool>> AddToRoleAsync(AppUser user, string role);
    Task<Result<IList<string>>> GetRolesAsync(AppUser user);

    Task<Result<string>> GenerateJwtAsync(AppUser user, DateTime expires);
    Task<Result<AppRefreshToken>> CreateRefreshTokenAsync(Guid userId);
    Task<Result<AppRefreshToken>> ValidateRefreshTokenAsync(string refreshToken);
    Task<Result<AppRefreshToken>> RotateRefreshTokenAsync(AppRefreshToken token);
    Task<Result<bool>> RevokeRefreshTokenAsync(Guid userId, string refreshToken);
}
