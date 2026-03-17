import { useGameStore } from '../game-store';
import { getKingdomColor } from '../canvas/hex-renderer';

export function TurnIndicator() {
  const turnNumber = useGameStore((s) => s.turnNumber);
  const currentTurnKingdomId = useGameStore((s) => s.currentTurnKingdomId);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);

  const isMyTurn = myKingdomId !== null && currentTurnKingdomId === myKingdomId;
  const currentKingdom = currentTurnKingdomId
    ? kingdoms.get(currentTurnKingdomId)
    : undefined;
  const kingdomColor = currentTurnKingdomId
    ? getKingdomColor(kingdoms, currentTurnKingdomId)
    : '#6b6b6b';

  return (
    <div className="flex items-center gap-2">
      <span
        className="inline-block w-3 h-3 rounded-full"
        style={{ backgroundColor: kingdomColor }}
      />
      {isMyTurn ? (
        <span className="text-gold-400 font-heading font-bold text-sm">
          Turn {turnNumber} (You)
        </span>
      ) : (
        <span className="text-parchment-300 text-sm">
          Turn {turnNumber} ({currentKingdom?.name ?? 'Waiting'})
        </span>
      )}
    </div>
  );
}
