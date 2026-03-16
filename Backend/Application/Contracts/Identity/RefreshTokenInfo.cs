namespace Application.Contracts.Identity;

/// <summary>
/// Application-layer DTO for refresh token data without exposing infrastructure Identity types.
/// </summary>
public record RefreshTokenInfo(Guid UserId, string Email, string Token);
