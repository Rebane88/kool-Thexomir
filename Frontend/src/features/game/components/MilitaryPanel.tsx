import { useGameStore } from '../game-store';
import { trainTroops } from '../game-api';
import { UnitRow } from './UnitRow';
import type { UnitTypeRef } from '../types/military-types';

interface MilitaryPanelProps {
  selectedTileKey: string;
}

const RESOURCE_COST_KEYS: { key: keyof UnitTypeRef; resource: string }[] = [
  { key: 'goldCost', resource: 'Gold' },
  { key: 'foodCost', resource: 'Food' },
  { key: 'woodCost', resource: 'Wood' },
  { key: 'stoneCost', resource: 'Stone' },
  { key: 'manaCost', resource: 'Mana' },
];

export function MilitaryPanel({ selectedTileKey }: MilitaryPanelProps) {
  const unitTypes = useGameStore((s) => s.unitTypes);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const tiles = useGameStore((s) => s.tiles);
  const gameId = useGameStore((s) => s.gameId);

  const tile = tiles.get(selectedTileKey);
  const building = tile?.buildings[0] ?? null;

  const trainableUnits = building
    ? unitTypes.filter((ut) => ut.producedByBuildingTypeIds.includes(building.buildingTypeId))
    : [];

  const resources = (myKingdomId ? kingdoms.get(myKingdomId)?.resources : undefined) ?? {};

  function canAfford(ut: UnitTypeRef): boolean {
    for (const { key, resource } of RESOURCE_COST_KEYS) {
      const cost = ut[key] as number;
      if (cost > 0 && (resources[resource] ?? 0) < cost) return false;
    }
    return true;
  }

  function getInsufficientResources(ut: UnitTypeRef): string[] {
    const insufficient: string[] = [];
    for (const { key, resource } of RESOURCE_COST_KEYS) {
      const cost = ut[key] as number;
      if (cost > 0 && (resources[resource] ?? 0) < cost) insufficient.push(resource);
    }
    return insufficient;
  }

  function getMaxAffordable(ut: UnitTypeRef): number {
    let maxQty = Infinity;
    for (const { key, resource } of RESOURCE_COST_KEYS) {
      const cost = ut[key] as number;
      if (cost > 0) {
        maxQty = Math.min(maxQty, Math.floor((resources[resource] ?? 0) / cost));
      }
    }
    return maxQty === Infinity ? 0 : maxQty;
  }

  function handleTrain(unitTypeId: string, quantity: number) {
    if (!gameId || !building) return;
    trainTroops(gameId, {
      buildingId: building.id,
      unitTypeId,
      quantity,
    }).catch((err) => console.error('Failed to train troops:', err));
  }

  if (trainableUnits.length === 0) {
    return (
      <div className="text-bronze-400 text-sm px-3 py-2">No units trainable here</div>
    );
  }

  return (
    <div>
      {trainableUnits.map((ut) => (
        <UnitRow
          key={ut.id}
          unitType={ut}
          canAfford={canAfford(ut)}
          insufficientResources={getInsufficientResources(ut)}
          maxAffordable={getMaxAffordable(ut)}
          onTrain={handleTrain}
        />
      ))}
    </div>
  );
}
