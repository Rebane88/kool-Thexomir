namespace Application.Services.Turn.DTOs;

public class TurnStartedDto
{
    public Guid KingdomId { get; set; }
    public string KingdomName { get; set; } = string.Empty;
    public int ActionPoints { get; set; }
    public DateTime? TurnDeadline { get; set; }
    public int RoundNumber { get; set; }
}
