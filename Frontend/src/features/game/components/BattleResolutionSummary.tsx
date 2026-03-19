import { useGameStore } from '../game-store';
import type { BattleResolvedEvent } from '../types/event-types';
import { Button } from '@/shared/ui/Button';

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

interface BattleResolutionSummaryProps {
  result: BattleResolvedEvent;
  onContinue: () => void;
}

export function BattleResolutionSummary({ result, onContinue }: BattleResolutionSummaryProps) {
  const kingdoms = useGameStore((s) => s.kingdoms);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const tiles = useGameStore((s) => s.tiles);
  const tileIdToCoord = useGameStore((s) => s.tileIdToCoord);
  const declaredAttacks = useGameStore((s) => s.declaredAttacks);

  const attackerKingdom = kingdoms.get(result.attackerKingdomId);
  const defenderKingdom = kingdoms.get(result.defenderKingdomId);

  const attackerWon = result.outcome === 'AttackerWon';
  const winnerKingdomId = attackerWon ? result.attackerKingdomId : result.defenderKingdomId;
  const loserKingdomId = attackerWon ? result.defenderKingdomId : result.attackerKingdomId;

  const winner = kingdoms.get(winnerKingdomId)?.name ?? (attackerWon ? 'Attacker' : 'Defender');
  const loser = kingdoms.get(loserKingdomId)?.name ?? (attackerWon ? 'Defender' : 'Attacker');

  const isMyVictory = myKingdomId !== null && winnerKingdomId === myKingdomId;
  const isSpectator =
    myKingdomId === null ||
    (myKingdomId !== result.attackerKingdomId && myKingdomId !== result.defenderKingdomId);

  // Captured tile info
  const hasCapturedTile =
    result.tileCapturedId &&
    result.tileCapturedId !== EMPTY_GUID;

  const capturedTileCoord = hasCapturedTile
    ? (tileIdToCoord.get(result.tileCapturedId) ?? result.tileCapturedId)
    : null;

  const capturedTile = capturedTileCoord ? tiles.get(capturedTileCoord) : null;
  const isElimination = capturedTile?.isCastle === true;

  // Determine if there are more battles remaining after this one
  // declaredAttacks is already filtered to exclude the resolved battle
  const hasMoreBattles = declaredAttacks.length > 0;

  return (
    <div className="flex flex-col items-center gap-4 p-6">
      {/* Victory/Defeat banner */}
      <div
        className={`font-heading text-3xl font-bold ${isMyVictory ? 'text-green-400' : isSpectator ? 'text-gold-400' : 'text-red-400'}`}
      >
        {isMyVictory ? 'Victory!' : isSpectator ? `${winner} Wins!` : 'Defeat!'}
      </div>

      {/* Outcome details */}
      <div className="bg-ash-800/80 border border-ash-600 rounded-lg p-4 max-w-sm w-full">
        <div className="flex flex-col gap-2 text-sm">
          <div className="flex justify-between">
            <span className="text-parchment-400">Winner:</span>
            <span className="text-parchment-200 font-heading">{winner}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-parchment-400">Outcome:</span>
            <span className="text-parchment-200">
              {result.outcome === 'AttackerWon' ? 'Attack Successful' : 'Defense Held'}
            </span>
          </div>

          {/* Attacker vs defender */}
          <div className="flex justify-between text-xs text-parchment-500 pt-1 border-t border-ash-700">
            <span>{attackerKingdom?.name ?? 'Attacker'} (attacked)</span>
            <span>vs</span>
            <span>{defenderKingdom?.name ?? 'Defender'} (defended)</span>
          </div>

          {/* Tile captured */}
          {hasCapturedTile && capturedTileCoord && (
            <div className="flex justify-between">
              <span className="text-parchment-400">Tile Captured:</span>
              <span className="text-gold-400 font-heading">{capturedTileCoord}</span>
            </div>
          )}

          {/* Elimination */}
          {isElimination && (
            <div className="mt-2 py-2 border-t border-ash-600">
              <span className="text-red-400 font-heading text-base animate-pulse">
                {loser} has been eliminated!
              </span>
            </div>
          )}
        </div>
      </div>

      {/* Round summary */}
      <div className="text-xs text-parchment-500">
        {result.rounds.length} round{result.rounds.length !== 1 ? 's' : ''} fought
      </div>

      {/* Continue button */}
      <Button variant="primary" size="sm" onClick={onContinue}>
        {hasMoreBattles ? 'Next Battle' : 'Done'}
      </Button>
    </div>
  );
}
