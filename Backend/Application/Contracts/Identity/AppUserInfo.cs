namespace Application.Contracts.Identity;

/// <summary>
/// Application-layer identity DTO representing a user without exposing infrastructure Identity types.
/// </summary>
public record AppUserInfo(Guid Id, string Email);
