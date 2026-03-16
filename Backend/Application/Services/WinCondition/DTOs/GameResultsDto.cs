namespace Application.Services.WinCondition.DTOs;

public class GameResultsDto
{
    public Guid GameId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? WinnerKingdomId { get; set; }
    public string WinConditionType { get; set; } = string.Empty;
    public int TotalTurns { get; set; }
    public List<KingdomScoreDto> FinalScores { get; set; } = [];
    public List<Guid> EliminationOrder { get; set; } = [];
}
