import { useState, useEffect, useRef } from 'react';
import battleslotframeSrc from '@/assets/images/battleslotframe.png';

const SYMBOL_HEIGHT = 48;

interface CombatSlotReelProps {
  spinning: boolean;
  symbols: string[];
  targetIndex: number | null;
  label: string;
  onStopped?: () => void;
}

type ReelPhase = 'idle' | 'spinning' | 'stopping' | 'done';

export function CombatSlotReel({ spinning, symbols, targetIndex, label, onStopped }: CombatSlotReelProps) {
  const [phase, setPhase] = useState<ReelPhase>('idle');
  const [offset, setOffset] = useState(0);
  const [transition, setTransition] = useState('');
  const onStoppedRef = useRef(onStopped);
  onStoppedRef.current = onStopped;
  const phaseRef = useRef(phase);
  phaseRef.current = phase;

  const CYCLE_HEIGHT = symbols.length * SYMBOL_HEIGHT;
  const reelStrip = [...symbols, ...symbols, ...symbols];

  // Start spinning when spinning prop becomes true
  useEffect(() => {
    if (spinning && phase === 'idle') {
      setPhase('spinning');
      setOffset(0);
      setTransition('');
    }
  }, [spinning, phase]);

  // Stop when targetIndex arrives and we're spinning
  useEffect(() => {
    if (targetIndex === null || phase !== 'spinning') return;

    const targetOffset = (symbols.length + targetIndex) * SYMBOL_HEIGHT;
    setTransition('transform 0.5s cubic-bezier(0.25, 0.46, 0.45, 0.94)');
    setOffset(targetOffset);
    setPhase('stopping');
  }, [targetIndex, phase, symbols.length]);

  // Done detection for stopping phase
  useEffect(() => {
    if (phase !== 'stopping') return;

    const timer = setTimeout(() => {
      setPhase('done');
      setTransition('');
      onStoppedRef.current?.();
    }, 500);

    return () => clearTimeout(timer);
  }, [phase]);

  // Reset when spinning goes false and phase is done
  useEffect(() => {
    if (!spinning && phase === 'done') {
      setPhase('idle');
      setOffset(0);
      setTransition('');
    }
  }, [spinning, phase]);

  const isAnimating = phase === 'spinning';

  const stripStyle: React.CSSProperties = isAnimating
    ? { animation: `combat-spin-reel-${CYCLE_HEIGHT} 120ms linear infinite` }
    : {
        transform: `translateY(-${offset}px)`,
        transition: transition || undefined,
      };

  return (
    <div className="flex flex-col items-center gap-1">
      <style>{`
        @keyframes combat-spin-reel {
          from { transform: translateY(0); }
          to { transform: translateY(-${CYCLE_HEIGHT}px); }
        }
      `}</style>
      <span className="text-[10px] text-parchment-500 font-heading uppercase">{label}</span>
      <div className="relative">
        <img src={battleslotframeSrc} alt="" className="w-[100px] pointer-events-none select-none" />
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="w-[80px] h-[48px] overflow-hidden relative">
            <div
              className="absolute w-full"
              style={{
                ...stripStyle,
                animationName: isAnimating ? 'combat-spin-reel' : undefined,
              }}
            >
              {reelStrip.map((symbol, i) => (
                <div
                  key={i}
                  className="flex items-center justify-center h-[48px] font-heading text-lg font-bold text-parchment-200"
                >
                  {symbol}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
