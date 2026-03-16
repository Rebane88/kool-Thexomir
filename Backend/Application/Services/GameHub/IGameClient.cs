using Application.Services.Building.DTOs;
using Application.Services.GameInitialization.DTOs;
using Application.Services.Lobby.DTOs;
using Application.Services.Military.DTOs;
using Application.Services.Turn.DTOs;

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

    // Phase 11 events
    Task TurnAdvanced(TurnAdvancedDto turnAdvanced);
    Task BuildingPlaced(BuildingPlacedDto buildingPlaced);

    // Phase 12 events
    Task TroopsTrained(TroopsTrainedDto troopsTrained);
    Task ArmyMoved(ArmyMovedDto armyMoved);
    Task CombatResolved(CombatResolvedDto combatResolved);
}
