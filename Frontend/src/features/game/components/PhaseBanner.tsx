import { useState, useEffect } from 'react';
import { useAnimationStore } from '../animation-store';
import type { GamePhase } from '../types/enums';
import phaseActionPng from '@/assets/images/phase-action.png';
import phaseBattlePng from '@/assets/images/phase-battle.png';
import phaseIncomePng from '@/assets/images/phase-income.png';
import phaseRoundendPng from '@/assets/images/phase-roundend.png';

const PHASE_BANNER_CONFIG: Record<string, { bg: string; text: string; iconSrc: string; label: string }> = {
  Action:   { bg: 'from-yellow-900/80 to-yellow-800/60', text: 'text-yellow-300',  iconSrc: phaseActionPng, label: 'ACTION PHASE' },
  Battle:   { bg: 'from-red-900/80 to-red-800/60',       text: 'text-red-300',     iconSrc: phaseBattlePng, label: 'BATTLE PHASE' },
  Income:   { bg: 'from-green-900/80 to-green-800/60',   text: 'text-green-300',   iconSrc: phaseIncomePng, label: 'INCOME PHASE' },
  RoundEnd: { bg: 'from-stone-900/80 to-stone-800/60',   text: 'text-stone-300',   iconSrc: phaseRoundendPng, label: 'ROUND END' },
};

export function PhaseBanner() {
  const phaseBannerPhase = useAnimationStore((s) => s.phaseBannerPhase);
  const phaseBannerRound = useAnimationStore((s) => s.phaseBannerRound);

  const [visible, setVisible] = useState(false);
  const [displayPhase, setDisplayPhase] = useState<GamePhase | null>(null);
  const [displayRound, setDisplayRound] = useState<number | null>(null);

  useEffect(() => {
    if (phaseBannerPhase) {
      setDisplayPhase(phaseBannerPhase);
      setDisplayRound(phaseBannerRound);
      setVisible(true);
      const timer = setTimeout(() => {
        useAnimationStore.getState().hidePhaseBanner();
      }, 2000);
      return () => clearTimeout(timer);
    } else {
      // Start exit animation
      setVisible(false);
      const timer = setTimeout(() => {
        setDisplayPhase(null);
        setDisplayRound(null);
      }, 400); // match exit animation duration
      return () => clearTimeout(timer);
    }
  }, [phaseBannerPhase, phaseBannerRound]);

  if (!displayPhase) return null;

  const config = PHASE_BANNER_CONFIG[displayPhase];
  if (!config) return null;

  return (
    <div
      className={`fixed top-[15%] left-0 right-0 z-40 pointer-events-none flex items-center justify-center ${
        visible ? 'animate-banner-in' : 'animate-banner-out'
      }`}
    >
      <div
        className={`bg-gradient-to-r ${config.bg} backdrop-blur-sm border-y border-white/10 px-16 py-6 text-center`}
      >
        {displayRound !== null && (
          <div className="text-parchment-400 text-sm tracking-[0.3em] uppercase mb-1">
            Round {displayRound}
          </div>
        )}
        <div className={`font-heading ${config.text} text-3xl tracking-[0.2em] uppercase flex items-center justify-center gap-4`}>
          <img src={config.iconSrc} alt="" className="w-10 h-10 object-contain" />
          <span>{config.label}</span>
          <img src={config.iconSrc} alt="" className="w-10 h-10 object-contain" />
        </div>
      </div>
    </div>
  );
}
