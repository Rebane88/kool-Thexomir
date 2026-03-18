import { useGameStore } from '../game-store';

export function WinConditionLabel() {
  const winCondition = useGameStore((s) => s.winCondition);

  if (!winCondition) return null;

  return (
    <span className="text-parchment-400 text-xs">{winCondition}</span>
  );
}
