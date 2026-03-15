using Application.Services.Auth.DTOs;
using Base.Contracts;

namespace Application.Services.Auth;

public interface IAuthService
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result<RefreshResponse>> RefreshAsync(string accessToken, string refreshToken);
    Task<Result<bool>> LogoutAsync(Guid userId, string refreshToken);
}
