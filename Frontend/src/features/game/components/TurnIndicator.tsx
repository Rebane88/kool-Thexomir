import { useGameStore } from '../game-store';
import { getKingdomColor } from '../canvas/hex-renderer';

const PHASE_CONFIG: Record<string, { color: string; label: string }> = {
  Action: { color: 'text-gold-400', label: 'Action' },
  Battle: { color: 'text-red-400', label: 'Battle' },
  Income: { color: 'text-green-400', label: 'Income' },
  RoundEnd: { color: 'text-parchment-400', label: 'Round End' },
};

export function TurnIndicator() {
  const roundNumber = useGameStore((s) => s.roundNumber);
  const currentPhase = useGameStore((s) => s.currentPhase);
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

  const phaseConfig = currentPhase ? PHASE_CONFIG[currentPhase] : null;

  return (
    <div className="flex flex-col items-center gap-0.5">
      {phaseConfig && (
        <span
          className={`${phaseConfig.color} font-heading font-bold text-xs uppercase tracking-wide`}
        >
          {phaseConfig.label}
        </span>
      )}
      <div className="flex items-center gap-2">
        <span
          className="inline-block w-3 h-3 rounded-full"
          style={{ backgroundColor: kingdomColor }}
        />
        {isMyTurn ? (
          <span className="text-gold-400 font-heading font-bold text-sm">
            Round {roundNumber} (You)
          </span>
        ) : (
          <span className="text-parchment-300 text-sm">
            Round {roundNumber} ({currentKingdom?.name || currentKingdom?.factionName || 'Waiting'})
          </span>
        )}
      </div>
    </div>
  );
}
