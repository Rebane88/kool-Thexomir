import { describe, it, expect, vi, beforeEach } from 'vitest';

// Track .on() registrations
const onHandlers = new Map<string, Function>();
const mockConnection = {
  on: vi.fn((event: string, handler: Function) => {
    onHandlers.set(event, handler);
  }),
  start: vi.fn(() => Promise.resolve()),
  stop: vi.fn(() => Promise.resolve()),
  invoke: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
  onclose: vi.fn(),
};

vi.mock('@/lib/signalr-client', () => ({
  createGameHubConnection: vi.fn(() => mockConnection),
}));

vi.mock('@/features/auth/auth-store', () => ({
  useAuthStore: { getState: () => ({ user: { id: 'user-1' } }) },
}));

// Mock game store with spy handlers
const mockGameStoreState = {
  setConnectionStatus: vi.fn(),
  loadSnapshot: vi.fn(),
  handleTurnAdvanced: vi.fn(),
  handleBuildingPlaced: vi.fn(),
  handlePhaseChanged: vi.fn(),
  handleTurnStarted: vi.fn(),
  handleRoundStarted: vi.fn(),
  handleSlotMachineSpun: vi.fn(),
  handleArmyTrained: vi.fn(),
  handleAttackDeclared: vi.fn(),
  handleArmiesSelected: vi.fn(),
  handleLineupSet: vi.fn(),
  handleBattleResolved: vi.fn(),
  handleGameOver: vi.fn(),
  resetState: vi.fn(),
};

vi.mock('./game-store', () => ({
  useGameStore: { getState: () => mockGameStoreState },
}));

const mockAnimationStoreState = {
  setSlotResult: vi.fn(),
  startCombatPlayback: vi.fn(),
};

vi.mock('./animation-store', () => ({
  useAnimationStore: { getState: () => mockAnimationStoreState },
}));

import { connectToGame, disconnectFromGame } from './game-hub';

describe('game-hub', () => {
  beforeEach(() => {
    onHandlers.clear();
    vi.clearAllMocks();
  });

  describe('event handler registration', () => {
    const EXPECTED_EVENTS = [
      'GameStateSnapshot',
      'TurnAdvanced',
      'PhaseChanged',
      'TurnStarted',
      'RoundStarted',
      'BuildingPlaced',
      'SlotMachineSpun',
      'ArmyTrained',
      'AttackDeclared',
      'ArmiesSelected',
      'LineupSet',
      'BattleResolved',
      'GameOver',
      'PlayerJoinedGame',
      'PlayerLeftGame',
    ];

    it('registers all 15 event handlers', async () => {
      await connectToGame('game-1');

      for (const event of EXPECTED_EVENTS) {
        expect(onHandlers.has(event), `missing handler for ${event}`).toBe(true);
      }
      expect(onHandlers.size).toBe(15);
    });

    it('does NOT register old v5.0 event names', async () => {
      await connectToGame('game-1');

      expect(onHandlers.has('TroopsTrained')).toBe(false);
      expect(onHandlers.has('ArmyMoved')).toBe(false);
      expect(onHandlers.has('CombatResolved')).toBe(false);
    });
  });

  describe('SlotMachineSpun handler', () => {
    it('calls game store handler and animation store setSlotResult', async () => {
      await connectToGame('game-1');

      const handler = onHandlers.get('SlotMachineSpun')!;
      const data = { outcome: 3, actionPointsAfter: 2, goldAfter: 100, goldSpent: 10 };
      handler(data);

      expect(mockGameStoreState.handleSlotMachineSpun).toHaveBeenCalledWith(data);
      expect(mockAnimationStoreState.setSlotResult).toHaveBeenCalledWith({
        outcome: 3,
        actionPointsAfter: 2,
      });
    });
  });

  describe('BattleResolved handler', () => {
    it('calls game store handler and animation store startCombatPlayback', async () => {
      await connectToGame('game-1');

      const handler = onHandlers.get('BattleResolved')!;
      const mockRounds = [{ roundNumber: 1 }];
      const data = {
        battleId: 'b1',
        attackerKingdomId: 'k1',
        defenderKingdomId: 'k2',
        outcome: 'AttackerWins',
        tileCapturedId: 't1',
        tileCapturedFromKingdomId: 'k2',
        rounds: mockRounds,
      };
      handler(data);

      expect(mockGameStoreState.handleBattleResolved).toHaveBeenCalledWith(data);
      expect(mockAnimationStoreState.startCombatPlayback).toHaveBeenCalledWith(mockRounds);
    });
  });

  describe('GameStateSnapshot handler', () => {
    it('calls loadSnapshot with data and userId from auth store', async () => {
      await connectToGame('game-1');

      const handler = onHandlers.get('GameStateSnapshot')!;
      const snapshot = { gameId: 'game-1', tiles: [], kingdoms: [], armies: [] };
      handler(snapshot);

      expect(mockGameStoreState.loadSnapshot).toHaveBeenCalledWith(snapshot, 'user-1');
      expect(mockGameStoreState.setConnectionStatus).toHaveBeenCalledWith('connected');
    });
  });

  describe('disconnectFromGame', () => {
    it('stops connection and resets state', async () => {
      await connectToGame('game-1');
      disconnectFromGame();

      expect(mockConnection.stop).toHaveBeenCalled();
      expect(mockGameStoreState.resetState).toHaveBeenCalled();
    });
  });
});
