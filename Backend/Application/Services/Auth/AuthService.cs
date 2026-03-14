using Application.Contracts;
using Application.Services.Auth.DTOs;
using Base.Contracts;

namespace Application.Services.Auth;

public class AuthService(IIdentityService identityService, IUnitOfWork unitOfWork) : IAuthService
{
    // ReSharper disable once NotAccessedField.Local
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request)
    {
        var createResult = await identityService.CreateUserAsync(request.Email, request.Password);
        if (!createResult.IsSuccess)
            return Result<RegisterResponse>.Fail(createResult.Error!);

        var user = createResult.Value!;

        var roleResult = await identityService.AddToRoleAsync(user, "Player");
        if (!roleResult.IsSuccess)
            return Result<RegisterResponse>.Fail(roleResult.Error!);

        return Result<RegisterResponse>.Ok(new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email!
        });
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var userResult = await identityService.GetByEmailAsync(request.Email);
        if (!userResult.IsSuccess)
            return Result<LoginResponse>.Fail("Invalid credentials.");

        var user = userResult.Value!;

        var passwordResult = await identityService.CheckPasswordAsync(user, request.Password);
        if (!passwordResult.IsSuccess)
            return Result<LoginResponse>.Fail("Invalid credentials.");

        var rolesResult = await identityService.GetRolesAsync(user);

        return Result<LoginResponse>.Ok(new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            Roles = rolesResult.Value!
        });
    }
}
