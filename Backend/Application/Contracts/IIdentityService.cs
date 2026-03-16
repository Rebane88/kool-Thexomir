using Application.Contracts.Identity;
using Base.Contracts;

namespace Application.Contracts;

public interface IIdentityService
{
    Task<Result<AppUserInfo>> GetByEmailAsync(string email);
    Task<Result<AppUserInfo>> CreateUserAsync(string email, string password);
    Task<Result<bool>> CheckPasswordAsync(Guid userId, string password);
    Task<bool> IsLockedOutAsync(Guid userId);
    Task<Result<bool>> AddToRoleAsync(Guid userId, string role);
    Task<Result<IList<string>>> GetRolesAsync(Guid userId);

    Task<Result<string>> GenerateJwtAsync(Guid userId, DateTime expires);
    Task<Result<RefreshTokenInfo>> CreateRefreshTokenAsync(Guid userId);
    Task<Result<RefreshTokenInfo>> ValidateRefreshTokenAsync(string refreshToken);
    Task<Result<RefreshTokenInfo>> RotateRefreshTokenAsync(string currentToken);
    Task<Result<bool>> RevokeRefreshTokenAsync(Guid userId, string refreshToken);

    Task<string?> GetEmailAsync(Guid userId);
    Task<Dictionary<Guid, string>> GetEmailsAsync(IEnumerable<Guid> userIds);
}
