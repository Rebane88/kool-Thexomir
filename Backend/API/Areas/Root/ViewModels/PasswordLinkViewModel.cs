using Domain.Identity;

namespace API.Areas.Root.ViewModels;

public class PasswordLinkViewModel
{
    public string PasswordLink { get; set; } = default!;
    public AppUser AppUser { get; set; } = default!;
}
