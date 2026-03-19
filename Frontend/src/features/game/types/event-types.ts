import type { Building } from './map-types';

// Snapshot (received on connect / reconnect)
export interface GameStateSnapshot {
  gameId: string;
  status: string;
  turnNumber: number;
  winCondition: string;
  mapRadius: number;
  currentTurnKingdomId: string | null;
  tiles: SnapshotTile[];
  kingdoms: SnapshotKingdom[];
  armies: SnapshotArmy[];
}

export interface SnapshotTile {
  id: string;
  coordQ: number;
  coordR: number;
  terrainTypeId: string;
  terrainName: string;
  kingdomId: string | null;
  isCapital: boolean;
  buildings: Building[];
}

export interface SnapshotKingdom {
  id: string;
  name: string;
  userId: string | null;
  factionTypeId: string | null;
  factionName: string | null;
  isEliminated: boolean;
  resources: { resourceType: string; amount: number }[];
}

export interface SnapshotArmy {
  id: string;
  tileId: string;
  kingdomId: string;
  units: { unitTypeId: string; unitTypeName: string; quantity: number }[];
}

// Delta events
export interface TurnAdvancedEvent {
  newKingdomId: string;
  turnNumber: number;
  incomeApplied: Record<string, number>;
  gameOver: GameOverEvent | null;
}

export interface BuildingPlacedEvent {
  buildingId: string;
  tileId: string;
  buildingTypeId: string;
  buildingName: string;
  kingdomId: string;
  resourcesAfter: Record<string, number>;
}

export interface TroopsTrainedEvent {
  armyId: string;
  tileId: string;
  unitTypeId: string;
  unitTypeName: string;
  quantityTrained: number;
  totalQuantity: number;
  kingdomId: string;
  resourcesAfter: Record<string, number>;
}

export interface ArmyMovedEvent {
  armyId: string;
  fromTileId: string;
  toTileId: string;
  kingdomId: string;
  tileClaimed: boolean;
  armyMerged: boolean;
  mergedIntoArmyId: string | null;
}

export interface CombatResolvedEvent {
  battleId: string;
  tileId: string;
  attackerKingdomId: string;
  defenderKingdomId: string;
  winnerKingdomId: string | null;
  tileCaptured: boolean;
  attackerStrength: number;
  defenderStrength: number;
  attackerCasualties: { unitTypeId: string; unitTypeName: string; before: number; lost: number }[];
  defenderCasualties: { unitTypeId: string; unitTypeName: string; before: number; lost: number }[];
  gameOver: GameOverEvent | null;
}

export interface GameOverEvent {
  gameId: string;
  winnerKingdomId: string | null;
  winConditionType: string;
  finalStandings: { kingdomId: string; kingdomName: string; tilesOwned: number; status: string }[];
  eliminationOrder: string[];
}
