using System.ComponentModel.DataAnnotations;
using Domain.Game;

namespace API.Areas.Public.ViewModels;

public class CreateLobbyViewModel
{
    [Required]
    [Range(2, 8, ErrorMessage = "Lobby_MaxPlayersRange")]
    [Display(Name = "Lobby_MaxPlayers")]
    public int MaxPlayers { get; set; } = 4;

    [Required]
    [Display(Name = "Lobby_WinCondition")]
    public EWinCondition WinCondition { get; set; } = EWinCondition.Elimination;
}
