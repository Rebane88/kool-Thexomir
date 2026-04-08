using System.ComponentModel.DataAnnotations;

namespace API.Areas.Public.ViewModels;

public class JoinLobbyViewModel
{
    [Required(ErrorMessage = "Lobby_InviteCodeRequired")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Lobby_InviteCodeLength")]
    [Display(Name = "Lobby_InviteCode")]
    public string InviteCode { get; set; } = string.Empty;
}
