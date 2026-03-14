using Base.Contracts;
using Domain.Identity;

namespace Application.Contracts;

public interface IIdentityService
{
    Task<Result<AppUser>> GetByEmailAsync(string email);
    Task<Result<AppUser>> CreateUserAsync(string email, string password);
    Task<Result<bool>> CheckPasswordAsync(AppUser user, string password);
    Task<Result<bool>> AddToRoleAsync(AppUser user, string role);
    Task<Result<IList<string>>> GetRolesAsync(AppUser user);
}
