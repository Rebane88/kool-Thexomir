using Domain.Game;

namespace API.Areas.Root.ViewModels;

public class GameListViewModel
{
    public Guid Id { get; set; }
    public string LobbyCode { get; set; } = string.Empty;
    public string? HostName { get; set; }
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public EGameStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
