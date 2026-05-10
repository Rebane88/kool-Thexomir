namespace Application.Services.Auth.DTOs.V1;

public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}
