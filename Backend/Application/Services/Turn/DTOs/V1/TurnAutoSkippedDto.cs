namespace Application.Services.Turn.DTOs.V1;

public class TurnAutoSkippedDto
{
    public Guid SkippedKingdomId { get; set; }
    public string SkippedKingdomName { get; set; } = string.Empty;
    public int ConsecutiveMissedTurns { get; set; }
}
