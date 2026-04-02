import { useTranslation } from 'react-i18next';
import { useGameStore } from '../game-store';
import { getKingdomColor } from '../canvas/hex-renderer';
import { FACTION_VISUALS } from '../../lobby/faction-constants';

const PHASE_COLOR: Record<string, string> = {
  Action: 'text-gold-400',
  Battle: 'text-red-400',
  Income: 'text-green-400',
  RoundEnd: 'text-parchment-400',
};

const PHASE_KEY: Record<string, string> = {
  Action: 'game.phase.action',
  Battle: 'game.phase.battle',
  Income: 'game.phase.income',
  RoundEnd: 'game.phase.roundEnd',
};

export function TurnIndicator() {
  const { t } = useTranslation();
  const roundNumber = useGameStore((s) => s.roundNumber);
  const currentPhase = useGameStore((s) => s.currentPhase);
  const currentTurnKingdomId = useGameStore((s) => s.currentTurnKingdomId);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const actionPoints = useGameStore((s) => s.actionPoints);
  const maxActionPoints = useGameStore((s) => s.maxActionPoints);

  const isMyTurn = myKingdomId !== null && currentTurnKingdomId === myKingdomId;
  const currentKingdom = currentTurnKingdomId
    ? kingdoms.get(currentTurnKingdomId)
    : undefined;
  const kingdomColor = currentTurnKingdomId
    ? getKingdomColor(kingdoms, currentTurnKingdomId)
    : '#6b6b6b';

  const phaseColor = currentPhase ? (PHASE_COLOR[currentPhase] ?? 'text-parchment-400') : null;
  const phaseLabel = currentPhase ? t(PHASE_KEY[currentPhase] ?? 'game.phase.action') : null;

  const factionCrest = currentKingdom?.factionTypeId
    ? FACTION_VISUALS[currentKingdom.factionTypeId.toLowerCase()]?.crestImage
    : undefined;

  const showAp = actionPoints !== null && maxActionPoints !== null;
  const pips = showAp
    ? Array.from({ length: maxActionPoints! }, (_, i) => i < actionPoints!)
    : [];

  return (
    <div
      className="flex items-center gap-3 px-4 py-1.5 rounded-lg border border-gold-500/60"
      style={{
        background: 'linear-gradient(135deg, #1a1a24 0%, #111118 50%, #1a1a24 100%)',
        boxShadow: 'inset 0 1px 0 rgba(201, 168, 76, 0.15), inset 0 -1px 0 rgba(0, 0, 0, 0.3), 0 0 0 1px #3d3225, 0 4px 12px rgba(0, 0, 0, 0.5)',
      }}
    >
      {phaseColor && phaseLabel && (
        <span className={`${phaseColor} font-heading font-bold text-xs uppercase tracking-wide`}>
          {phaseLabel}
        </span>
      )}
      <div className="flex items-center gap-2">
        {factionCrest && <img src={factionCrest} alt="" className="w-5 h-5 object-contain" />}
        <span
          className="inline-block w-3 h-3 rounded-full"
          style={{ backgroundColor: kingdomColor }}
        />
        {isMyTurn ? (
          <span className="text-gold-400 font-heading font-bold text-sm">
            {t('game.roundYou', { n: roundNumber })}
          </span>
        ) : (
          <span className="text-parchment-300 text-sm">
            {t('game.roundN', { n: roundNumber })} ({currentKingdom?.name || currentKingdom?.factionName || 'Waiting'})
          </span>
        )}
      </div>
      {showAp && (
        <>
          <div className="w-px h-4 bg-bronze-700" />
          <div className="flex items-center gap-1">
            {pips.map((filled, i) => (
              <span
                key={i}
                className={`inline-block w-2.5 h-2.5 rounded-full transition-all duration-300 ${
                  filled
                    ? 'bg-gold-400 scale-100'
                    : 'bg-transparent border border-ash-500 scale-90'
                }`}
              />
            ))}
            <span className="text-parchment-300 text-xs ml-1 tabular-nums">
              {actionPoints}/{maxActionPoints} {t('game.ap')}
            </span>
          </div>
        </>
      )}
    </div>
  );
}
