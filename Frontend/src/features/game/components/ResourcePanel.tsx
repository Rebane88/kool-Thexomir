import { useState, useEffect } from 'react';
import { useGameStore } from '../game-store';
import { GoldIcon, FoodIcon, WoodIcon, StoneIcon, ManaIcon } from '@/assets/icons';
import type { ComponentType, SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

const RESOURCE_ORDER = ['Gold', 'Food', 'Wood', 'Stone', 'Mana'] as const;

const RESOURCE_ICONS: Record<string, ComponentType<IconProps>> = {
  Gold: GoldIcon,
  Food: FoodIcon,
  Wood: WoodIcon,
  Stone: StoneIcon,
  Mana: ManaIcon,
};

export function ResourcePanel() {
  const myKingdom = useGameStore((s) =>
    s.myKingdomId ? s.kingdoms.get(s.myKingdomId) : undefined,
  );
  const lastIncomeApplied = useGameStore((s) => s.lastIncomeApplied);

  const [visibleDeltas, setVisibleDeltas] = useState<Record<string, number> | null>(null);
  const [showDeltas, setShowDeltas] = useState(true);

  useEffect(() => {
    if (lastIncomeApplied === null) {
      setVisibleDeltas(null);
      setShowDeltas(true);
      return;
    }

    setVisibleDeltas(lastIncomeApplied);
    setShowDeltas(true);

    const fadeTimer = setTimeout(() => setShowDeltas(false), 2500);
    const clearTimer = setTimeout(() => setVisibleDeltas(null), 3000);

    return () => {
      clearTimeout(fadeTimer);
      clearTimeout(clearTimer);
    };
  }, [lastIncomeApplied]);

  if (!myKingdom) return null;

  return (
    <div className="flex items-center gap-4">
      {RESOURCE_ORDER.map((type) => {
        const Icon = RESOURCE_ICONS[type];
        const count = myKingdom.resources[type] ?? 0;
        const delta = visibleDeltas?.[type];
        const hasDelta = delta !== undefined && delta !== 0;

        return (
          <div key={type} className="flex items-center gap-1">
            <Icon size={16} className="text-gold-400" />
            <span className="text-parchment-200 text-sm tabular-nums">{count}</span>
            <span
              className={`text-green-400 text-xs ml-0.5 transition-opacity duration-500 w-6 ${hasDelta && showDeltas ? 'opacity-100' : 'opacity-0'}`}
            >
              {hasDelta ? `+${delta}` : ''}
            </span>
          </div>
        );
      })}
    </div>
  );
}
