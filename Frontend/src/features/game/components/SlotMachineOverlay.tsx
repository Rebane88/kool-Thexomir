import { useState, useCallback, useEffect } from 'react';
import { useAnimationStore } from '../animation-store';
import { SlotReel } from './SlotReel';
import { Button } from '@/shared/ui/Button';
import slotframeSrc from '@/assets/images/slotframe.png';

export function SlotMachineOverlay() {
  const slotSpinning = useAnimationStore((s) => s.slotSpinning);
  const slotResult = useAnimationStore((s) => s.slotResult);

  const [reelsStoppedCount, setReelsStoppedCount] = useState(0);
  const allReelsStopped = reelsStoppedCount >= 3;

  const isVisible = slotSpinning || slotResult !== null;

  // Reset stopped count when a new spin starts
  useEffect(() => {
    if (slotSpinning) {
      setReelsStoppedCount(0);
    }
  }, [slotSpinning]);

  const handleReelStopped = useCallback(() => {
    setReelsStoppedCount((c) => c + 1);
  }, []);

  const handleDismiss = useCallback(() => {
    if (!allReelsStopped) return;
    useAnimationStore.getState().clearSlot();
    setReelsStoppedCount(0);
  }, [allReelsStopped]);

  if (!isVisible) return null;

  const outcome = slotResult?.outcome ?? null;
  const isSpinning = slotSpinning && !slotResult;
  const isNegative = outcome !== null && outcome < 0;

  const outcomeColorClass =
    outcome !== null && outcome > 0
      ? 'text-green-400'
      : outcome !== null && outcome < 0
        ? 'text-red-400'
        : 'text-parchment-300';

  const glowClass =
    outcome !== null && outcome > 0
      ? 'animate-pulse drop-shadow-[0_0_8px_rgba(74,222,128,0.6)]'
      : outcome !== null && outcome < 0
        ? 'animate-pulse drop-shadow-[0_0_8px_rgba(248,113,113,0.6)]'
        : '';

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60"
      onClick={handleDismiss}
    >
      <div className="relative flex flex-col items-center" onClick={(e) => e.stopPropagation()}>
        {/* Slot frame border */}
        <div className="relative">
          <img
            src={slotframeSrc}
            alt=""
            className="w-[320px] pointer-events-none select-none"
          />
          {/* Reels positioned inside the frame */}
          <div className="absolute inset-0 flex items-center justify-center gap-3">
            <SlotReel
              spinning={isSpinning}
              targetSymbol={outcome}
              stopDelay={0}
              nearMiss={false}
              onStopped={handleReelStopped}
            />
            <SlotReel
              spinning={isSpinning}
              targetSymbol={outcome}
              stopDelay={400}
              nearMiss={false}
              onStopped={handleReelStopped}
            />
            <SlotReel
              spinning={isSpinning}
              targetSymbol={outcome}
              stopDelay={800}
              nearMiss={isNegative}
              onStopped={handleReelStopped}
            />
          </div>
        </div>

        {/* Result display - only after all reels stop */}
        {allReelsStopped && slotResult && (
          <div className="mt-4 flex flex-col items-center gap-2">
            <span className={`font-heading text-3xl font-bold ${outcomeColorClass} ${glowClass}`}>
              AP {outcome! > 0 ? '+' : ''}{outcome}
            </span>
            <span className="font-body text-sm text-parchment-400">
              New AP: {slotResult.actionPointsAfter}
            </span>
            <Button variant="primary" size="sm" onClick={handleDismiss}>
              OK
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
