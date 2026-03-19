using Application.Services.Army.DTOs;
using Application.Services.Building.DTOs;
using Application.Services.GameInitialization.DTOs;
using Application.Services.Lobby.DTOs;
using Application.Services.SlotMachine.DTOs;
using Application.Services.Turn.DTOs;
using Application.Services.WinCondition.DTOs;

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

    // Turn lifecycle events
    Task PhaseChanged(PhaseChangedDto phaseChanged);
    Task TurnStarted(TurnStartedDto turnStarted);
    Task RoundStarted(RoundStartedDto roundStarted);

    // Slot machine events
    Task SlotMachineSpun(SpinResultDto spinResult);

    // Army events
    Task ArmyTrained(ArmyTrainedDto armyTrained);

    // Phase 13 events
    Task GameOver(GameOverDto gameOver);
}
