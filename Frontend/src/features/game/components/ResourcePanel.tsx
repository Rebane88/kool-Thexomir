import { useGameStore } from '../game-store';
import { useCountingTicker } from '../hooks/useCountingTicker';
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

function ResourceValue({ amount }: { amount: number }) {
  const display = useCountingTicker(amount, 1000);
  const direction = amount > display ? 'up' : amount < display ? 'down' : null;
  return (
    <span className={`text-sm tabular-nums transition-colors duration-300 ${
      direction === 'up' ? 'text-green-400' : direction === 'down' ? 'text-red-400' : 'text-parchment-200'
    }`}>
      {display}
    </span>
  );
}

export function ResourcePanel() {
  const myKingdom = useGameStore((s) =>
    s.myKingdomId ? s.kingdoms.get(s.myKingdomId) : undefined,
  );

  if (!myKingdom) return null;

  return (
    <div className="flex items-center gap-4">
      {RESOURCE_ORDER.map((type) => {
        const Icon = RESOURCE_ICONS[type];
        const count = myKingdom.resources[type] ?? 0;

        return (
          <div key={type} className="flex items-center gap-1">
            <Icon size={16} className="text-gold-400" />
            <ResourceValue amount={count} />
          </div>
        );
      })}
    </div>
  );
}
