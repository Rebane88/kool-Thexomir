import { useState, useEffect } from 'react';
import { useGameStore } from '../game-store';
import { BuildingRow } from './BuildingRow';
import { MilitaryPanel } from './MilitaryPanel';
import type { BuildingTypeRef } from '../types/building-types';

interface BuildingPanelProps {
  selectedTileKey: string;
}

export function BuildingPanel({ selectedTileKey }: BuildingPanelProps) {
  const buildingTypes = useGameStore((s) => s.buildingTypes);
  const buildModeTypeId = useGameStore((s) => s.buildModeTypeId);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const tiles = useGameStore((s) => s.tiles);
  const setBuildMode = useGameStore((s) => s.setBuildMode);
  const unitTypes = useGameStore((s) => s.unitTypes);

  const [activeTab, setActiveTab] = useState<'buildings' | 'military'>('buildings');

  // Reset tab when tile selection changes
  useEffect(() => setActiveTab('buildings'), [selectedTileKey]);

  const myKingdom = myKingdomId ? kingdoms.get(myKingdomId) : undefined;
  const resources = myKingdom?.resources ?? {};

  // Check if military tab should be visible
  const selectedTile = tiles.get(selectedTileKey);
  const existingBuilding = selectedTile?.buildings[0] ?? null;
  const isCastleTile = selectedTile?.isCastle === true;
  const hasMilitaryTab = existingBuilding !== null
    && unitTypes.some((ut) => ut.producedByBuildingTypeIds.includes(existingBuilding.buildingTypeId));

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

  // Context-aware catalog title
  const existingBuildingName = existingBuilding?.buildingName
    ?? buildingTypes.find((bt) => bt.id === existingBuilding?.buildingTypeId)?.name;
  const catalogTitle = existingBuilding
    ? `Upgrade ${existingBuildingName ?? 'Building'}`
    : 'Build';

  // If castle tile: no building catalog (castle is not upgradable)
  // If tile has a building: show possible upgrades
  // If empty tile: show Tier 1 buildings
  const displayedBuildings = isCastleTile
    ? []
    : existingBuilding
      ? buildingTypes.filter((bt) => bt.prerequisiteBuildingTypeId === existingBuilding.buildingTypeId)
      : buildingTypes.filter((bt) => bt.tier === 1);

  function handleSelect(typeId: string) {
    setBuildMode(buildModeTypeId === typeId ? null : typeId);
  }

  const buildingsContent = displayedBuildings.length === 0 ? (
    <div className="text-bronze-400 text-sm px-3 py-4 text-center">
      {isCastleTile ? 'Castle cannot be upgraded' : existingBuilding ? 'No upgrades available' : 'No buildings available'}
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
    </div>
  );

  return (
    <div className="stone-panel absolute top-12 right-0 bottom-0 w-72 z-30 overflow-y-auto p-3">
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
