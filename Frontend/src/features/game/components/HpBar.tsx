import { useState, useEffect } from 'react';

interface HpBarProps {
  currentHP: number;
  maxHP: number;
  className?: string;
}

function getBarColor(pct: number): { fg: string; ghost: string } {
  if (pct > 60) return { fg: 'bg-green-500', ghost: 'bg-green-300' };
  if (pct > 30) return { fg: 'bg-yellow-500', ghost: 'bg-yellow-300' };
  return { fg: 'bg-red-500', ghost: 'bg-red-300' };
}

export function HpBar({ currentHP, maxHP, className }: HpBarProps) {
  const pct = maxHP > 0 ? Math.max(0, Math.min(100, (currentHP / maxHP) * 100)) : 0;

  const [displayPct, setDisplayPct] = useState(pct);
  const [ghostPct, setGhostPct] = useState(pct);

  useEffect(() => {
    if (pct < displayPct) {
      // Damage: foreground drops instantly, ghost trails after 300ms
      setDisplayPct(pct);
      const timer = setTimeout(() => setGhostPct(pct), 300);
      return () => clearTimeout(timer);
    } else {
      // Healing or no change: both update immediately
      setDisplayPct(pct);
      setGhostPct(pct);
    }
  }, [pct]); // eslint-disable-line react-hooks/exhaustive-deps

  const { fg, ghost } = getBarColor(displayPct);

  return (
    <div className={`relative h-3 bg-ash-800 rounded overflow-hidden ${className ?? ''}`}>
      {/* Ghost trail — transitions smoothly */}
      <div
        className={`absolute inset-y-0 left-0 transition-all duration-700 ${ghost}`}
        style={{ width: `${ghostPct}%` }}
      />
      {/* Foreground — instant drop */}
      <div
        className={`absolute inset-y-0 left-0 ${fg}`}
        style={{ width: `${displayPct}%` }}
      />
      {/* HP text */}
      <div className="absolute inset-0 flex items-center justify-center text-[10px] text-white font-bold">
        {currentHP}/{maxHP}
      </div>
    </div>
  );
}
