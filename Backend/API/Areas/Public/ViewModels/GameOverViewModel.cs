using Application.Services.WinCondition.DTOs;

namespace API.Areas.Public.ViewModels;

public class GameOverViewModel
{
    public Guid GameId { get; set; }
    public GameOverDto GameOver { get; set; } = null!;
    public Guid? MyKingdomId { get; set; }
    public bool IWon => MyKingdomId.HasValue && GameOver.WinnerKingdomId == MyKingdomId;
}
