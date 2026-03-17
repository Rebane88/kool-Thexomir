import * as signalR from '@microsoft/signalr';
import { createGameHubConnection } from '@/lib/signalr-client';
import { useAuthStore } from '@/features/auth/auth-store';
import { useGameStore } from './game-store';
import type {
  GameStateSnapshot,
  TurnAdvancedEvent,
  BuildingPlacedEvent,
  TroopsTrainedEvent,
  ArmyMovedEvent,
  CombatResolvedEvent,
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

  // Register event handlers using getState() (never hooks)
  connection.on('GameStateSnapshot', (snapshot: GameStateSnapshot) => {
    const userId = useAuthStore.getState().user?.id;
    if (userId) {
      useGameStore.getState().loadSnapshot(snapshot, userId);
    }
    useGameStore.getState().setConnectionStatus('connected');
  });

  connection.on('TurnAdvanced', (data: TurnAdvancedEvent) => {
    useGameStore.getState().handleTurnAdvanced(data);
  });

  connection.on('BuildingPlaced', (data: BuildingPlacedEvent) => {
    useGameStore.getState().handleBuildingPlaced(data);
  });

  connection.on('TroopsTrained', (data: TroopsTrainedEvent) => {
    useGameStore.getState().handleTroopsTrained(data);
  });

  connection.on('ArmyMoved', (data: ArmyMovedEvent) => {
    useGameStore.getState().handleArmyMoved(data);
  });

  connection.on('CombatResolved', (data: CombatResolvedEvent) => {
    useGameStore.getState().handleCombatResolved(data);
  });

  connection.on('GameOver', (data: GameOverEvent) => {
    useGameStore.getState().handleGameOver(data);
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
