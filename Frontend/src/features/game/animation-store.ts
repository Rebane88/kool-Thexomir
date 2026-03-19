import { create } from 'zustand';
import type { BattleRoundResult } from './types/event-types';
import type { GamePhase } from './types/enums';

type CombatSpinPhase = 'idle' | 'initiative' | 'damage' | 'chipDamage' | 'hpUpdate' | 'destruction' | 'done';

interface AnimationState {
  // Slot machine
  slotSpinning: boolean;
  slotResult: { outcome: number; actionPointsAfter: number } | null;

  // Server-driven combat playback
  currentRound: BattleRoundResult | null;
  currentBattleId: string | null;
  battleRoundHistory: BattleRoundResult[];

  // Combat round playback phases
  combatSpinPhase: CombatSpinPhase;
  combatSpinResults: {
    initiativeWinner: string | null;
    damageDealt: number | null;
    chipDamageDealt: number | null;
  };

  // Phase banner
  phaseBannerPhase: GamePhase | null;
  phaseBannerRound: number | null;

  // Actions
  startSlotSpin: () => void;
  setSlotResult: (result: { outcome: number; actionPointsAfter: number }) => void;
  clearSlot: () => void;
  receiveRound: (round: BattleRoundResult, battleId: string) => void;
  clearCombatPlayback: () => void;
  setCombatSpinPhase: (phase: CombatSpinPhase) => void;
  setCombatSpinResult: (key: 'initiativeWinner' | 'damageDealt' | 'chipDamageDealt', value: string | number | null) => void;
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
  combatSpinPhase: 'idle' as CombatSpinPhase,
  combatSpinResults: {
    initiativeWinner: null as string | null,
    damageDealt: null as number | null,
    chipDamageDealt: null as number | null,
  },
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
      combatSpinPhase: 'initiative' as CombatSpinPhase,
      combatSpinResults: { initiativeWinner: null, damageDealt: null, chipDamageDealt: null },
    })),

  clearCombatPlayback: () =>
    set({
      currentRound: null,
      currentBattleId: null,
      battleRoundHistory: [],
      combatSpinPhase: 'idle' as CombatSpinPhase,
      combatSpinResults: { initiativeWinner: null, damageDealt: null, chipDamageDealt: null },
    }),

  setCombatSpinPhase: (phase) => set({ combatSpinPhase: phase }),

  setCombatSpinResult: (key, value) =>
    set((state) => ({
      combatSpinResults: { ...state.combatSpinResults, [key]: value },
    })),

  showPhaseBanner: (phase, round) =>
    set({ phaseBannerPhase: phase, phaseBannerRound: round }),

  hidePhaseBanner: () =>
    set({ phaseBannerPhase: null, phaseBannerRound: null }),

  reset: () => set({ ...initialState }),
}));
