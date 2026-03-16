using Application.Services.WinCondition.DTOs;

namespace Application.Services.Military.DTOs;

public class CombatResolvedDto
{
    public Guid BattleId { get; set; }
    public Guid TileId { get; set; }
    public Guid AttackerKingdomId { get; set; }
    public Guid DefenderKingdomId { get; set; }
    public Guid? WinnerKingdomId { get; set; }
    public bool TileCaptured { get; set; }
    public decimal AttackerStrength { get; set; }
    public decimal DefenderStrength { get; set; }
    public List<CasualtyDto> AttackerCasualties { get; set; } = new();
    public List<CasualtyDto> DefenderCasualties { get; set; } = new();
    public GameOverDto? GameOver { get; set; }
}
