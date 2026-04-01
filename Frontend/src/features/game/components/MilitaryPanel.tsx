import { useTranslation } from 'react-i18next';
import { useGameStore } from '../game-store';
import { trainArmy } from '../game-api';
import { UnitRow } from './UnitRow';
import type { ArmyTypeRef } from '../types/military-types';

interface MilitaryPanelProps {
  selectedTileKey: string | null;
}

const RESOURCE_COST_KEYS: { key: keyof ArmyTypeRef; resource: string }[] = [
  { key: 'trainingCostGold', resource: 'Gold' },
  { key: 'trainingCostFood', resource: 'Food' },
  { key: 'trainingCostStone', resource: 'Stone' },
  { key: 'trainingCostMana', resource: 'Mana' },
];

export function MilitaryPanel({ selectedTileKey }: MilitaryPanelProps) {
  const { t } = useTranslation();
  const armyTypes = useGameStore((s) => s.armyTypes);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const tiles = useGameStore((s) => s.tiles);
  const gameId = useGameStore((s) => s.gameId);

  const tile = selectedTileKey ? tiles.get(selectedTileKey) : undefined;
  const building = tile?.buildings[0] ?? null;

  const trainableUnits = building
    ? armyTypes.filter((at) => at.requiredBuildingTypeId === building.buildingTypeId)
    : [];

  const resources = (myKingdomId ? kingdoms.get(myKingdomId)?.resources : undefined) ?? {};

  function canAfford(at: ArmyTypeRef): boolean {
    for (const { key, resource } of RESOURCE_COST_KEYS) {
      const cost = at[key] as number;
      if (cost > 0 && (resources[resource] ?? 0) < cost) return false;
    }
    return true;
  }

  function getInsufficientResources(at: ArmyTypeRef): string[] {
    const insufficient: string[] = [];
    for (const { key, resource } of RESOURCE_COST_KEYS) {
      const cost = at[key] as number;
      if (cost > 0 && (resources[resource] ?? 0) < cost) insufficient.push(resource);
    }
    return insufficient;
  }

  function getMaxAffordable(at: ArmyTypeRef): number {
    let maxQty = Infinity;
    for (const { key, resource } of RESOURCE_COST_KEYS) {
      const cost = at[key] as number;
      if (cost > 0) {
        maxQty = Math.min(maxQty, Math.floor((resources[resource] ?? 0) / cost));
      }
    }
    return maxQty === Infinity ? 0 : maxQty;
  }

  function handleTrain(armyTypeId: string, quantity: number) {
    if (!gameId || !building) return;
    const promises = Array.from({ length: quantity }, () =>
      trainArmy(gameId, { buildingId: building!.id, armyTypeId }),
    );
    Promise.all(promises).catch((err) => console.error('Failed to train army:', err));
  }

  if (trainableUnits.length === 0) {
    return (
      <div className="text-bronze-400 text-sm px-3 py-2">{t('game.noUnitsHere')}</div>
    );
  }

  return (
    <div>
      {trainableUnits.map((at) => (
        <UnitRow
          key={at.id}
          armyType={at}
          canAfford={canAfford(at)}
          insufficientResources={getInsufficientResources(at)}
          maxAffordable={getMaxAffordable(at)}
          onTrain={handleTrain}
        />
      ))}
    </div>
  );
}
