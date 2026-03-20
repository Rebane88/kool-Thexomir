import { Panel } from '@/shared/ui/Panel';
import { useGameStore } from '../game-store';
import { ResourcePanel } from './ResourcePanel';
import { TurnIndicator } from './TurnIndicator';
import { GambleButton } from './GambleButton';
import { EndTurnButton } from './EndTurnButton';

interface GameHudProps {
  onStandingsToggle: () => void;
}

export function GameHud({ onStandingsToggle }: GameHudProps) {
  const isMyTurn = useGameStore(
    (s) => s.myKingdomId !== null && s.currentTurnKingdomId === s.myKingdomId,
  );

  return (
    <Panel
      className={`absolute top-0 left-0 right-0 z-40 flex items-center justify-between px-4 py-2 ${isMyTurn ? 'shadow-ember' : 'border-ash-600'}`}
      style={isMyTurn ? {
        borderColor: 'var(--faction-color, #c9a84c)',
        boxShadow: '0 0 12px var(--faction-color-glow, rgba(217, 119, 6, 0.2))',
      } : undefined}
    >
      <div className="stone-panel rounded-lg px-3 py-1.5 flex items-center"
        style={{
          boxShadow: isMyTurn
            ? '0 0 8px rgba(217, 119, 6, 0.3), inset 0 1px 0 rgba(201, 168, 76, 0.2)'
            : undefined,
        }}
      >
        <ResourcePanel />
      </div>
      <TurnIndicator />
      <div className="flex items-center gap-2">
        <GambleButton />
        <button
          onClick={onStandingsToggle}
          className="text-parchment-400 hover:text-gold-400 text-xs px-2 py-1 border border-bronze-700 rounded transition-colors"
          title="Toggle Standings (Tab)"
        >
          Standings
        </button>
        <EndTurnButton />
      </div>
    </Panel>
  );
}
