import type { Building } from './map-types';

// --- Snapshot ---
export interface GameStateSnapshot {
  gameId: string;
  status: string;
  roundNumber: number;
  winCondition: string;
  mapWidth: number;
  mapHeight: number;
  mapRadius: number;
  currentTurnKingdomId: string | null;
  currentPhase: string | null;
  remainingActionPoints: number | null;
  spinCostGold: number;
  declaredAttacks: SnapshotDeclaredAttack[];
  tiles: SnapshotTile[];
  kingdoms: SnapshotKingdom[];
  armies: SnapshotArmy[];
}

export interface SnapshotDeclaredAttack {
  attackId: string;
  targetTileId: string;
  riskedTileId: string;
  attackerKingdomId: string;
  defenderKingdomId: string;
  attackerArmiesSelected: boolean;
  defenderArmiesSelected: boolean;
  attackerLineupConfirmed: boolean;
  defenderLineupConfirmed: boolean;
}

export interface SnapshotTile {
  id: string;
  coordQ: number;
  coordR: number;
  terrainTypeId: string;
  terrainName: string;
  kingdomId: string | null;
  isCastle: boolean;
  buildings: Building[];
}

export interface SnapshotKingdom {
  id: string;
  name: string;
  userId: string | null;
  factionTypeId: string | null;
  factionName: string | null;
  status: string;
  resources: { resourceType: string; amount: number }[];
}

export interface SnapshotArmy {
  id: string;
  buildingId: string;
  kingdomId: string;
  armyTypeId: string;
  currentHP: number;
  maxHP: number;
}

// --- Turn events ---
export interface TurnAdvancedEvent {
  nextKingdomId: string | null;
  roundNumber: number;
  currentPhase: string;
  actionPoints: number | null;
  turnDeadline: string | null;
  incomeApplied: Record<string, Record<string, number>> | null;
  battleResults: BattleResolvedEvent[] | null;
  phaseChanged: boolean;
  gameOver: GameOverEvent | null;
}

export interface PhaseChangedEvent {
  phase: string;
  previousPhase: string;
  roundNumber: number;
}

export interface TurnStartedEvent {
  kingdomId: string;
  kingdomName: string;
  actionPoints: number;
  turnDeadline: string | null;
  roundNumber: number;
}

export interface RoundStartedEvent {
  roundNumber: number;
}

// --- Building events ---
export interface BuildingPlacedEvent {
  buildingId: string;
  tileId: string;
  buildingTypeId: string;
  buildingName: string;
  kingdomId: string;
  resourcesAfter: Record<string, number>;
  claimedTileIds: string[];
  isUpgrade: boolean;
  actionPointsAfter: number;
}

// --- Slot machine events ---
export interface SlotMachineSpunEvent {
  kingdomId: string;
  outcome: number;
  actionPointsAfter: number;
  goldAfter: number;
  goldSpent: number;
}

// --- Army events ---
export interface ArmyTrainedEvent {
  armyId: string;
  buildingId: string;
  armyTypeId: string;
  armyTypeName: string;
  kingdomId: string;
  currentHP: number;
  maxHP: number;
  resourcesAfter: Record<string, number>;
  actionPointsAfter: number;
}

// --- Combat events ---
export interface AttackDeclaredEvent {
  attackId: string;
  targetTileId: string;
  riskedTileId: string;
  attackerKingdomId: string;
  defenderKingdomId: string;
  actionPointsAfter: number;
}

export interface ArmiesSelectedEvent {
  declaredAttackId: string;
  kingdomId: string;
  armiesSelected: number;
  maxArmies: number;
}

export interface LineupSetEvent {
  declaredAttackId: string;
  kingdomId: string;
  armiesSelected: number;
  maxArmies: number;
}

export interface BattleResolvedEvent {
  battleId: string;
  attackerKingdomId: string;
  defenderKingdomId: string;
  outcome: string;
  tileCapturedId: string;
  tileCapturedFromKingdomId: string;
  rounds: BattleRoundResult[];
}

export interface BattleRoundResult {
  roundNumber: number;
  attackerArmyId: string;
  defenderArmyId: string;
  initiativeWinner: string;
  attackerInitiativeChance: number;
  defenderInitiativeChance: number;
  damageDealt: number;
  chipDamageDealt: number;
  attackerArmyHPAfter: number;
  defenderArmyHPAfter: number;
  armyDestroyedId: string | null;
}

export interface BattleRoundResolvedEvent extends BattleRoundResult {
  battleId: string;
}

export interface ArmyReveal {
  declaredAttackId: string;
  attackerKingdomId: string;
  defenderKingdomId: string;
  attackerArmies: RevealedArmy[];
  defenderArmies: RevealedArmy[];
}

export interface RevealedArmy {
  armyId: string;
  armyTypeId: string;
  armyTypeName: string;
  currentHP: number;
  maxHP: number;
  attack: number;
  initiative: number;
}

export interface GameOverEvent {
  gameId: string;
  winnerKingdomId: string | null;
  winConditionType: string;
  finalStandings: KingdomResult[];
  eliminationOrder: string[];
}

export interface KingdomResult {
  kingdomId: string;
  kingdomName: string;
  tilesOwned: number;
  status: string;
}
