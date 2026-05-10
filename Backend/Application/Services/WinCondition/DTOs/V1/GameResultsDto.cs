namespace Application.Services.WinCondition.DTOs.V1;

public class GameResultsDto
{
    public Guid GameId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? WinnerKingdomId { get; set; }
    public string WinConditionType { get; set; } = string.Empty;
    public int TotalTurns { get; set; }
    public List<KingdomResultDto> FinalStandings { get; set; } = [];
    public List<Guid> EliminationOrder { get; set; } = [];
}
