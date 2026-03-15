namespace Application.Services.Auth.DTOs;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = default!;
}
