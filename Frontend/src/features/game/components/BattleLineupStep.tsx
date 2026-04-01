import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useGameStore } from '../game-store';
import { setLineup } from '../game-api';
import { ArmyCard } from './ArmyCard';
import { Button } from '@/shared/ui/Button';

export function BattleLineupStep() {
  const { t } = useTranslation();
  const declaredAttacks = useGameStore((s) => s.declaredAttacks);
  const battleLineups = useGameStore((s) => s.battleLineups);
  const battleSelections = useGameStore((s) => s.battleSelections);
  const armies = useGameStore((s) => s.armies);
  const armyTypes = useGameStore((s) => s.armyTypes);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const gameId = useGameStore((s) => s.gameId);

  const [confirmed, setConfirmed] = useState(false);

  const myBattles = declaredAttacks.filter(
    (da) => da.attackerKingdomId === myKingdomId || da.defenderKingdomId === myKingdomId,
  );

  // Initialize lineups from selections if not already set
  useEffect(() => {
    const state = useGameStore.getState();
    for (const battle of myBattles) {
      if (!state.battleLineups.has(battle.attackId)) {
        const selected = state.battleSelections.get(battle.attackId) ?? [];
        state.setBattleLineup(battle.attackId, [...selected]);
      }
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleConfirmAll = async () => {
    if (!gameId) return;
    setConfirmed(true);
    for (const battle of myBattles) {
      const armyIdsInOrder = battleLineups.get(battle.attackId) ??
        battleSelections.get(battle.attackId) ?? [];
      try {
        await setLineup(gameId, { declaredAttackId: battle.attackId, armyIdsInOrder });
      } catch (err) {
        console.error('Failed to set lineup:', err);
      }
    }
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex-1 overflow-y-auto p-4">
        {myBattles.map((battle) => {
          const opponentId =
            battle.attackerKingdomId === myKingdomId
              ? battle.defenderKingdomId
              : battle.attackerKingdomId;
          const opponentName = kingdoms.get(opponentId)?.name ?? 'Unknown';
          const lineup = battleLineups.get(battle.attackId) ??
            battleSelections.get(battle.attackId) ?? [];

          return (
            <div key={battle.attackId} className="mb-6">
              <h3 className="font-heading text-sm text-parchment-300 mb-2">
                vs {opponentName} — {t('game.lineup')}
              </h3>
              <div className="flex flex-col gap-1">
                {lineup.map((armyId, index) => {
                  const army = armies.get(armyId);
                  const armyType = armyTypes.find((ty) => ty.id === army?.armyTypeId);
                  if (!army || !armyType) return null;
                  return (
                    <div key={armyId} className="flex items-center gap-2">
                      <span className="text-xs text-parchment-500 w-6 text-right">
                        #{index + 1}
                      </span>
                      <div className="flex-1">
                        <ArmyCard army={army} armyType={armyType} compact />
                      </div>
                      <div className="flex flex-col">
                        <button
                          onClick={() =>
                            useGameStore.getState().moveLineupArmy(battle.attackId, armyId, 'up')
                          }
                          disabled={index === 0 || confirmed}
                          className="text-parchment-400 hover:text-gold-400 disabled:opacity-30 disabled:cursor-not-allowed p-0.5"
                        >
                          <svg viewBox="0 0 24 24" className="w-4 h-4">
                            <path d="M18 15l-6-6-6 6" stroke="currentColor" strokeWidth="2" fill="none" />
                          </svg>
                        </button>
                        <button
                          onClick={() =>
                            useGameStore.getState().moveLineupArmy(battle.attackId, armyId, 'down')
                          }
                          disabled={index === lineup.length - 1 || confirmed}
                          className="text-parchment-400 hover:text-gold-400 disabled:opacity-30 disabled:cursor-not-allowed p-0.5"
                        >
                          <svg viewBox="0 0 24 24" className="w-4 h-4">
                            <path d="M6 9l6 6 6-6" stroke="currentColor" strokeWidth="2" fill="none" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>

      {/* Bottom action bar */}
      <div className="border-t border-ash-700 p-4 flex items-center justify-end">
        {confirmed ? (
          <p className="text-parchment-400 text-sm animate-pulse w-full text-center">
            {t('game.waitingForOpponent')}
          </p>
        ) : (
          <Button variant="primary" onClick={handleConfirmAll} disabled={confirmed}>
            {t('game.confirmAllLineups')}
          </Button>
        )}
      </div>
    </div>
  );
}
