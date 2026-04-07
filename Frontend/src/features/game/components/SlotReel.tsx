import { useState, useEffect, useRef } from 'react';

const SYMBOLS = ['+2', '+1', '0', '-1', '-2'];
const REEL_STRIP = [...SYMBOLS, ...SYMBOLS, ...SYMBOLS, ...SYMBOLS];
const SYMBOL_HEIGHT = 60;
const CYCLE_HEIGHT = SYMBOLS.length * SYMBOL_HEIGHT; // 300px

interface SlotReelProps {
  spinning: boolean;
  targetSymbol: number | null;
  stopDelay: number;
  nearMiss: boolean;
  onStopped?: () => void;
}

type ReelPhase = 'idle' | 'spinning' | 'stopping' | 'nearMiss' | 'snapping' | 'done';

function symbolColor(symbol: string): string {
  if (symbol === '+2' || symbol === '+1') return 'text-green-400';
  if (symbol === '-1' || symbol === '-2') return 'text-red-400';
  return 'text-parchment-300';
}

function symbolIndex(value: number): number {
  // +2=0, +1=1, 0=2, -1=3, -2=4
  return 2 - value;
}

export function SlotReel({ spinning, targetSymbol, stopDelay, nearMiss, onStopped }: SlotReelProps) {
  const [phase, setPhase] = useState<ReelPhase>('idle');
  const [offset, setOffset] = useState(0);
  const [transition, setTransition] = useState('');
  const onStoppedRef = useRef(onStopped);
  onStoppedRef.current = onStopped;
  const phaseRef = useRef(phase);
  phaseRef.current = phase;

  // Start spinning when spinning prop becomes true
  useEffect(() => {
    if (spinning && phase === 'idle') {
      setPhase('spinning');
      setOffset(0);
      setTransition('');
    }
  }, [spinning, phase]);

  // Stop sequence: when targetSymbol arrives and we're spinning
  useEffect(() => {
    if (targetSymbol === null || phase !== 'spinning') return;

    const timer = setTimeout(() => {
      if (phaseRef.current !== 'spinning') return;

      if (nearMiss) {
        // Near-miss: first go to +2 position
        const plusTwoOffset = (SYMBOLS.length + symbolIndex(2)) * SYMBOL_HEIGHT;
        setTransition('transform 0.6s cubic-bezier(0.25, 0.46, 0.45, 0.94)');
        setOffset(plusTwoOffset);
        setPhase('nearMiss');
      } else {
        // Normal stop: go directly to target
        const targetOffset = (SYMBOLS.length + symbolIndex(targetSymbol)) * SYMBOL_HEIGHT;
        setTransition('transform 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94)');
        setOffset(targetOffset);
        setPhase('stopping');
      }
    }, stopDelay);

    return () => clearTimeout(timer);
  }, [targetSymbol, phase, stopDelay, nearMiss]);

  // Near-miss: hold at +2, then snap to actual target
  useEffect(() => {
    if (phase !== 'nearMiss' || targetSymbol === null) return;

    const holdTimer = setTimeout(() => {
      const targetOffset = (SYMBOLS.length + symbolIndex(targetSymbol)) * SYMBOL_HEIGHT;
      setTransition('transform 0.3s cubic-bezier(0.42, 0, 0.58, 1)');
      setOffset(targetOffset);
      setPhase('snapping');
    }, 800); // 600ms transition + 200ms hold

    return () => clearTimeout(holdTimer);
  }, [phase, targetSymbol]);

  // Done detection for stopping and snapping phases
  useEffect(() => {
    if (phase !== 'stopping' && phase !== 'snapping') return;

    const duration = phase === 'stopping' ? 800 : 300;
    const timer = setTimeout(() => {
      setPhase('done');
      setTransition('');
      onStoppedRef.current?.();
    }, duration);

    return () => clearTimeout(timer);
  }, [phase]);

  // Reset when spinning stops entirely (overlay dismissed)
  useEffect(() => {
    if (!spinning && phase === 'done') {
      setPhase('idle');
      setOffset(0);
      setTransition('');
    }
  }, [spinning, phase]);

  const isAnimating = phase === 'spinning';

  const stripStyle: React.CSSProperties = isAnimating
    ? { animation: 'spin-reel 150ms linear infinite' }
    : {
        transform: `translateY(-${offset}px)`,
        transition: transition || undefined,
      };

  return (
    <div className="w-[60px] h-[60px] overflow-hidden relative">
      <style>{`
        @keyframes spin-reel {
          from { transform: translateY(0); }
          to { transform: translateY(-${CYCLE_HEIGHT}px); }
        }
      `}</style>
      <div className="absolute w-full" style={stripStyle}>
        {REEL_STRIP.map((symbol, i) => (
          <div
            key={i}
            className={`flex items-center justify-center h-[60px] font-heading text-2xl font-bold ${symbolColor(symbol)}`}
          >
            {symbol}
          </div>
        ))}
      </div>
    </div>
  );
}
