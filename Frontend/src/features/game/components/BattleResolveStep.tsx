import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useAnimationStore } from '../animation-store';
import { useGameStore } from '../game-store';
import { CombatSlotReel } from './CombatSlotReel';
import { HpBar } from './HpBar';
import { ArmyCard } from './ArmyCard';
import { BattleResolutionSummary } from './BattleResolutionSummary';
import type { Army, ArmyTypeRef } from '../types/military-types';

// --- Symbol generation helpers ---

function generateInitiativeSymbols(attackerName: string, defenderName: string): string[] {
  const base = [attackerName, defenderName];
  return [...base, ...base, ...base, ...base]; // 8 entries
}

function generateDamageSymbols(actualDamage: number): string[] {
  const min = Math.max(0, actualDamage - 5);
  const max = actualDamage + 5;
  const symbols: string[] = [];
  for (let i = min; i <= max; i++) symbols.push(String(i));
  return symbols;
}

// --- Component ---

export function BattleResolveStep() {
  const { t } = useTranslation();
  const currentRound = useAnimationStore((s) => s.currentRound);
  const currentBattleId = useAnimationStore((s) => s.currentBattleId);
  const battleRoundHistory = useAnimationStore((s) => s.battleRoundHistory);
  const combatSpinPhase = useAnimationStore((s) => s.combatSpinPhase);
  const combatSpinResults = useAnimationStore((s) => s.combatSpinResults);

  const armies = useGameStore((s) => s.armies);
  const armyTypes = useGameStore((s) => s.armyTypes);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const declaredAttacks = useGameStore((s) => s.declaredAttacks);
  const lastBattleResult = useGameStore((s) => s.lastBattleResult);
  const battleReveals = useGameStore((s) => s.battleReveals);
  const showBattleSummary = useGameStore((s) => s.showBattleSummary);

  // Track HP across rounds
  const [armyHpMap, setArmyHpMap] = useState<Map<string, { current: number; max: number }>>(new Map());
  // Track destroyed army for fade animation
  const [destroyedId, setDestroyedId] = useState<string | null>(null);

  // Initialize HP from battleReveals when a battle starts
  useEffect(() => {
    if (!currentBattleId) return;
    const reveal = battleReveals.get(currentBattleId);
    if (!reveal) return;

    const hpMap = new Map<string, { current: number; max: number }>();
    for (const a of reveal.attackerArmies) {
      hpMap.set(a.armyId, { current: a.currentHP, max: a.maxHP });
    }
    for (const a of reveal.defenderArmies) {
      hpMap.set(a.armyId, { current: a.currentHP, max: a.maxHP });
    }
    setArmyHpMap(hpMap);
  }, [currentBattleId]); // eslint-disable-line react-hooks/exhaustive-deps

  // --- Spin phase transition handlers ---

  const handleInitiativeStopped = useCallback(() => {
    if (!currentRound) return;
    useAnimationStore.getState().setCombatSpinResult('initiativeWinner', currentRound.initiativeWinner);
    setTimeout(() => {
      useAnimationStore.getState().setCombatSpinPhase('damage');
    }, 800);
  }, [currentRound]);

  const handleDamageStopped = useCallback(() => {
    if (!currentRound) return;
    useAnimationStore.getState().setCombatSpinResult('damageDealt', currentRound.damageDealt);
    setTimeout(() => {
      useAnimationStore.getState().setCombatSpinPhase('chipDamage');
    }, 600);
  }, [currentRound]);

  const handleChipDamageStopped = useCallback(() => {
    if (!currentRound) return;
    useAnimationStore.getState().setCombatSpinResult('chipDamageDealt', currentRound.chipDamageDealt);
    setTimeout(() => {
      // Update HP bars
      setArmyHpMap((prev) => {
        const next = new Map(prev);
        next.set(currentRound.attackerArmyId, {
          current: currentRound.attackerArmyHPAfter,
          max: prev.get(currentRound.attackerArmyId)?.max ?? currentRound.attackerArmyHPAfter,
        });
        next.set(currentRound.defenderArmyId, {
          current: currentRound.defenderArmyHPAfter,
          max: prev.get(currentRound.defenderArmyId)?.max ?? currentRound.defenderArmyHPAfter,
        });
        return next;
      });
      useAnimationStore.getState().setCombatSpinPhase('hpUpdate');

      // After HP animation completes
      setTimeout(() => {
        if (currentRound.armyDestroyedId) {
          setDestroyedId(currentRound.armyDestroyedId);
          useAnimationStore.getState().setCombatSpinPhase('destruction');
          setTimeout(() => {
            useAnimationStore.getState().setCombatSpinPhase('done');
            setDestroyedId(null);
          }, 1000);
        } else {
          useAnimationStore.getState().setCombatSpinPhase('done');
        }
      }, 1200); // HP bar animation duration
    }, 500);
  }, [currentRound]);

  // --- Derive display data ---

  // Find which declared attack corresponds to current battle
  const currentAttack = currentBattleId
    ? declaredAttacks.find((da) => da.attackId === currentBattleId)
    : null;

  const attackerKingdomName = currentAttack
    ? (kingdoms.get(currentAttack.attackerKingdomId)?.name ?? t('game.attacker'))
    : t('game.attacker');
  const defenderKingdomName = currentAttack
    ? (kingdoms.get(currentAttack.defenderKingdomId)?.name ?? t('game.defender'))
    : t('game.defender');

  // Current round army data
  const attackerArmyId = currentRound?.attackerArmyId ?? null;
  const defenderArmyId = currentRound?.defenderArmyId ?? null;

  const attackerArmy: Army | null = attackerArmyId ? (armies.get(attackerArmyId) ?? null) : null;
  const defenderArmy: Army | null = defenderArmyId ? (armies.get(defenderArmyId) ?? null) : null;

  const attackerType: ArmyTypeRef | null = attackerArmy
    ? (armyTypes.find((at) => at.id === attackerArmy.armyTypeId) ?? null)
    : null;
  const defenderType: ArmyTypeRef | null = defenderArmy
    ? (armyTypes.find((at) => at.id === defenderArmy.armyTypeId) ?? null)
    : null;

  // HP display values (from local tracking map, fallback to army store)
  const attackerHp = attackerArmyId
    ? (armyHpMap.get(attackerArmyId) ?? { current: attackerArmy?.currentHP ?? 0, max: attackerArmy?.maxHP ?? 1 })
    : { current: 0, max: 1 };
  const defenderHp = defenderArmyId
    ? (armyHpMap.get(defenderArmyId) ?? { current: defenderArmy?.currentHP ?? 0, max: defenderArmy?.maxHP ?? 1 })
    : { current: 0, max: 1 };

  // Initiative symbols
  const attackerTypeName = attackerType?.name ?? t('game.attacker');
  const defenderTypeName = defenderType?.name ?? t('game.defender');
  const initiativeSymbols = generateInitiativeSymbols(attackerTypeName, defenderTypeName);
  const initiativeTargetIndex = currentRound
    ? (currentRound.initiativeWinner === 'Attacker' ? 0 : 1)
    : 0;

  // Damage symbols
  const damageSymbols = currentRound ? generateDamageSymbols(currentRound.damageDealt) : ['0'];
  const damageTargetIndex = currentRound
    ? damageSymbols.indexOf(String(currentRound.damageDealt))
    : 0;

  // Chip damage symbols
  const chipDamageSymbols = currentRound ? generateDamageSymbols(currentRound.chipDamageDealt) : ['0'];
  const chipDamageTargetIndex = currentRound
    ? chipDamageSymbols.indexOf(String(currentRound.chipDamageDealt))
    : 0;

  // Show summary when battle resolved and acknowledgement pending
  if (showBattleSummary && lastBattleResult) {
    return (
      <BattleResolutionSummary
        result={lastBattleResult}
        onContinue={() => useGameStore.getState().acknowledgeBattleSummary()}
      />
    );
  }

  return (
    <div className="flex flex-col items-center gap-4 p-4 flex-1">
      {/* Round counter */}
      <div className="font-heading text-lg text-gold-400">
        {t('game.round')} {currentRound?.roundNumber ?? '...'}
      </div>

      {/* Battle arena: attacker card - slot machine - defender card */}
      <div className="flex items-center gap-6 justify-center">
        {/* Attacker army */}
        <div className="flex flex-col items-center gap-2">
          <span className="text-xs text-parchment-400">{attackerKingdomName}</span>
          {attackerArmy && attackerType && (
            <div className={destroyedId === attackerArmyId ? 'opacity-0 scale-75 transition-all duration-700' : ''}>
              <ArmyCard
                army={{ ...attackerArmy, currentHP: attackerHp.current }}
                armyType={attackerType}
                compact
              />
            </div>
          )}
          <HpBar currentHP={attackerHp.current} maxHP={attackerHp.max} className="w-24" />
        </div>

        {/* Slot machine area - 3 reels stacked vertically */}
        <div className="flex flex-col items-center gap-3">
          <CombatSlotReel
            spinning={combatSpinPhase === 'initiative'}
            symbols={initiativeSymbols}
            targetIndex={combatSpinPhase === 'initiative' ? initiativeTargetIndex : null}
            label={t('game.initiative')}
            onStopped={handleInitiativeStopped}
          />
          <CombatSlotReel
            spinning={combatSpinPhase === 'damage'}
            symbols={damageSymbols}
            targetIndex={combatSpinPhase === 'damage' ? damageTargetIndex : null}
            label={t('game.damage')}
            onStopped={handleDamageStopped}
          />
          <CombatSlotReel
            spinning={combatSpinPhase === 'chipDamage'}
            symbols={chipDamageSymbols}
            targetIndex={combatSpinPhase === 'chipDamage' ? chipDamageTargetIndex : null}
            label={t('game.chipDamage')}
            onStopped={handleChipDamageStopped}
          />
        </div>

        {/* Defender army */}
        <div className="flex flex-col items-center gap-2">
          <span className="text-xs text-parchment-400">{defenderKingdomName}</span>
          {defenderArmy && defenderType && (
            <div className={destroyedId === defenderArmyId ? 'opacity-0 scale-75 transition-all duration-700' : ''}>
              <ArmyCard
                army={{ ...defenderArmy, currentHP: defenderHp.current }}
                armyType={defenderType}
                compact
              />
            </div>
          )}
          <HpBar currentHP={defenderHp.current} maxHP={defenderHp.max} className="w-24" />
        </div>
      </div>

      {/* Initiative chance display */}
      {combatSpinPhase === 'initiative' && currentRound && (
        <div className="text-xs text-parchment-500">
          {Math.round(currentRound.attackerInitiativeChance * 100)}% vs {Math.round(currentRound.defenderInitiativeChance * 100)}%
        </div>
      )}

      {/* Result display after spin */}
      {combatSpinResults.initiativeWinner && combatSpinPhase !== 'initiative' && (
        <div className="text-sm text-gold-300 font-heading">
          {t('game.strikesFirst', { name: combatSpinResults.initiativeWinner === 'Attacker' ? attackerKingdomName : defenderKingdomName })}
        </div>
      )}
      {combatSpinResults.damageDealt !== null && combatSpinPhase !== 'damage' && (
        <div className="text-sm text-red-400 font-heading">
          {t('game.damageDealt', { amount: combatSpinResults.damageDealt })}
        </div>
      )}
      {combatSpinResults.chipDamageDealt !== null && combatSpinPhase !== 'chipDamage' && (
        <div className="text-xs text-amber-400 font-heading">
          {t('game.chipDamageDealt', { amount: combatSpinResults.chipDamageDealt })}
        </div>
      )}

      {/* Round history */}
      <div className="flex gap-2 mt-2">
        {battleRoundHistory.map((r, i) => (
          <div
            key={i}
            className="w-6 h-6 rounded-full bg-ash-700 flex items-center justify-center text-[10px] text-parchment-500"
          >
            {r.roundNumber}
          </div>
        ))}
      </div>
    </div>
  );
}
