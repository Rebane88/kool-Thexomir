using Application.Services.Combat.DTOs;
using Application.Services.WinCondition.DTOs;

namespace Application.Services.Turn.DTOs;

public class TurnAdvancedDto
{
    public Guid? NextKingdomId { get; set; }
    public int RoundNumber { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public int? ActionPoints { get; set; }
    public DateTime? TurnDeadline { get; set; }
    public Dictionary<string, int>? IncomeApplied { get; set; }
    public List<BattleResultDto>? BattleResults { get; set; }
    public bool PhaseChanged { get; set; }
    public GameOverDto? GameOver { get; set; }
}
