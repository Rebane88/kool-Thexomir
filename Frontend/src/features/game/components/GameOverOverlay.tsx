import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button';
import { Badge } from '@/shared/ui/Badge';
import { useGameStore } from '../game-store';
import { getKingdomColor } from '../canvas/hex-renderer';
import victoryPng from '@/assets/images/gameover-victory.png';
import defeatPng from '@/assets/images/gameover-defeat.png';

export function GameOverOverlay() {
  const { t } = useTranslation();
  const gameOver = useGameStore((s) => s.gameOver);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const navigate = useNavigate();

  if (!gameOver) return null;

  const isWinner = gameOver.winnerKingdomId === myKingdomId;
  const winnerName = gameOver.finalStandings.find(
    (s) => s.kingdomId === gameOver.winnerKingdomId,
  )?.kingdomName ?? 'Unknown';
  const titleColor = isWinner
    ? 'text-gold-400'
    : gameOver.winnerKingdomId
      ? 'text-ember-400'
      : 'text-parchment-300';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Background art */}
      <div
        className="absolute inset-0 bg-cover bg-center bg-no-repeat"
        style={{
          backgroundImage: `url(${isWinner ? victoryPng : defeatPng})`,
        }}
      />
      {/* Translucent overlay for readability */}
      <div className="absolute inset-0 bg-black/60" />
      {/* Content */}
      <div className="relative z-10 max-w-lg w-full mx-4 p-8">
        <h1
          className={`font-heading text-3xl font-bold text-center mb-1 ${titleColor}`}
          style={{ textShadow: '0 2px 8px rgba(0,0,0,0.8)' }}
        >
          {gameOver.winnerKingdomId
            ? isWinner
              ? t('game.victory')
              : t('game.defeat')
            : t('game.gameOver')}
        </h1>

        <p className="text-center text-parchment-300 text-sm mb-6" style={{ textShadow: '0 1px 4px rgba(0,0,0,0.8)' }}>
          {gameOver.winnerKingdomId
            ? `${winnerName} wins by ${gameOver.winConditionType}`
            : `Game ended - ${gameOver.winConditionType}`}
        </p>

        <div className="bg-black/40 backdrop-blur-sm rounded-lg p-4 space-y-2 mb-6">
          <h2 className="font-heading text-gold-400 text-sm font-semibold">
            {t('game.finalStandings')}
          </h2>
          {gameOver.finalStandings
            .slice()
            .sort((a, b) => {
              if (a.kingdomId === gameOver.winnerKingdomId) return -1;
              if (b.kingdomId === gameOver.winnerKingdomId) return 1;
              return b.tilesOwned - a.tilesOwned;
            })
            .map((entry, index) => {
              const color = getKingdomColor(kingdoms, entry.kingdomId);
              const isMe = entry.kingdomId === myKingdomId;
              const isEntryWinner =
                entry.kingdomId === gameOver.winnerKingdomId;
              return (
                <div
                  key={entry.kingdomId}
                  className={`flex items-center gap-3 px-3 py-2 rounded ${isMe ? 'bg-white/10 border border-white/20' : ''} ${entry.status === 'Defeated' ? 'opacity-50' : ''}`}
                >
                  <span className="text-parchment-400 text-sm w-4">
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
                      <span className="text-parchment-400 text-xs ml-1">
                        (You)
                      </span>
                    )}
                  </span>
                  <span className="text-parchment-400 text-xs">
                    {entry.tilesOwned} tiles
                  </span>
                  {entry.status === 'Defeated' && (
                    <Badge variant="danger">{t('game.eliminated')}</Badge>
                  )}
                </div>
              );
            })}
        </div>

        <div className="flex justify-center">
          <Button onClick={() => navigate('/')}>{t('game.returnToLobby')}</Button>
        </div>
      </div>
    </div>
  );
}
