import { Modal } from '@/shared/ui/Modal';
import { Button } from '@/shared/ui/Button';
import type { CombatResolvedEvent } from '../types/event-types';

interface CombatResultModalProps {
  open: boolean;
  onClose: () => void;
  result: CombatResolvedEvent;
  myKingdomId: string;
  attackerKingdomName: string;
  defenderKingdomName: string;
}

export function CombatResultModal({
  open,
  onClose,
  result,
  myKingdomId,
  attackerKingdomName,
  defenderKingdomName,
}: CombatResultModalProps) {
  const isVictory = result.winnerKingdomId === myKingdomId;
  const isDraw = result.winnerKingdomId === null;

  return (
    <Modal open={open} onClose={onClose}>
      <h2
        className={`font-heading text-xl font-bold ${
          isDraw
            ? 'text-parchment-300'
            : isVictory
              ? 'text-gold-400'
              : 'text-ember-400'
        }`}
      >
        {isDraw ? 'Draw' : isVictory ? 'Victory!' : 'Defeat!'}
      </h2>

      <p
        className={`text-sm ${
          result.tileCaptured ? 'text-green-400' : 'text-parchment-400'
        }`}
      >
        {result.tileCaptured ? 'Tile captured!' : 'Tile defended'}
      </p>

      <div className="grid grid-cols-2 gap-4 mt-4">
        <div>
          <h3 className="text-gold-400 text-sm font-semibold">
            {attackerKingdomName}
          </h3>
          <CasualtyTable casualties={result.attackerCasualties} />
        </div>
        <div>
          <h3 className="text-ember-400 text-sm font-semibold">
            {defenderKingdomName}
          </h3>
          <CasualtyTable casualties={result.defenderCasualties} />
        </div>
      </div>

      <div className="border-t border-bronze-700 mt-4 pt-3 flex justify-end">
        <Button variant="secondary" onClick={onClose}>
          Close
        </Button>
      </div>
    </Modal>
  );
}

function CasualtyTable({
  casualties,
}: {
  casualties: { unitTypeName: string; before: number; lost: number }[];
}) {
  return (
    <div className="mt-2 space-y-1">
      {casualties.map((c) => (
        <div
          key={c.unitTypeName}
          className={`flex justify-between text-sm ${
            c.lost > 0 ? 'text-ember-400' : 'text-parchment-400'
          }`}
        >
          <span className="text-parchment-200">{c.unitTypeName}</span>
          <span>
            {c.before} &rarr; {c.before - c.lost}
            {c.lost > 0 && (
              <span className="text-ember-400"> (-{c.lost})</span>
            )}
          </span>
        </div>
      ))}
    </div>
  );
}
