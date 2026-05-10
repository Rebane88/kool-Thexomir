using Application.Services.Combat.DTOs.V1;
using Application.Services.WinCondition.DTOs.V1;

namespace Application.Services.Turn.DTOs.V1;

public class TurnAdvancedDto
{
    public Guid? NextKingdomId { get; set; }
    public int RoundNumber { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public int? ActionPoints { get; set; }
    public DateTime? TurnDeadline { get; set; }
    public Dictionary<Guid, Dictionary<string, int>>? IncomeApplied { get; set; }
    public List<BattleResultDto>? BattleResults { get; set; }
    public bool PhaseChanged { get; set; }
    public GameOverDto? GameOver { get; set; }
}
