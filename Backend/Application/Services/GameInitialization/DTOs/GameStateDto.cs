namespace Application.Services.GameInitialization.DTOs;

public class GameStateDto
{
    public Guid GameId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public string WinCondition { get; set; } = string.Empty;
    public int MapRadius { get; set; }
    public Guid? CurrentTurnKingdomId { get; set; }
    public List<TileDto> Tiles { get; set; } = [];
    public List<KingdomDto> Kingdoms { get; set; } = [];
    public List<ArmyDto> Armies { get; set; } = [];
}
