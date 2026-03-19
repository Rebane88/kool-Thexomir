import { describe, it, expect, beforeEach } from 'vitest';
import { useAnimationStore } from './animation-store';
import { useGameStore } from './game-store';
import type { BattleRoundResult } from './types/event-types';

const mockRounds: BattleRoundResult[] = [
  {
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
  },
  {
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
  },
];

describe('useAnimationStore', () => {
  beforeEach(() => {
    useAnimationStore.getState().reset();
  });

  it('has correct initial state', () => {
    const state = useAnimationStore.getState();
    expect(state.slotSpinning).toBe(false);
    expect(state.slotResult).toBeNull();
    expect(state.combatPlaybackRounds).toBeNull();
    expect(state.combatPlaybackIndex).toBe(0);
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

  it('startCombatPlayback sets rounds and resets index to 0', () => {
    useAnimationStore.getState().startCombatPlayback(mockRounds);

    const state = useAnimationStore.getState();
    expect(state.combatPlaybackRounds).toEqual(mockRounds);
    expect(state.combatPlaybackIndex).toBe(0);
  });

  it('advanceCombatPlayback increments index by 1', () => {
    useAnimationStore.getState().startCombatPlayback(mockRounds);
    useAnimationStore.getState().advanceCombatPlayback();

    expect(useAnimationStore.getState().combatPlaybackIndex).toBe(1);

    useAnimationStore.getState().advanceCombatPlayback();
    expect(useAnimationStore.getState().combatPlaybackIndex).toBe(2);
  });

  it('clearCombatPlayback resets combat playback state', () => {
    useAnimationStore.getState().startCombatPlayback(mockRounds);
    useAnimationStore.getState().advanceCombatPlayback();
    useAnimationStore.getState().clearCombatPlayback();

    const state = useAnimationStore.getState();
    expect(state.combatPlaybackRounds).toBeNull();
    expect(state.combatPlaybackIndex).toBe(0);
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
