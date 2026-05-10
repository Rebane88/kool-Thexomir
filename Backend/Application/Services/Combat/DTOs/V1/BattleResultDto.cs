namespace Application.Services.Combat.DTOs.V1;

public class BattleResultDto
{
    public Guid BattleId { get; set; }
    public Guid AttackerKingdomId { get; set; }
    public Guid DefenderKingdomId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public Guid TileCapturedId { get; set; }
    public Guid TileCapturedFromKingdomId { get; set; }
    public List<BattleRoundResultDto> Rounds { get; set; } = [];
}
