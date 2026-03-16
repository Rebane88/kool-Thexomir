using Application.Services.GameInitialization.DTOs;
using Application.Services.Lobby.DTOs;

namespace Application.Services.GameHub;

public interface IGameClient
{
    // Lobby events
    Task LobbyPlayerJoined(LobbyResponse lobby);
    Task LobbyPlayerLeft(LobbyResponse lobby);
    Task LobbyFactionSelected(LobbyResponse lobby);
    Task LobbyGameStarting();

    // Game events
    Task GameStateSnapshot(GameStateDto gameState);
    Task PlayerJoinedGame(string userId);
    Task PlayerLeftGame(string userId);
}
