using Domain.Game;

namespace Application.Services.Lobby.DTOs;

public class CreateLobbyRequest
{
    public int MaxPlayers { get; set; } = 8;
    public EWinCondition WinCondition { get; set; }
    public int? MaxTurnCount { get; set; }
}
