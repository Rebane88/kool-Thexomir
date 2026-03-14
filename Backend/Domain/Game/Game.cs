using Base;
using Domain.Map;

namespace Domain.Game;

public class Game : BaseEntity
{
    public EGameStatus Status { get; set; }
    public int TurnNumber { get; set; }
    public EWinCondition WinCondition { get; set; }
    public int MaxPlayers { get; set; }
    public string LobbyCode { get; set; } = string.Empty; // 6-char unique code
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }

    // Navigation
    public ICollection<Kingdom>? Kingdoms { get; set; }
    public ICollection<Tile>? Tiles { get; set; }
    public ICollection<TurnLog>? TurnLogs { get; set; }
}
