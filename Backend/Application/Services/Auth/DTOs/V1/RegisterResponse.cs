namespace Application.Services.Auth.DTOs.V1;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public IList<string> Roles { get; set; } = [];
}
