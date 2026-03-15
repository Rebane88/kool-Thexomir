namespace Application.Services.Auth.DTOs;

public class RefreshRequest
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}
