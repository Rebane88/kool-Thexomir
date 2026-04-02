import * as signalR from '@microsoft/signalr';
import { createGameHubConnection } from '@/lib/signalr-client';
import { useAuthStore } from '@/features/auth/auth-store';
import { useGameStore } from './game-store';
import { useAnimationStore } from './animation-store';
import type { GamePhase } from './types/enums';
import type {
  GameStateSnapshot,
  TurnAdvancedEvent,
  TurnAutoSkippedEvent,
  BuildingPlacedEvent,
  PhaseChangedEvent,
  TurnStartedEvent,
  RoundStartedEvent,
  SlotMachineSpunEvent,
  ArmyTrainedEvent,
  AttackDeclaredEvent,
  ArmiesSelectedEvent,
  LineupSetEvent,
  BattleResolvedEvent,
  BattleRoundResolvedEvent,
  GameOverEvent,
} from './types';

let connection: signalR.HubConnection | null = null;
let generation = 0;

export async function connectToGame(gameId: string): Promise<void> {
  const thisGen = ++generation;

  // Clean up any existing connection
  if (connection) {
    await connection.stop();
    connection = null;
  }

  // Superseded by a newer connect/disconnect call (e.g. StrictMode double-mount)
  if (thisGen !== generation) return;

  useGameStore.getState().setConnectionStatus('connecting');

  connection = createGameHubConnection(gameId);

  // --- Game state ---
  connection.on('GameStateSnapshot', (snapshot: GameStateSnapshot) => {
    const userId = useAuthStore.getState().user?.id;
    if (userId) {
      useGameStore.getState().loadSnapshot(snapshot, userId);
    }
    useGameStore.getState().setConnectionStatus('connected');
  });

  // --- Turn lifecycle ---
  connection.on('TurnAdvanced', (data: TurnAdvancedEvent) => {
    useGameStore.getState().handleTurnAdvanced(data);
  });

  connection.on('PhaseChanged', (data: PhaseChangedEvent) => {
    useGameStore.getState().handlePhaseChanged(data);
    useAnimationStore.getState().showPhaseBanner(
      data.phase as GamePhase,
      data.roundNumber,
    );
  });

  connection.on('TurnStarted', (data: TurnStartedEvent) => {
    useGameStore.getState().handleTurnStarted(data);
  });

  connection.on('RoundStarted', (data: RoundStartedEvent) => {
    useGameStore.getState().handleRoundStarted(data);
  });

  // --- Building ---
  connection.on('BuildingPlaced', (data: BuildingPlacedEvent) => {
    useGameStore.getState().handleBuildingPlaced(data);
  });

  // --- Slot machine ---
  connection.on('SlotMachineSpun', (data: SlotMachineSpunEvent) => {
    useGameStore.getState().handleSlotMachineSpun(data);
    // Only show animation for the player who spun
    const myKingdomId = useGameStore.getState().myKingdomId;
    if (data.kingdomId === myKingdomId) {
      useAnimationStore.getState().setSlotResult({
        outcome: data.outcome,
        actionPointsAfter: data.actionPointsAfter,
      });
    }
  });

  // --- Army ---
  connection.on('ArmyTrained', (data: ArmyTrainedEvent) => {
    useGameStore.getState().handleArmyTrained(data);
  });

  // --- Combat flow ---
  connection.on('AttackDeclared', (data: AttackDeclaredEvent) => {
    useGameStore.getState().handleAttackDeclared(data);
  });

  connection.on('ArmiesSelected', (data: ArmiesSelectedEvent) => {
    useGameStore.getState().handleArmiesSelected(data);
  });

  connection.on('LineupSet', (data: LineupSetEvent) => {
    useGameStore.getState().handleLineupSet(data);
  });

  connection.on('BattleRoundResolved', (data: BattleRoundResolvedEvent) => {
    useAnimationStore.getState().receiveRound(data, data.battleId);
  });

  connection.on('BattleResolved', (data: BattleResolvedEvent) => {
    useGameStore.getState().handleBattleResolved(data);
    // Don't call startCombatPlayback — rounds already received via BattleRoundResolved
  });

  // --- Timeout / abandonment ---
  connection.on('TurnAutoSkipped', (data: TurnAutoSkippedEvent) => {
    console.info(`[AutoSkip] ${data.skippedKingdomName} missed turn (${data.consecutiveMissedTurns}/3)`);
  });

  // --- Game over ---
  connection.on('GameOver', (data: GameOverEvent) => {
    useGameStore.getState().handleGameOver(data);
  });

  // --- Presence (no-op handlers for SYNC-03 compliance) ---
  connection.on('PlayerJoinedGame', (_userId: string) => {
    // Presence indicator -- no game state impact
  });

  connection.on('PlayerLeftGame', (_userId: string) => {
    // Presence indicator -- no game state impact
  });

  // Connection lifecycle
  connection.onreconnecting(() => {
    useGameStore.getState().setConnectionStatus('reconnecting');
  });

  connection.onreconnected(() => {
    useGameStore.getState().setConnectionStatus('connected');
    // Request fresh snapshot since OnConnectedAsync does NOT re-fire on auto-reconnect
    connection?.invoke('RequestGameSnapshot');
  });

  connection.onclose(() => {
    useGameStore.getState().setConnectionStatus('disconnected');
  });

  try {
    await connection.start();
    // Note: connectionStatus set to 'connected' in GameStateSnapshot handler,
    // since that's when we actually have data to display
  } catch {
    // Ignore errors from superseded connections (StrictMode double-mount)
    if (thisGen !== generation) return;
    useGameStore.getState().setConnectionStatus('failed');
  }
}

export function disconnectFromGame(): void {
  generation++;
  connection?.stop();
  connection = null;
  useGameStore.getState().resetState();
}
