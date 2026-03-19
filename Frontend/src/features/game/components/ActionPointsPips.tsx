import { useGameStore } from '../game-store';

export function ActionPointsPips() {
  const actionPoints = useGameStore((s) => s.actionPoints);
  const maxActionPoints = useGameStore((s) => s.maxActionPoints);

  if (actionPoints === null || maxActionPoints === null) return null;

  const pips = Array.from({ length: maxActionPoints }, (_, i) => i < actionPoints);

  return (
    <div className="flex items-center gap-1">
      {pips.map((filled, i) => (
        <span
          key={i}
          className={`inline-block w-2.5 h-2.5 rounded-full transition-all duration-300 ${
            filled
              ? 'bg-gold-400 scale-100'
              : 'bg-transparent border border-ash-500 scale-90'
          }`}
        />
      ))}
      <span className="text-parchment-300 text-xs ml-1 tabular-nums">
        {actionPoints}/{maxActionPoints} AP
      </span>
    </div>
  );
}
