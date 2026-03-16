namespace Application.Services.Turn.DTOs;

public class TurnAdvancedDto
{
    public Guid NewKingdomId { get; set; }
    public int TurnNumber { get; set; }
    public Dictionary<string, int> IncomeApplied { get; set; } = new();
}
