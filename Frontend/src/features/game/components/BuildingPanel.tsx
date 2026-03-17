import { Panel } from '@/shared/ui/Panel';
import { useGameStore } from '../game-store';
import { BuildingRow } from './BuildingRow';
import type { BuildingTypeRef } from '../types/building-types';

interface BuildingPanelProps {
  selectedTileKey: string;
}

export function BuildingPanel({ selectedTileKey: _selectedTileKey }: BuildingPanelProps) {
  const buildingTypes = useGameStore((s) => s.buildingTypes);
  const buildModeTypeId = useGameStore((s) => s.buildModeTypeId);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const tiles = useGameStore((s) => s.tiles);
  const setBuildMode = useGameStore((s) => s.setBuildMode);

  const myKingdom = myKingdomId ? kingdoms.get(myKingdomId) : undefined;
  const resources = myKingdom?.resources ?? {};

  function canAfford(bt: BuildingTypeRef): boolean {
    if (bt.goldCost > 0 && (resources['Gold'] ?? 0) < bt.goldCost) return false;
    if (bt.woodCost > 0 && (resources['Wood'] ?? 0) < bt.woodCost) return false;
    if (bt.stoneCost > 0 && (resources['Stone'] ?? 0) < bt.stoneCost) return false;
    if (bt.manaCost > 0 && (resources['Mana'] ?? 0) < bt.manaCost) return false;
    return true;
  }

  function getInsufficientResources(bt: BuildingTypeRef): string[] {
    const insufficient: string[] = [];
    if (bt.goldCost > 0 && (resources['Gold'] ?? 0) < bt.goldCost) insufficient.push('Gold');
    if (bt.woodCost > 0 && (resources['Wood'] ?? 0) < bt.woodCost) insufficient.push('Wood');
    if (bt.stoneCost > 0 && (resources['Stone'] ?? 0) < bt.stoneCost) insufficient.push('Stone');
    if (bt.manaCost > 0 && (resources['Mana'] ?? 0) < bt.manaCost) insufficient.push('Mana');
    return insufficient;
  }

  function hasPrerequisite(bt: BuildingTypeRef): boolean {
    if (!bt.prerequisiteBuildingTypeId) return true;
    for (const tile of tiles.values()) {
      if (tile.kingdomId !== myKingdomId) continue;
      if (tile.buildings.some((b) => b.buildingTypeId === bt.prerequisiteBuildingTypeId)) {
        return true;
      }
    }
    return false;
  }

  const grouped = new Map<string, BuildingTypeRef[]>();
  for (const bt of buildingTypes) {
    const existing = grouped.get(bt.chain) || [];
    existing.push(bt);
    grouped.set(bt.chain, existing);
  }

  function handleSelect(typeId: string) {
    setBuildMode(buildModeTypeId === typeId ? null : typeId);
  }

  return (
    <Panel className="absolute top-12 right-0 bottom-0 w-72 z-30 overflow-y-auto p-3 border-l border-bronze-700">
      <h2 className="text-parchment-100 font-heading text-sm font-semibold mb-3 uppercase tracking-wider">
        Buildings
      </h2>
      {[...grouped.entries()].map(([chain, types]) => (
        <div key={chain} className="mb-3">
          <div className="text-bronze-400 text-xs font-semibold uppercase tracking-wide mb-1 px-3">
            {chain}
          </div>
          {types.map((bt) => (
            <BuildingRow
              key={bt.id}
              buildingType={bt}
              canAfford={canAfford(bt)}
              hasPrerequisite={hasPrerequisite(bt)}
              insufficientResources={getInsufficientResources(bt)}
              isActive={buildModeTypeId === bt.id}
              onSelect={handleSelect}
            />
          ))}
        </div>
      ))}
    </Panel>
  );
}
