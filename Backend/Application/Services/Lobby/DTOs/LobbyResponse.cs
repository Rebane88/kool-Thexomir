using Domain.Game;

namespace Application.Services.Lobby.DTOs;

public class LobbyResponse
{
    public Guid Id { get; set; }
    public string LobbyCode { get; set; } = string.Empty;
    public EGameStatus Status { get; set; }
    public int MaxPlayers { get; set; }
    public EWinCondition WinCondition { get; set; }
    public Guid? HostUserId { get; set; }
    public int PlayerCount { get; set; }
    public List<PlayerInLobbyDto> Players { get; set; } = [];
    public List<FactionAvailabilityDto> Factions { get; set; } = [];
}
