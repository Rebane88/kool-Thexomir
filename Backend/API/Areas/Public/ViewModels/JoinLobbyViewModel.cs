using System.ComponentModel.DataAnnotations;

namespace API.Areas.Public.ViewModels;

public class JoinLobbyViewModel
{
    [Required(ErrorMessage = "Invite code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Invite code must be exactly 6 characters.")]
    [Display(Name = "Invite Code")]
    public string InviteCode { get; set; } = string.Empty;
}
