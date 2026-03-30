import { useState, useEffect, useRef, useCallback } from 'react';
import { useAnimationStore } from '../animation-store';
import { Button } from '@/shared/ui/Button';
import slotframeSrc from '@/assets/images/slotframe.png';

const OUTCOMES = ['+2', '+1', '0', '-1', '-2'];
const SYMBOL_WIDTH = 120;
const VISIBLE_SYMBOLS = 3;

const STRIP_REPEATS = 30;
const STRIP = Array.from({ length: STRIP_REPEATS }, () => OUTCOMES).flat();

const FRAME_WIDTH = 520;
const FRAME_HEIGHT = 360;
const INNER_LEFT = 78;
const INNER_TOP = 105;
const INNER_WIDTH = VISIBLE_SYMBOLS * SYMBOL_WIDTH;
const INNER_HEIGHT = 100;

const SPIN_SPEED = 5; // px per frame (~300px/s)
const DECEL_DURATION = 5500; // 5.5s deceleration
const MIN_TRAVEL_CYCLES = 6;
const AUTO_DISMISS_MS = 5000;

function outcomeIndex(value: number): number {
  return 2 - value;
}

function symbolColor(symbol: string): string {
  if (symbol === '+2' || symbol === '+1') return 'text-green-400';
  if (symbol === '-1' || symbol === '-2') return 'text-red-400';
  return 'text-parchment-300';
}

