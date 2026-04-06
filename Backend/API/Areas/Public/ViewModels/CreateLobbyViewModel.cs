using System.ComponentModel.DataAnnotations;
using Domain.Game;

namespace API.Areas.Public.ViewModels;

public class CreateLobbyViewModel
{
    [Required]
    [Range(2, 8, ErrorMessage = "MaxPlayers must be between 2 and 8.")]
    [Display(Name = "Max Players")]
    public int MaxPlayers { get; set; } = 4;

    [Required]
    [Display(Name = "Win Condition")]
    public EWinCondition WinCondition { get; set; } = EWinCondition.Elimination;
}
