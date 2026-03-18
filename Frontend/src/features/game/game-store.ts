import { create } from 'zustand';
import type { Tile } from './types/map-types';
import type { Kingdom } from './types/kingdom-types';
import type { Army, UnitTypeRef } from './types/military-types';
import type { GameStatus, ConnectionStatus } from './types/enums';
import type { BuildingTypeRef } from './types/building-types';
import type {
  GameStateSnapshot,
  TurnAdvancedEvent,
  BuildingPlacedEvent,
  TroopsTrainedEvent,
  ArmyMovedEvent,
  CombatResolvedEvent,
  GameOverEvent,
} from './types/event-types';

interface GameState {
  // Data maps
  tiles: Map<string, Tile>;
  tileIdToCoord: Map<string, string>;
  kingdoms: Map<string, Kingdom>;
  armies: Map<string, Army>;

  // Player identity
  myKingdomId: string | null;

  // Game metadata
  gameId: string | null;
  status: GameStatus;
  turnNumber: number;
  currentTurnKingdomId: string | null;
  winCondition: string | null;
  mapRadius: number;
  gameOver: GameOverEvent | null;
  lastIncomeApplied: Record<string, number> | null;

  // Building reference data
  buildingTypes: BuildingTypeRef[];
  buildModeTypeId: string | null;

  // Military reference data
  unitTypes: UnitTypeRef[];
  lastCombatResult: CombatResolvedEvent | null;

  // Connection state
  connectionStatus: ConnectionStatus;
  activeGameId: string | null;

  // Actions
  setBuildingTypes: (types: BuildingTypeRef[]) => void;
  setBuildMode: (typeId: string | null) => void;
  setUnitTypes: (types: UnitTypeRef[]) => void;
  dismissCombatResult: () => void;
  loadSnapshot: (snapshot: GameStateSnapshot, userId: string) => void;
  resetState: () => void;
  setConnectionStatus: (status: ConnectionStatus) => void;
  setActiveGameId: (id: string | null) => void;
  handleTurnAdvanced: (data: TurnAdvancedEvent) => void;
  handleBuildingPlaced: (data: BuildingPlacedEvent) => void;
  handleTroopsTrained: (data: TroopsTrainedEvent) => void;
  handleArmyMoved: (data: ArmyMovedEvent) => void;
  handleCombatResolved: (data: CombatResolvedEvent) => void;
  handleGameOver: (data: GameOverEvent) => void;
}

const initialState = {
  tiles: new Map<string, Tile>(),
  tileIdToCoord: new Map<string, string>(),
  kingdoms: new Map<string, Kingdom>(),
  armies: new Map<string, Army>(),
  myKingdomId: null,
  gameId: null,
  status: 'Lobby' as GameStatus,
  turnNumber: 0,
  currentTurnKingdomId: null,
  winCondition: null,
  mapRadius: 0,
  gameOver: null,
  lastIncomeApplied: null,
  buildingTypes: [] as BuildingTypeRef[],
  buildModeTypeId: null as string | null,
  unitTypes: [] as UnitTypeRef[],
  lastCombatResult: null as CombatResolvedEvent | null,
  connectionStatus: 'disconnected' as ConnectionStatus,
  activeGameId: null,
};

