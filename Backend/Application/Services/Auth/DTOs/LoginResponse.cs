namespace Application.Services.Auth.DTOs;

public class LoginResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public IList<string> Roles { get; set; } = [];
}
