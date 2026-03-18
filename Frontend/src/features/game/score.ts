import type { Tile } from './types/map-types';
import type { Army } from './types/military-types';
import type { BuildingTypeRef } from './types/building-types';

/**
 * Calculate the score for a kingdom matching the backend ScoreChecker formula:
 *   score = (tilesOwned * 1) + (sum of building.tier * 2) + (sum of army unit quantities * 1)
 */
export function calculateKingdomScore(
  kingdomId: string,
  tiles: Map<string, Tile>,
  armies: Map<string, Army>,
  buildingTypes: BuildingTypeRef[],
): number {
  const tierMap = new Map<string, number>();
  for (const bt of buildingTypes) {
    tierMap.set(bt.id, bt.tier);
  }

  let score = 0;

  for (const tile of tiles.values()) {
    if (tile.kingdomId !== kingdomId) continue;
    score += 1; // TilePoints = 1
    for (const b of tile.buildings) {
      score += (tierMap.get(b.buildingTypeId) ?? 1) * 2; // BuildingTierMultiplier = 2
    }
  }

  for (const army of armies.values()) {
    if (army.kingdomId !== kingdomId) continue;
    for (const unit of army.units) {
      score += unit.quantity; // ArmyUnitPoints = 1
    }
  }

  return score;
}
