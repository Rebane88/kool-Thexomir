namespace Application.Services.WinCondition.DTOs;

public class GameOverDto
{
    public Guid GameId { get; set; }
    public Guid? WinnerKingdomId { get; set; }
    public string WinConditionType { get; set; } = string.Empty;
    public List<KingdomResultDto> FinalStandings { get; set; } = [];
    public List<Guid> EliminationOrder { get; set; } = [];
}

public class KingdomResultDto
{
    public Guid KingdomId { get; set; }
    public string KingdomName { get; set; } = string.Empty;
    public int TilesOwned { get; set; }
    public string Status { get; set; } = string.Empty;
}
