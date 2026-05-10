namespace Application.Services.Turn.DTOs.V1;

public class PhaseChangedDto
{
    public string Phase { get; set; } = string.Empty;
    public string PreviousPhase { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
}
