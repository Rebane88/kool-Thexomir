import { useGameStore } from '../game-store';
import { useCountingTicker } from '../hooks/useCountingTicker';
import resourceGoldPng from '@/assets/images/resource-gold.png';
import resourceFoodPng from '@/assets/images/resource-food.png';
import resourceWoodPng from '@/assets/images/resource-wood.png';
import resourceStonePng from '@/assets/images/resource-stone.png';
import resourceManaPng from '@/assets/images/resource-mana.png';

const RESOURCE_ORDER = ['Gold', 'Food', 'Wood', 'Stone', 'Mana'] as const;

const RESOURCE_PNGS: Record<string, string> = {
  Gold: resourceGoldPng,
  Food: resourceFoodPng,
  Wood: resourceWoodPng,
  Stone: resourceStonePng,
  Mana: resourceManaPng,
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
        const count = myKingdom.resources[type] ?? 0;

        return (
          <div key={type} className="flex items-center gap-1">
            <img src={RESOURCE_PNGS[type]} alt={type} className="w-6 h-6 object-contain" />
            <ResourceValue amount={count} />
          </div>
        );
      })}
    </div>
  );
}
