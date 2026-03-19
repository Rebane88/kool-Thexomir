import { useState } from 'react';
import { useGameStore } from '../game-store';
import { selectArmies } from '../game-api';
import { ArmyCard } from './ArmyCard';
import { Button } from '@/shared/ui/Button';

export function BattleSelectStep() {
  const armies = useGameStore((s) => s.armies);
  const armyTypes = useGameStore((s) => s.armyTypes);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const declaredAttacks = useGameStore((s) => s.declaredAttacks);
  const battleSelections = useGameStore((s) => s.battleSelections);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const gameId = useGameStore((s) => s.gameId);

  const [confirmed, setConfirmed] = useState(false);
  const [selectedArmyId, setSelectedArmyId] = useState<string | null>(null);

  const myBattles = declaredAttacks.filter(
    (da) => da.attackerKingdomId === myKingdomId || da.defenderKingdomId === myKingdomId,
  );

  const myArmies = [...armies.values()].filter((a) => a.kingdomId === myKingdomId);

  const isArmyAssigned = (armyId: string): boolean => {
    return [...battleSelections.values()].some((ids) => ids.includes(armyId));
  };

  const getAssignedBattle = (armyId: string): string | null => {
    for (const [bid, ids] of battleSelections.entries()) {
      if (ids.includes(armyId)) return bid;
    }
    return null;
  };

  const handleArmyClick = (armyId: string) => {
    const assignedBattle = getAssignedBattle(armyId);
    if (assignedBattle) {
      // Unassign
      useGameStore.getState().toggleArmySelection(assignedBattle, armyId);
      if (selectedArmyId === armyId) setSelectedArmyId(null);
    } else {
      // Select for assignment
      setSelectedArmyId((prev) => (prev === armyId ? null : armyId));
    }
  };

  const handleBattleColumnClick = (attackId: string) => {
    if (!selectedArmyId) return;
    useGameStore.getState().toggleArmySelection(attackId, selectedArmyId);
    setSelectedArmyId(null);
  };

  const handleConfirmAll = async () => {
    if (!gameId) return;
    setConfirmed(true);
    for (const battle of myBattles) {
      const armyIds = battleSelections.get(battle.attackId) ?? [];
      try {
        await selectArmies(gameId, { declaredAttackId: battle.attackId, armyIds });
      } catch (err) {
        console.error('Failed to select armies:', err);
      }
    }
  };

  return (
    <div className="flex flex-col h-full">
      {selectedArmyId && (
        <div className="text-center py-2 text-xs text-gold-400 font-heading animate-pulse">
          Click a battle column to assign army
        </div>
      )}
      <div className="flex gap-4 p-4 overflow-x-auto flex-1">
        {/* Roster column */}
        <div className="min-w-[180px] flex-shrink-0">
          <h3 className="font-heading text-sm text-parchment-300 mb-2">Your Armies</h3>
          <div className="flex flex-col gap-2">
            {myArmies.map((army) => {
              const armyType = armyTypes.find((t) => t.id === army.armyTypeId);
              if (!armyType) return null;
              const assigned = isArmyAssigned(army.id);
              return (
                <ArmyCard
                  key={army.id}
                  army={army}
                  armyType={armyType}
                  selected={selectedArmyId === army.id || assigned}
                  onClick={() => handleArmyClick(army.id)}
                  compact
                />
              );
            })}
          </div>
        </div>

        {/* One column per battle */}
        {myBattles.map((battle) => {
          const opponentId =
            battle.attackerKingdomId === myKingdomId
              ? battle.defenderKingdomId
              : battle.attackerKingdomId;
          const opponentName = kingdoms.get(opponentId)?.name ?? 'Unknown';
          const selectedIds = battleSelections.get(battle.attackId) ?? [];

          return (
            <div
              key={battle.attackId}
              className="min-w-[180px] flex-shrink-0 border-l border-ash-600 pl-4 cursor-pointer"
              onClick={() => handleBattleColumnClick(battle.attackId)}
            >
              <h3 className="font-heading text-sm text-parchment-300 mb-1">vs {opponentName}</h3>
              <span className="text-xs text-parchment-500">{selectedIds.length} selected</span>
              <div className="flex flex-col gap-2 mt-2">
                {selectedIds.map((id) => {
                  const army = armies.get(id);
                  const armyType = armyTypes.find((t) => t.id === army?.armyTypeId);
                  if (!army || !armyType) return null;
                  return (
                    <div
                      key={id}
                      onClick={(e) => {
                        e.stopPropagation();
                        useGameStore.getState().toggleArmySelection(battle.attackId, id);
                      }}
                    >
                      <ArmyCard
                        army={army}
                        armyType={armyType}
                        selected
                        compact
                      />
                    </div>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>

      {/* Bottom action bar */}
      <div className="border-t border-ash-700 p-4 flex items-center justify-between">
        {confirmed ? (
          <p className="text-parchment-400 text-sm animate-pulse w-full text-center">
            Waiting for opponent...
          </p>
        ) : (
          <>
            <p className="text-xs text-parchment-500">
              Select armies from your roster, then click a battle column to assign
            </p>
            <Button
              variant="primary"
              onClick={handleConfirmAll}
              disabled={confirmed}
            >
              Confirm All
            </Button>
          </>
        )}
      </div>
    </div>
  );
}
