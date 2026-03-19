import { describe, it, expect, beforeEach } from 'vitest';
import { useAnimationStore } from './animation-store';
import { useGameStore } from './game-store';
import type { BattleRoundResult } from './types/event-types';

const mockRound1: BattleRoundResult = {
  roundNumber: 1,
  attackerArmyId: 'a1',
  defenderArmyId: 'd1',
  initiativeWinner: 'attacker',
  attackerInitiativeChance: 0.6,
  defenderInitiativeChance: 0.4,
  damageDealt: 5,
  chipDamageDealt: 1,
  attackerArmyHPAfter: 10,
  defenderArmyHPAfter: 5,
  armyDestroyedId: null,
};

const mockRound2: BattleRoundResult = {
  roundNumber: 2,
  attackerArmyId: 'a1',
  defenderArmyId: 'd1',
  initiativeWinner: 'defender',
  attackerInitiativeChance: 0.6,
  defenderInitiativeChance: 0.4,
  damageDealt: 3,
  chipDamageDealt: 0,
  attackerArmyHPAfter: 7,
  defenderArmyHPAfter: 5,
  armyDestroyedId: null,
};

describe('useAnimationStore', () => {
  beforeEach(() => {
    useAnimationStore.getState().reset();
  });

  it('has correct initial state', () => {
    const state = useAnimationStore.getState();
    expect(state.slotSpinning).toBe(false);
    expect(state.slotResult).toBeNull();
    expect(state.currentRound).toBeNull();
    expect(state.currentBattleId).toBeNull();
    expect(state.battleRoundHistory).toEqual([]);
  });

  it('startSlotSpin sets slotSpinning=true and slotResult=null', () => {
    // Pre-set a result to verify it gets cleared
    useAnimationStore.getState().setSlotResult({ outcome: 3, actionPointsAfter: 2 });
    useAnimationStore.getState().startSlotSpin();

    const state = useAnimationStore.getState();
    expect(state.slotSpinning).toBe(true);
    expect(state.slotResult).toBeNull();
  });

  it('setSlotResult sets slotSpinning=false and stores the result', () => {
    useAnimationStore.getState().startSlotSpin();
    useAnimationStore.getState().setSlotResult({ outcome: 5, actionPointsAfter: 3 });

    const state = useAnimationStore.getState();
    expect(state.slotSpinning).toBe(false);
    expect(state.slotResult).toEqual({ outcome: 5, actionPointsAfter: 3 });
  });

  it('clearSlot resets slot state', () => {
    useAnimationStore.getState().setSlotResult({ outcome: 5, actionPointsAfter: 3 });
    useAnimationStore.getState().clearSlot();

    const state = useAnimationStore.getState();
    expect(state.slotSpinning).toBe(false);
    expect(state.slotResult).toBeNull();
  });

  it('receiveRound sets currentRound, currentBattleId, and appends to battleRoundHistory', () => {
    useAnimationStore.getState().receiveRound(mockRound1, 'battle-1');

    const state = useAnimationStore.getState();
    expect(state.currentRound).toEqual(mockRound1);
    expect(state.currentBattleId).toBe('battle-1');
    expect(state.battleRoundHistory).toHaveLength(1);
    expect(state.battleRoundHistory[0]).toEqual(mockRound1);
  });

  it('receiveRound accumulates history across multiple rounds', () => {
    useAnimationStore.getState().receiveRound(mockRound1, 'battle-1');
    useAnimationStore.getState().receiveRound(mockRound2, 'battle-1');

    const state = useAnimationStore.getState();
    expect(state.currentRound).toEqual(mockRound2);
    expect(state.battleRoundHistory).toHaveLength(2);
  });

  it('clearCombatPlayback resets server-driven combat state', () => {
    useAnimationStore.getState().receiveRound(mockRound1, 'battle-1');
    useAnimationStore.getState().clearCombatPlayback();

    const state = useAnimationStore.getState();
    expect(state.currentRound).toBeNull();
    expect(state.currentBattleId).toBeNull();
    expect(state.battleRoundHistory).toEqual([]);
  });

  it('is a separate store instance from useGameStore', () => {
    // Verify they are different store instances
    expect(useAnimationStore).not.toBe(useGameStore);
    expect(useAnimationStore.getState()).not.toBe(useGameStore.getState());

    // Verify animation store does not have game store properties
    const animState = useAnimationStore.getState();
    expect('tiles' in animState).toBe(false);
    expect('kingdoms' in animState).toBe(false);
  });
});
