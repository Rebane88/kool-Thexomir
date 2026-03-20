import { useState, useEffect } from 'react';
import { useGameStore } from '../game-store';
import { BuildingRow } from './BuildingRow';
import { MilitaryPanel } from './MilitaryPanel';
import type { BuildingTypeRef } from '../types/building-types';

interface BuildingPanelProps {
  selectedTileKey: string | null;
}

export function BuildingPanel({ selectedTileKey }: BuildingPanelProps) {
  const buildingTypes = useGameStore((s) => s.buildingTypes);
  const buildModeTypeId = useGameStore((s) => s.buildModeTypeId);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const tiles = useGameStore((s) => s.tiles);
  const setBuildMode = useGameStore((s) => s.setBuildMode);
  const armyTypes = useGameStore((s) => s.armyTypes);

  const [activeTab, setActiveTab] = useState<'buildings' | 'military'>('buildings');

  // Reset tab when tile selection changes
  useEffect(() => setActiveTab('buildings'), [selectedTileKey]);

  const myKingdom = myKingdomId ? kingdoms.get(myKingdomId) : undefined;
  const resources = myKingdom?.resources ?? {};

  // Check if military tab should be visible
  const selectedTile = selectedTileKey ? tiles.get(selectedTileKey) : undefined;
  const isOwnTile = selectedTile?.kingdomId === myKingdomId;
  const existingBuilding = (isOwnTile ? selectedTile?.buildings[0] : null) ?? null;
  const isCastleTile = isOwnTile && selectedTile?.isCastle === true;
  const hasMilitaryTab = existingBuilding !== null
    && armyTypes.some((at) => at.requiredBuildingTypeId === existingBuilding.buildingTypeId);

  function canAfford(bt: BuildingTypeRef): boolean {
    if (bt.costGold > 0 && (resources['Gold'] ?? 0) < bt.costGold) return false;
    if (bt.costWood > 0 && (resources['Wood'] ?? 0) < bt.costWood) return false;
    if (bt.costStone > 0 && (resources['Stone'] ?? 0) < bt.costStone) return false;
    if (bt.costMana > 0 && (resources['Mana'] ?? 0) < bt.costMana) return false;
    return true;
  }

  function getInsufficientResources(bt: BuildingTypeRef): string[] {
    const insufficient: string[] = [];
    if (bt.costGold > 0 && (resources['Gold'] ?? 0) < bt.costGold) insufficient.push('Gold');
    if (bt.costWood > 0 && (resources['Wood'] ?? 0) < bt.costWood) insufficient.push('Wood');
    if (bt.costStone > 0 && (resources['Stone'] ?? 0) < bt.costStone) insufficient.push('Stone');
    if (bt.costMana > 0 && (resources['Mana'] ?? 0) < bt.costMana) insufficient.push('Mana');
    return insufficient;
  }

  function hasPrerequisite(bt: BuildingTypeRef): boolean {
    if (!bt.unlockedByBuildingTypeId) return true;
    for (const tile of tiles.values()) {
      if (tile.kingdomId !== myKingdomId) continue;
      if (tile.buildings.some((b) => b.buildingTypeId === bt.unlockedByBuildingTypeId)) {
        return true;
      }
    }
    return false;
  }

  // Context-aware catalog title & building list
  const existingBuildingName = existingBuilding?.buildingName
    ?? buildingTypes.find((bt) => bt.id === existingBuilding?.buildingTypeId)?.name;

  let catalogTitle: string;
  let displayedBuildings: BuildingTypeRef[];

  if (isCastleTile) {
    catalogTitle = 'Castle';
    displayedBuildings = [];
  } else if (existingBuilding) {
    catalogTitle = `Upgrade ${existingBuildingName ?? 'Building'}`;
    displayedBuildings = buildingTypes.filter((bt) => bt.unlockedByBuildingTypeId === existingBuilding.buildingTypeId);
  } else {
    catalogTitle = 'Build';
    displayedBuildings = buildingTypes.filter((bt) => bt.tier === 1);
  }

  function handleSelect(typeId: string) {
    setBuildMode(buildModeTypeId === typeId ? null : typeId);
  }

  const buildingsContent = displayedBuildings.length === 0 ? (
    <div className="text-bronze-400 text-sm px-3 py-4 text-center">
      {isCastleTile ? 'Castle cannot be upgraded' : existingBuilding ? 'No upgrades available' : 'Select a building, then click a tile to place it'}
    </div>
  ) : (
    <div className="space-y-1">
      {displayedBuildings.map((bt) => (
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
      {buildModeTypeId && (
        <div className="text-bronze-400 text-xs px-3 py-2 text-center italic">
          Click a highlighted tile to place
        </div>
      )}
    </div>
  );

  return (
    <div className="stone-panel absolute top-14 right-0 bottom-[44px] w-72 z-30 overflow-y-auto p-3">
      {hasMilitaryTab ? (
        <>
          <div className="flex gap-1 mb-3 border-b border-bronze-700">
            <button
              onClick={() => setActiveTab('buildings')}
              className={`text-sm font-semibold uppercase tracking-wider px-3 py-1.5 stone-panel-tab ${
                activeTab === 'buildings'
                  ? 'stone-panel-tab-active'
                  : 'text-bronze-400 hover:text-parchment-200'
              }`}
            >
              Buildings
            </button>
            <button
              onClick={() => setActiveTab('military')}
              className={`text-sm font-semibold uppercase tracking-wider px-3 py-1.5 stone-panel-tab ${
                activeTab === 'military'
                  ? 'stone-panel-tab-active'
                  : 'text-bronze-400 hover:text-parchment-200'
              }`}
            >
              Military
            </button>
          </div>
          {activeTab === 'buildings' ? buildingsContent : <MilitaryPanel selectedTileKey={selectedTileKey} />}
        </>
      ) : (
        <>
          <h2 className="text-parchment-100 font-heading text-sm font-semibold mb-3 uppercase tracking-wider">
            {catalogTitle}
          </h2>
          {buildingsContent}
        </>
      )}
    </div>
  );
}
