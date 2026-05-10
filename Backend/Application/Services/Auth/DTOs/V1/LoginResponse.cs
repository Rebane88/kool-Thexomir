namespace Application.Services.Auth.DTOs.V1;

public class LoginResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public IList<string> Roles { get; set; } = [];
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}
