import { create } from 'zustand';
import type { BattleRoundResult } from './types/event-types';
import type { GamePhase } from './types/enums';

interface AnimationState {
  // Slot machine
  slotSpinning: boolean;
  slotResult: { outcome: number; actionPointsAfter: number } | null;

  // Combat playback
  combatPlaybackRounds: BattleRoundResult[] | null;
  combatPlaybackIndex: number;

  // Phase banner
  phaseBannerPhase: GamePhase | null;
  phaseBannerRound: number | null;

  // Actions
  startSlotSpin: () => void;
  setSlotResult: (result: { outcome: number; actionPointsAfter: number }) => void;
  clearSlot: () => void;
  startCombatPlayback: (rounds: BattleRoundResult[]) => void;
  advanceCombatPlayback: () => void;
  clearCombatPlayback: () => void;
  showPhaseBanner: (phase: GamePhase, round: number) => void;
  hidePhaseBanner: () => void;
  reset: () => void;
}

const initialState = {
  slotSpinning: false,
  slotResult: null as { outcome: number; actionPointsAfter: number } | null,
  combatPlaybackRounds: null as BattleRoundResult[] | null,
  combatPlaybackIndex: 0,
  phaseBannerPhase: null as GamePhase | null,
  phaseBannerRound: null as number | null,
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

  showPhaseBanner: (phase, round) =>
    set({ phaseBannerPhase: phase, phaseBannerRound: round }),

  hidePhaseBanner: () =>
    set({ phaseBannerPhase: null, phaseBannerRound: null }),

  reset: () => set({ ...initialState }),
}));
