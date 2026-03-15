namespace API.Areas.Root.ViewModels;

public class UserListViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public IList<string> Roles { get; set; } = [];
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}