export function SlotMachineOverlay() {
  const slotSpinning = useAnimationStore((s) => s.slotSpinning);
  const slotResult = useAnimationStore((s) => s.slotResult);
  const isVisible = slotSpinning || slotResult !== null;

  const [offset, setOffset] = useState(0);
  const [done, setDone] = useState(false);

  // All mutable animation state lives in refs to avoid effect conflicts
  const rafRef = useRef(0);
  const offsetRef = useRef(0);
  const modeRef = useRef<'idle' | 'spinning' | 'decelerating' | 'done'>('idle');
  const decelRef = useRef<{
    startOffset: number;
    totalDistance: number;
    startTime: number;
    targetOffset: number;
  } | null>(null);

  const dismiss = useCallback(() => {
    cancelAnimationFrame(rafRef.current);
    modeRef.current = 'idle';
    decelRef.current = null;
    offsetRef.current = 0;
    setOffset(0);
    setDone(false);
    useAnimationStore.getState().clearSlot();
  }, []);

  // Single animation loop — reads mode from ref, no effect dep conflicts
  useEffect(() => {
    if (!isVisible) return;

    const tick = () => {
      const mode = modeRef.current;

      if (mode === 'spinning') {
        offsetRef.current += SPIN_SPEED;
        const cycleWidth = OUTCOMES.length * SYMBOL_WIDTH;
        if (offsetRef.current > cycleWidth * (STRIP_REPEATS - VISIBLE_SYMBOLS)) {
          offsetRef.current -= cycleWidth;
        }
        setOffset(offsetRef.current);
      } else if (mode === 'decelerating' && decelRef.current) {
        const d = decelRef.current;
        const t = Math.min((performance.now() - d.startTime) / DECEL_DURATION, 1);
        const eased = 1 - Math.pow(1 - t, 5);
        offsetRef.current = d.startOffset + d.totalDistance * eased;
        setOffset(offsetRef.current);

        if (t >= 1) {
          offsetRef.current = d.targetOffset;
          setOffset(d.targetOffset);
          modeRef.current = 'done';
          decelRef.current = null;
          setDone(true);
          return; // stop the loop
        }
      } else if (mode === 'done' || mode === 'idle') {
        return; // stop the loop
      }

      rafRef.current = requestAnimationFrame(tick);
    };

    rafRef.current = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(rafRef.current);
  }, [isVisible]);

  // Kick off spinning when slotSpinning goes true
  useEffect(() => {
    if (slotSpinning && modeRef.current === 'idle') {
      modeRef.current = 'spinning';
      offsetRef.current = 0;
      setOffset(0);
      setDone(false);
    }
  }, [slotSpinning]);

  // Transition to deceleration when result arrives
  useEffect(() => {
    if (!slotResult) return;
    if (modeRef.current !== 'spinning') return;

    const targetIdx = outcomeIndex(slotResult.outcome);
    const cycleWidth = OUTCOMES.length * SYMBOL_WIDTH;
    const minDistance = cycleWidth * MIN_TRAVEL_CYCLES;
    const currentOffset = offsetRef.current;

    const viewportCenterOffset = ((VISIBLE_SYMBOLS - 1) / 2) * SYMBOL_WIDTH;
    let targetOffset = targetIdx * SYMBOL_WIDTH - viewportCenterOffset;
    while (targetOffset < currentOffset + minDistance) {
      targetOffset += cycleWidth;
    }

    decelRef.current = {
      startOffset: currentOffset,
      totalDistance: targetOffset - currentOffset,
      startTime: performance.now(),
      targetOffset,
    };
    modeRef.current = 'decelerating';
  }, [slotResult]);

  // Auto-dismiss after done
  useEffect(() => {
    if (!done) return;
    const timer = setTimeout(dismiss, AUTO_DISMISS_MS);
    return () => clearTimeout(timer);
  }, [done, dismiss]);

  // Reset if cleared externally
  useEffect(() => {
    if (!isVisible && modeRef.current !== 'idle') {
      cancelAnimationFrame(rafRef.current);
      modeRef.current = 'idle';
      decelRef.current = null;
      offsetRef.current = 0;
      setOffset(0);
      setDone(false);
    }
  }, [isVisible]);

  if (!isVisible) return null;

  const outcome = slotResult?.outcome ?? null;
  const showResult = done && slotResult;

  const outcomeColorClass =
    outcome !== null && outcome > 0
      ? 'text-green-400'
      : outcome !== null && outcome < 0
        ? 'text-red-400'
        : 'text-parchment-300';

  const glowClass =
    outcome !== null && outcome > 0
      ? 'animate-pulse drop-shadow-[0_0_12px_rgba(74,222,128,0.7)]'
      : outcome !== null && outcome < 0
        ? 'animate-pulse drop-shadow-[0_0_12px_rgba(248,113,113,0.7)]'
        : '';

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center"
      onClick={() => done && dismiss()}
    >
      <div className="relative flex flex-col items-center" onClick={(e) => e.stopPropagation()}>
        {/* Stone-panel bg behind the slot */}
        <div
          className="absolute rounded-xl"
          style={{
            left: -28,
            top: -28,
            width: FRAME_WIDTH + 56,
            height: FRAME_HEIGHT + (showResult ? 180 : 56),
            background: 'linear-gradient(135deg, #1a1a24 0%, #111118 50%, #1a1a24 100%)',
            border: '1px solid #5a4a35',
            boxShadow:
              'inset 0 1px 0 rgba(201, 168, 76, 0.1), ' +
              '0 0 0 1px #3d3225, ' +
              '0 8px 32px rgba(0, 0, 0, 0.8), ' +
              '0 0 60px rgba(0, 0, 0, 0.6)',
          }}
        />

        {/* Frame container */}
        <div className="relative" style={{ width: FRAME_WIDTH, height: FRAME_HEIGHT }}>
          {/* Reel viewport — behind the frame */}
          <div
            className="absolute overflow-hidden z-0"
            style={{
              left: INNER_LEFT,
              top: INNER_TOP,
              width: INNER_WIDTH,
              height: INNER_HEIGHT,
            }}
          >
            {/* Edge fade gradients */}
            <div className="absolute inset-y-0 left-0 w-10 z-10 bg-gradient-to-r from-ash-900/90 to-transparent pointer-events-none" />
            <div className="absolute inset-y-0 right-0 w-10 z-10 bg-gradient-to-l from-ash-900/90 to-transparent pointer-events-none" />

            {/* Center highlight bar */}
            <div
              className="absolute inset-y-0 z-[5] border-x-2 border-gold-400/40 bg-gold-400/5 pointer-events-none"
              style={{
                left: (INNER_WIDTH - SYMBOL_WIDTH) / 2,
                width: SYMBOL_WIDTH,
              }}
            />

            {/* Scrolling strip */}
            <div
              className="flex items-center h-full"
              style={{
                transform: `translateX(-${offset}px)`,
                width: STRIP.length * SYMBOL_WIDTH,
              }}
            >
              {STRIP.map((symbol, i) => (
                <div
                  key={i}
                  className={`flex-shrink-0 flex items-center justify-center font-heading text-5xl font-bold ${symbolColor(symbol)}`}
                  style={{ width: SYMBOL_WIDTH, height: INNER_HEIGHT }}
                >
                  {symbol}
                </div>
              ))}
            </div>
          </div>

          {/* Frame image — on top so edges are covered */}
          <img
            src={slotframeSrc}
            alt=""
            className="absolute inset-0 w-full h-full pointer-events-none select-none z-10"
            style={{ objectFit: 'fill' }}
          />

          {/* Pointer triangle below the reel */}
          <div
            className="absolute z-20"
            style={{ left: INNER_LEFT + INNER_WIDTH / 2 - 10, top: INNER_TOP + INNER_HEIGHT + 2 }}
          >
            <div className="w-0 h-0 border-l-[10px] border-l-transparent border-r-[10px] border-r-transparent border-b-[12px] border-b-gold-400" />
          </div>
        </div>

        {/* Result display below the frame */}
        {showResult && (
          <div className="relative mt-4 flex flex-col items-center gap-3">
            <span className={`font-heading text-4xl font-bold ${outcomeColorClass} ${glowClass}`}>
              AP {outcome! > 0 ? '+' : ''}{outcome}
            </span>
            <span className="font-body text-sm text-parchment-400">
              New AP: {slotResult.actionPointsAfter}
            </span>
            <Button variant="primary" size="sm" onClick={dismiss}>
              OK
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
