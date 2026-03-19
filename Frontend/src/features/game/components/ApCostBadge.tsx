import { useGameStore } from '../game-store';

interface ApCostBadgeProps {
  cost: number;
}

export function ApCostBadge({ cost }: ApCostBadgeProps) {
  const actionPoints = useGameStore((s) => s.actionPoints);
  const canAfford = actionPoints !== null && actionPoints >= cost;

  return (
    <span
      className={`inline-flex items-center justify-center min-w-[2rem] px-1 py-0.5 rounded-full text-[10px] font-bold leading-none ${
        canAfford
          ? 'bg-gold-400/20 text-gold-300 border border-gold-400/40'
          : 'bg-red-900/30 text-red-400/70 border border-red-400/30'
      }`}
    >
      {cost} AP
    </span>
  );
}

export function useCanAffordAp(cost: number): boolean {
  const actionPoints = useGameStore((s) => s.actionPoints);
  return actionPoints !== null && actionPoints >= cost;
}
