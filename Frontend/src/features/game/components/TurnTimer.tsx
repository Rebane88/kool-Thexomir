import { useState, useEffect } from 'react';
import { useGameStore } from '../game-store';

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, '0')}`;
}

export function TurnTimer() {
  const turnDeadline = useGameStore((s) => s.turnDeadline);
  const [remaining, setRemaining] = useState<number>(0);

  useEffect(() => {
    if (!turnDeadline) {
      setRemaining(0);
      return;
    }

    const calculate = () => {
      const diff = Math.max(0, Math.ceil((new Date(turnDeadline).getTime() - Date.now()) / 1000));
      setRemaining(diff);
    };

    calculate();
    const interval = setInterval(calculate, 1000);
    return () => clearInterval(interval);
  }, [turnDeadline]);

  if (!turnDeadline) return null;

  const isWarning = remaining <= 30 && remaining > 10;
  const isCritical = remaining <= 10 && remaining > 0;
  const isExpired = remaining === 0;

  let className = 'font-mono text-xs tabular-nums';
  if (isCritical) {
    className += ' text-red-500 font-bold animate-pulse';
  } else if (isWarning) {
    className += ' text-red-400 animate-pulse';
  } else if (isExpired) {
    className += ' text-red-500 font-bold';
  } else {
    className += ' text-parchment-300';
  }

  return (
    <span className={className}>
      {isExpired ? "Time's up!" : formatTime(remaining)}
    </span>
  );
}
