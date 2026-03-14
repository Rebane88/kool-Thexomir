using Application.Contracts;
using Base.Contracts;
using Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class IdentityService(UserManager<AppUser> userManager) : IIdentityService
{
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
}