export const useGameStore = create<GameState>((set, get) => ({
  ...initialState,

  loadSnapshot: (snapshot, userId) => {
    const tiles = new Map<string, Tile>();
    const tileIdToCoord = new Map<string, string>();

    for (const t of snapshot.tiles) {
      const key = `${t.coordQ},${t.coordR}`;
      tiles.set(key, {
        id: t.id,
        coordQ: t.coordQ,
        coordR: t.coordR,
        terrainTypeId: t.terrainTypeId,
        terrainName: t.terrainName,
        kingdomId: t.kingdomId,
        isCapital: t.isCapital,
        buildings: [...t.buildings],
      });
      tileIdToCoord.set(t.id, key);
    }

    const kingdoms = new Map<string, Kingdom>();
    let myKingdomId: string | null = null;

    for (const k of snapshot.kingdoms) {
      const resources = Object.fromEntries(
        k.resources.map((r) => [r.resourceType, r.amount]),
      );
      kingdoms.set(k.id, {
        id: k.id,
        name: k.name,
        userId: k.userId,
        factionTypeId: k.factionTypeId,
        factionName: k.factionName,
        isEliminated: k.isEliminated,
        resources,
      });
      if (k.userId === userId) {
        myKingdomId = k.id;
      }
    }

    const armies = new Map<string, Army>();
    for (const a of snapshot.armies) {
      armies.set(a.id, {
        id: a.id,
        tileId: a.tileId,
        kingdomId: a.kingdomId,
        units: a.units.map((u) => ({ ...u })),
      });
    }

    set({
      tiles,
      tileIdToCoord,
      kingdoms,
      armies,
      myKingdomId,
      gameId: snapshot.gameId,
      activeGameId: snapshot.gameId,
      status: snapshot.status as GameStatus,
      turnNumber: snapshot.turnNumber,
      currentTurnKingdomId: snapshot.currentTurnKingdomId,
      winCondition: snapshot.winCondition,
      mapRadius: snapshot.mapRadius,
      gameOver: null,
    });
  },

  resetState: () => {
    set({
      tiles: new Map(),
      tileIdToCoord: new Map(),
      kingdoms: new Map(),
      armies: new Map(),
      myKingdomId: null,
      gameId: null,
      status: 'Lobby',
      turnNumber: 0,
      currentTurnKingdomId: null,
      winCondition: null,
      mapRadius: 0,
      gameOver: null,
      lastIncomeApplied: null,
      buildingTypes: [],
      buildModeTypeId: null,
      unitTypes: [],
      lastCombatResult: null,
      connectionStatus: 'disconnected',
      // activeGameId intentionally preserved — cleared only by setActiveGameId(null)
    });
  },

  setBuildingTypes: (types) => set({ buildingTypes: types }),

  setBuildMode: (typeId) => set({ buildModeTypeId: typeId }),

  setUnitTypes: (types) => set({ unitTypes: types }),

  dismissCombatResult: () => set({ lastCombatResult: null }),

  setConnectionStatus: (status) => set({ connectionStatus: status }),

  setActiveGameId: (id) => set({ activeGameId: id }),

  handleTurnAdvanced: (data) => {
    const { kingdoms, currentTurnKingdomId, myKingdomId } = get();
    // Income applies to the kingdom whose turn just ended (currentTurnKingdomId)
    const isMyIncome = currentTurnKingdomId !== null && currentTurnKingdomId === myKingdomId;

    const newKingdoms = new Map(kingdoms);
    if (currentTurnKingdomId) {
      const kingdom = newKingdoms.get(currentTurnKingdomId);
      if (kingdom) {
        const updatedResources = { ...kingdom.resources };
        for (const [resourceType, amount] of Object.entries(data.incomeApplied)) {
          updatedResources[resourceType] = (updatedResources[resourceType] ?? 0) + amount;
        }
        newKingdoms.set(currentTurnKingdomId, { ...kingdom, resources: updatedResources });
      }
    }

    set({
      turnNumber: data.turnNumber,
      currentTurnKingdomId: data.newKingdomId,
      kingdoms: newKingdoms,
      gameOver: data.gameOver ?? get().gameOver,
      lastIncomeApplied: isMyIncome ? data.incomeApplied : null,
    });
  },

  handleBuildingPlaced: (data) => {
    const { tiles, tileIdToCoord, kingdoms } = get();
    const coord = tileIdToCoord.get(data.tileId);
    if (!coord) return;

    const newTiles = new Map(tiles);
    const tile = newTiles.get(coord);
    if (tile) {
      newTiles.set(coord, {
        ...tile,
        buildings: [
          ...tile.buildings,
          { id: data.buildingId, buildingTypeId: data.buildingTypeId, buildingName: data.buildingName },
        ],
      });
    }

    const newKingdoms = new Map(kingdoms);
    const kingdom = newKingdoms.get(data.kingdomId);
    if (kingdom) {
      newKingdoms.set(data.kingdomId, { ...kingdom, resources: { ...data.resourcesAfter } });
    }

    set({ tiles: newTiles, kingdoms: newKingdoms });
  },

  handleTroopsTrained: (data) => {
    const { armies, kingdoms } = get();
    const newArmies = new Map(armies);

    const existing = newArmies.get(data.armyId);
    if (existing) {
      const unitIndex = existing.units.findIndex((u) => u.unitTypeId === data.unitTypeId);
      const newUnits = [...existing.units];
      if (unitIndex >= 0) {
        newUnits[unitIndex] = { ...newUnits[unitIndex], quantity: data.totalQuantity };
      } else {
        newUnits.push({
          unitTypeId: data.unitTypeId,
          unitTypeName: data.unitTypeName,
          quantity: data.totalQuantity,
        });
      }
      newArmies.set(data.armyId, { ...existing, units: newUnits });
    } else {
      newArmies.set(data.armyId, {
        id: data.armyId,
        tileId: data.tileId,
        kingdomId: data.kingdomId,
        units: [
          {
            unitTypeId: data.unitTypeId,
            unitTypeName: data.unitTypeName,
            quantity: data.totalQuantity,
          },
        ],
      });
    }

    const newKingdoms = new Map(kingdoms);
    const kingdom = newKingdoms.get(data.kingdomId);
    if (kingdom) {
      newKingdoms.set(data.kingdomId, { ...kingdom, resources: { ...data.resourcesAfter } });
    }

    set({ armies: newArmies, kingdoms: newKingdoms });
  },

  handleArmyMoved: (data) => {
    const { armies, tiles, tileIdToCoord } = get();
    const newArmies = new Map(armies);
    const newTiles = new Map(tiles);

    if (data.armyMerged) {
      // Army was absorbed into another; remove it
      newArmies.delete(data.armyId);
    } else {
      const army = newArmies.get(data.armyId);
      if (army) {
        newArmies.set(data.armyId, { ...army, tileId: data.toTileId });
      }
    }

    if (data.tileClaimed) {
      const coord = tileIdToCoord.get(data.toTileId);
      if (coord) {
        const tile = newTiles.get(coord);
        if (tile) {
          newTiles.set(coord, { ...tile, kingdomId: data.kingdomId });
        }
      }
    }

    set({ armies: newArmies, tiles: newTiles });
  },

  handleCombatResolved: (data) => {
    const { tiles, tileIdToCoord, myKingdomId } = get();

    const newTiles = new Map(tiles);
    if (data.tileCaptured && data.winnerKingdomId) {
      const coord = tileIdToCoord.get(data.tileId);
      if (coord) {
        const tile = newTiles.get(coord);
        if (tile) {
          newTiles.set(coord, { ...tile, kingdomId: data.winnerKingdomId });
        }
      }
    }

    const isInvolved =
      myKingdomId === data.attackerKingdomId || myKingdomId === data.defenderKingdomId;

    set({
      tiles: newTiles,
      gameOver: data.gameOver ?? get().gameOver,
      lastCombatResult: isInvolved ? data : null,
    });
  },

  handleGameOver: (data) => {
    set({
      status: 'Completed',
      gameOver: data,
      lastCombatResult: null,
    });
  },
}));
