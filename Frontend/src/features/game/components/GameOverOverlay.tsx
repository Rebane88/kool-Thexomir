import { useNavigate } from 'react-router';
import { Panel } from '@/shared/ui/Panel';
import { Button } from '@/shared/ui/Button';
import { Badge } from '@/shared/ui/Badge';
import { useGameStore } from '../game-store';
import { getKingdomColor } from '../canvas/hex-renderer';

export function GameOverOverlay() {
  const gameOver = useGameStore((s) => s.gameOver);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const navigate = useNavigate();

  if (!gameOver) return null;

  const isWinner = gameOver.winnerKingdomId === myKingdomId;
  const winnerScore = gameOver.finalScores.find(
    (s) => s.kingdomId === gameOver.winnerKingdomId,
  );
  const titleColor = isWinner
    ? 'text-gold-400'
    : gameOver.winnerKingdomId
      ? 'text-ember-400'
      : 'text-parchment-300';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70">
      <Panel className="relative max-w-lg w-full mx-4 p-8">
        <h1
          className={`font-heading text-2xl font-bold text-center mb-1 ${titleColor}`}
        >
          {gameOver.winnerKingdomId
            ? isWinner
              ? 'Victory!'
              : 'Defeat'
            : 'Game Over'}
        </h1>

        <p className="text-center text-parchment-400 text-sm mb-6">
          {gameOver.winnerKingdomId
            ? `${winnerScore?.kingdomName ?? 'Unknown'} wins by ${gameOver.winConditionType}`
            : `Game ended - ${gameOver.winConditionType}`}
        </p>

        <div className="space-y-2 mb-6">
          <h2 className="font-heading text-gold-400 text-sm font-semibold">
            Final Standings
          </h2>
          {gameOver.finalScores
            .slice()
            .sort((a, b) => b.score - a.score)
            .map((entry, index) => {
              const color = getKingdomColor(kingdoms, entry.kingdomId);
              const isMe = entry.kingdomId === myKingdomId;
              const isEntryWinner =
                entry.kingdomId === gameOver.winnerKingdomId;
              return (
                <div
                  key={entry.kingdomId}
                  className={`flex items-center gap-3 px-3 py-2 rounded ${isMe ? 'bg-bronze-800/50 border border-bronze-600' : ''} ${entry.isEliminated ? 'opacity-50' : ''}`}
                >
                  <span className="text-parchment-500 text-sm w-4">
                    {index + 1}.
                  </span>
                  <span
                    className="w-3 h-3 rounded-full flex-shrink-0"
                    style={{ backgroundColor: color }}
                  />
                  <span
                    className={`flex-1 text-sm ${isEntryWinner ? 'text-gold-400 font-semibold' : 'text-parchment-200'}`}
                  >
                    {entry.kingdomName}
                    {isMe && (
                      <span className="text-parchment-500 text-xs ml-1">
                        (You)
                      </span>
                    )}
                  </span>
                  <span className="text-parchment-300 text-sm">
                    {entry.score} pts
                  </span>
                  <span className="text-parchment-500 text-xs">
                    {entry.tilesOwned} tiles
                  </span>
                  {entry.isEliminated && (
                    <Badge variant="danger">Eliminated</Badge>
                  )}
                </div>
              );
            })}
        </div>

        <div className="flex justify-center">
          <Button onClick={() => navigate('/')}>Return to Lobby</Button>
        </div>
      </Panel>
    </div>
  );
}
