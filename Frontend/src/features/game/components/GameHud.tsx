import { Panel } from '@/shared/ui/Panel';
import { useGameStore } from '../game-store';
import { ResourcePanel } from './ResourcePanel';
import { TurnIndicator } from './TurnIndicator';
import { WinConditionLabel } from './WinConditionLabel';
import { EndTurnButton } from './EndTurnButton';

interface GameHudProps {
  onScoreboardToggle: () => void;
}

export function GameHud({ onScoreboardToggle }: GameHudProps) {
  const isMyTurn = useGameStore(
    (s) => s.myKingdomId !== null && s.currentTurnKingdomId === s.myKingdomId,
  );

  return (
    <Panel
      className={`absolute top-0 left-0 right-0 z-20 flex items-center justify-between px-4 py-2 ${isMyTurn ? 'border-gold-500 shadow-ember' : 'border-ash-600'}`}
    >
      <ResourcePanel />
      <div className="flex flex-col items-center gap-0.5">
        <TurnIndicator />
        <WinConditionLabel />
      </div>
      <div className="flex items-center gap-2">
        <button
          onClick={onScoreboardToggle}
          className="text-parchment-400 hover:text-gold-400 text-xs px-2 py-1 border border-bronze-700 rounded transition-colors"
          title="Toggle Scoreboard (Tab)"
        >
          Scores
        </button>
        <div className={isMyTurn ? '' : 'opacity-50 pointer-events-none'}>
          <EndTurnButton />
        </div>
      </div>
    </Panel>
  );
}
