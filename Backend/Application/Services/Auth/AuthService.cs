using Application.Contracts;
using Application.Services.Auth.DTOs;
using Base.Contracts;

namespace Application.Services.Auth;

public class AuthService(IIdentityService identityService, IUnitOfWork unitOfWork) : IAuthService
{
    // ReSharper disable once NotAccessedField.Local
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private static readonly Random _random = new();

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request)
    {
        var createResult = await identityService.CreateUserAsync(request.Email, request.Password);
        if (!createResult.IsSuccess)
            return Result<RegisterResponse>.Fail(createResult.Error!);

        var user = createResult.Value!;

        var roleResult = await identityService.AddToRoleAsync(user, "Player");
        if (!roleResult.IsSuccess)
            return Result<RegisterResponse>.Fail(roleResult.Error!);

        var rolesResult = await identityService.GetRolesAsync(user);

        return Result<RegisterResponse>.Ok(new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            Roles = rolesResult.Value!
        });
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var userResult = await identityService.GetByEmailAsync(request.Email);
        if (!userResult.IsSuccess)
        {
            await Task.Delay(_random.Next(500, 5001));
            return Result<LoginResponse>.Fail("Invalid credentials.");
        }

        var user = userResult.Value!;

        if (await identityService.IsLockedOutAsync(user))
        {
            await Task.Delay(_random.Next(500, 5001));
            return Result<LoginResponse>.Fail("Account is locked.");
        }

        var passwordResult = await identityService.CheckPasswordAsync(user, request.Password);
        if (!passwordResult.IsSuccess)
        {
            await Task.Delay(_random.Next(500, 5001));
            return Result<LoginResponse>.Fail("Invalid credentials.");
        }

        var rolesResult = await identityService.GetRolesAsync(user);

        var jwtResult = await identityService.GenerateJwtAsync(user, DateTime.UtcNow.AddMinutes(15));
        if (!jwtResult.IsSuccess)
            return Result<LoginResponse>.Fail(jwtResult.Error!);

        var refreshResult = await identityService.CreateRefreshTokenAsync(user.Id);
        if (!refreshResult.IsSuccess)
            return Result<LoginResponse>.Fail(refreshResult.Error!);

        return Result<LoginResponse>.Ok(new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            Roles = rolesResult.Value!,
            AccessToken = jwtResult.Value!,
            RefreshToken = refreshResult.Value!.RefreshToken
        });
    }

    public async Task<Result<RefreshResponse>> RefreshAsync(string refreshToken)
    {
        var validateResult = await identityService.ValidateRefreshTokenAsync(refreshToken);
        if (!validateResult.IsSuccess)
            return Result<RefreshResponse>.Fail(validateResult.Error!);

        var refreshTokenEntity = validateResult.Value!;
        var user = refreshTokenEntity.User!;

        if (await identityService.IsLockedOutAsync(user))
            return Result<RefreshResponse>.Fail("Account is locked.");

        var rolesResult = await identityService.GetRolesAsync(user);

        var newJwtResult = await identityService.GenerateJwtAsync(user, DateTime.UtcNow.AddMinutes(15));
        if (!newJwtResult.IsSuccess)
            return Result<RefreshResponse>.Fail(newJwtResult.Error!);

        var rotateResult = await identityService.RotateRefreshTokenAsync(refreshTokenEntity);
        if (!rotateResult.IsSuccess)
            return Result<RefreshResponse>.Fail(rotateResult.Error!);

        return Result<RefreshResponse>.Ok(new RefreshResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            Roles = rolesResult.Value!,
            AccessToken = newJwtResult.Value!,
            RefreshToken = rotateResult.Value!.RefreshToken
        });
    }

    public async Task<Result<bool>> LogoutAsync(Guid userId, string refreshToken)
    {
        return await identityService.RevokeRefreshTokenAsync(userId, refreshToken);
    }
}
