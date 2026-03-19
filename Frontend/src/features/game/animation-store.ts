import { create } from 'zustand';
import type { BattleRoundResult } from './types/event-types';
import type { GamePhase } from './types/enums';

interface AnimationState {
  // Slot machine
  slotSpinning: boolean;
  slotResult: { outcome: number; actionPointsAfter: number } | null;

  // Server-driven combat playback
  currentRound: BattleRoundResult | null;
  currentBattleId: string | null;
  battleRoundHistory: BattleRoundResult[];

  // Phase banner
  phaseBannerPhase: GamePhase | null;
  phaseBannerRound: number | null;

  // Actions
  startSlotSpin: () => void;
  setSlotResult: (result: { outcome: number; actionPointsAfter: number }) => void;
  clearSlot: () => void;
  receiveRound: (round: BattleRoundResult, battleId: string) => void;
  clearCombatPlayback: () => void;
  showPhaseBanner: (phase: GamePhase, round: number) => void;
  hidePhaseBanner: () => void;
  reset: () => void;
}

const initialState = {
  slotSpinning: false,
  slotResult: null as { outcome: number; actionPointsAfter: number } | null,
  currentRound: null as BattleRoundResult | null,
  currentBattleId: null as string | null,
  battleRoundHistory: [] as BattleRoundResult[],
  phaseBannerPhase: null as GamePhase | null,
  phaseBannerRound: null as number | null,
};

export const useAnimationStore = create<AnimationState>((set) => ({
  ...initialState,

  startSlotSpin: () => set({ slotSpinning: true, slotResult: null }),

  setSlotResult: (result) => set({ slotSpinning: false, slotResult: result }),

  clearSlot: () => set({ slotSpinning: false, slotResult: null }),

  receiveRound: (round, battleId) =>
    set((state) => ({
      currentRound: round,
      currentBattleId: battleId,
      battleRoundHistory: [...state.battleRoundHistory, round],
    })),

  clearCombatPlayback: () =>
    set({ currentRound: null, currentBattleId: null, battleRoundHistory: [] }),

  showPhaseBanner: (phase, round) =>
    set({ phaseBannerPhase: phase, phaseBannerRound: round }),

  hidePhaseBanner: () =>
    set({ phaseBannerPhase: null, phaseBannerRound: null }),

  reset: () => set({ ...initialState }),
}));
