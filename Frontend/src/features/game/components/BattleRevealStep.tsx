import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useGameStore } from '../game-store';
import { revealArmies } from '../game-api';
import { HpBar } from './HpBar';
import type { RevealedArmy } from '../types/event-types';

interface ArmyRevealCardProps {
  army: RevealedArmy;
  revealed: boolean;
  delay: number;
}

function ArmyRevealCard({ army, revealed, delay }: ArmyRevealCardProps) {
  return (
    <div className="w-40 h-28" style={{ perspective: '600px' }}>
      <div
        className="relative w-full h-full transition-transform duration-500"
        style={{
          transformStyle: 'preserve-3d',
          transform: revealed ? 'rotateY(0deg)' : 'rotateY(180deg)',
          transitionDelay: `${delay}ms`,
        }}
      >
        {/* Front face */}
        <div
          className="absolute inset-0 bg-ash-800 border border-bronze-700 rounded-lg p-2 flex flex-col gap-1"
          style={{ backfaceVisibility: 'hidden' }}
        >
          <div className="font-heading text-xs text-parchment-200 truncate">{army.armyTypeName}</div>
          <HpBar currentHP={army.currentHP} maxHP={army.maxHP} />
          <div className="text-[10px] text-parchment-400 space-y-0.5">
            <div>ATK: {army.attack}</div>
            <div>INIT: {army.initiative}</div>
          </div>
        </div>

        {/* Back face */}
        <div
          className="absolute inset-0 bg-ash-700 border border-ash-500 rounded-lg flex items-center justify-center"
          style={{ backfaceVisibility: 'hidden', transform: 'rotateY(180deg)' }}
        >
          <span className="font-heading text-2xl text-parchment-500">???</span>
        </div>
      </div>
    </div>
  );
}

export function BattleRevealStep() {
  const { t } = useTranslation();
  const declaredAttacks = useGameStore((s) => s.declaredAttacks);
  const battleReveals = useGameStore((s) => s.battleReveals);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const gameId = useGameStore((s) => s.gameId);
  const activeBattle = useGameStore((s) => s.activeBattle);

  const [revealed, setRevealed] = useState(false);

  const myBattles = declaredAttacks.filter(
    (da) => da.attackerKingdomId === myKingdomId || da.defenderKingdomId === myKingdomId,
  );

  useEffect(() => {
    if (activeBattle !== 'RevealArmies' || !gameId) return;

    // Fetch reveal data for each battle
    for (const battle of myBattles) {
      if (battleReveals.has(battle.attackId)) continue;
      revealArmies(gameId, battle.attackId)
        .then((reveal) => {
          useGameStore.getState().setBattleReveal(battle.attackId, reveal);
        })
        .catch((err) => console.error('Failed to reveal armies:', err));
    }

    // Dramatic pause before flip
    const timer = setTimeout(() => setRevealed(true), 500);
    return () => clearTimeout(timer);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeBattle, gameId]);

  // Auto-advance to SetLineup after cards are revealed
  useEffect(() => {
    if (!revealed) return;
    // Wait for card flip animations to finish, then advance
    const timer = setTimeout(() => {
      useGameStore.setState({ activeBattle: 'SetLineup' });
    }, 2000);
    return () => clearTimeout(timer);
  }, [revealed]);

  return (
    <div className="flex flex-col gap-8 p-6 overflow-y-auto">
      {myBattles.map((battle) => {
        const reveal = battleReveals.get(battle.attackId);
        const attackerName = kingdoms.get(battle.attackerKingdomId)?.name ?? t('game.attacker');
        const defenderName = kingdoms.get(battle.defenderKingdomId)?.name ?? t('game.defender');

        return (
          <div key={battle.attackId} className="flex flex-col gap-4">
            <div className="flex gap-8 items-start justify-center">
              {/* Attacker side */}
              <div className="flex flex-col gap-2 items-center">
                <h4 className="font-heading text-sm text-parchment-300">{attackerName}</h4>
                {reveal
                  ? reveal.attackerArmies.map((army, i) => (
                      <ArmyRevealCard
                        key={army.armyId}
                        army={army}
                        revealed={revealed}
                        delay={i * 200}
                      />
                    ))
                  : (
                    <div className="w-40 h-28 bg-ash-700 border border-ash-500 rounded-lg flex items-center justify-center">
                      <span className="font-heading text-2xl text-parchment-500">???</span>
                    </div>
                  )}
              </div>

              <div className="text-2xl font-heading text-gold-400 self-center">VS</div>

              {/* Defender side */}
              <div className="flex flex-col gap-2 items-center">
                <h4 className="font-heading text-sm text-parchment-300">{defenderName}</h4>
                {reveal
                  ? reveal.defenderArmies.map((army, i) => (
                      <ArmyRevealCard
                        key={army.armyId}
                        army={army}
                        revealed={revealed}
                        delay={i * 200 + 100}
                      />
                    ))
                  : (
                    <div className="w-40 h-28 bg-ash-700 border border-ash-500 rounded-lg flex items-center justify-center">
                      <span className="font-heading text-2xl text-parchment-500">???</span>
                    </div>
                  )}
              </div>
            </div>
          </div>
        );
      })}

      {revealed && (
        <p className="text-center text-parchment-400 text-sm animate-pulse">
          {t('game.preparingLineups')}
        </p>
      )}
    </div>
  );
}
