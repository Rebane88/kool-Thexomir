import { Panel } from '@/shared/ui/Panel';
import { useGameStore } from '../game-store';
import { ResourcePanel } from './ResourcePanel';
import { TurnIndicator } from './TurnIndicator';
import { EndTurnButton } from './EndTurnButton';

export function GameHud() {
  const isMyTurn = useGameStore(
    (s) => s.myKingdomId !== null && s.currentTurnKingdomId === s.myKingdomId,
  );

  return (
    <Panel
      className={`absolute top-0 left-0 right-0 z-20 flex items-center justify-between px-4 py-2 ${isMyTurn ? 'border-gold-500 shadow-ember' : 'border-ash-600'}`}
    >
      <ResourcePanel />
      <TurnIndicator />
      <div className={isMyTurn ? '' : 'opacity-50 pointer-events-none'}>
        <EndTurnButton />
      </div>
    </Panel>
  );
}
