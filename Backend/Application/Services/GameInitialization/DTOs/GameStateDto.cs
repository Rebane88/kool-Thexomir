using Application.Services.Combat.DTOs;

namespace Application.Services.GameInitialization.DTOs;

public class GameStateDto
{
    public Guid GameId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public string WinCondition { get; set; } = string.Empty;
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    public Guid? CurrentTurnKingdomId { get; set; }
    public string? CurrentPhase { get; set; }
    public int? RemainingActionPoints { get; set; }
    public List<DeclareAttackResponse> DeclaredAttacks { get; set; } = [];
    public List<TileDto> Tiles { get; set; } = [];
    public List<KingdomDto> Kingdoms { get; set; } = [];
    public List<ArmyDto> Armies { get; set; } = [];
}
