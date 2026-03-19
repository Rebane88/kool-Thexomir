import { create } from 'zustand';
import type { BattleRoundResult } from './types/event-types';

interface AnimationState {
  // Slot machine
  slotSpinning: boolean;
  slotResult: { outcome: number; actionPointsAfter: number } | null;

  // Combat playback
  combatPlaybackRounds: BattleRoundResult[] | null;
  combatPlaybackIndex: number;

  // Actions
  startSlotSpin: () => void;
  setSlotResult: (result: { outcome: number; actionPointsAfter: number }) => void;
  clearSlot: () => void;
  startCombatPlayback: (rounds: BattleRoundResult[]) => void;
  advanceCombatPlayback: () => void;
  clearCombatPlayback: () => void;
  reset: () => void;
}

const initialState = {
  slotSpinning: false,
  slotResult: null as { outcome: number; actionPointsAfter: number } | null,
  combatPlaybackRounds: null as BattleRoundResult[] | null,
  combatPlaybackIndex: 0,
};

export const useAnimationStore = create<AnimationState>((set) => ({
  ...initialState,

  startSlotSpin: () => set({ slotSpinning: true, slotResult: null }),

  setSlotResult: (result) => set({ slotSpinning: false, slotResult: result }),

  clearSlot: () => set({ slotSpinning: false, slotResult: null }),

  startCombatPlayback: (rounds) =>
    set({ combatPlaybackRounds: rounds, combatPlaybackIndex: 0 }),

  advanceCombatPlayback: () =>
    set((state) => ({ combatPlaybackIndex: state.combatPlaybackIndex + 1 })),

  clearCombatPlayback: () =>
    set({ combatPlaybackRounds: null, combatPlaybackIndex: 0 }),

  reset: () => set({ ...initialState }),
}));
