import { create } from 'zustand';
import type { Tile } from './types/map-types';
import type { Kingdom } from './types/kingdom-types';
import type { Army, ArmyTypeRef, DeclaredAttack } from './types/military-types';
import type { GameStatus, ConnectionStatus, GamePhase, BattleStep } from './types/enums';
import type { BuildingTypeRef } from './types/building-types';
import type {
  GameStateSnapshot,
  TurnAdvancedEvent,
  BuildingPlacedEvent,
  PhaseChangedEvent,
  TurnStartedEvent,
  RoundStartedEvent,
  SlotMachineSpunEvent,
  ArmyTrainedEvent,
  AttackDeclaredEvent,
  ArmiesSelectedEvent,
  LineupSetEvent,
  BattleResolvedEvent,
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
  roundNumber: number;
  currentTurnKingdomId: string | null;
  winCondition: string | null;
  mapWidth: number;
  mapHeight: number;
  gameOver: GameOverEvent | null;
  lastIncomeApplied: Record<string, number> | null;

  // v6.0 phase/AP tracking
  currentPhase: GamePhase | null;
  actionPoints: number | null;
  declaredAttacks: DeclaredAttack[];
  activeBattle: BattleStep | null;
  lastBattleResult: BattleResolvedEvent | null;

  // Building reference data
  buildingTypes: BuildingTypeRef[];
  buildModeTypeId: string | null;

  // Military reference data
  armyTypes: ArmyTypeRef[];

  // Connection state
  connectionStatus: ConnectionStatus;
  activeGameId: string | null;

  // Actions
  setBuildingTypes: (types: BuildingTypeRef[]) => void;
  setBuildMode: (typeId: string | null) => void;
  setArmyTypes: (types: ArmyTypeRef[]) => void;
  dismissBattleResult: () => void;
  loadSnapshot: (snapshot: GameStateSnapshot, userId: string) => void;
  resetState: () => void;
  setConnectionStatus: (status: ConnectionStatus) => void;
  setActiveGameId: (id: string | null) => void;

  // v6.0 event handlers
  handleTurnAdvanced: (data: TurnAdvancedEvent) => void;
  handleBuildingPlaced: (data: BuildingPlacedEvent) => void;
  handlePhaseChanged: (data: PhaseChangedEvent) => void;
  handleTurnStarted: (data: TurnStartedEvent) => void;
  handleRoundStarted: (data: RoundStartedEvent) => void;
  handleSlotMachineSpun: (data: SlotMachineSpunEvent) => void;
  handleArmyTrained: (data: ArmyTrainedEvent) => void;
  handleAttackDeclared: (data: AttackDeclaredEvent) => void;
  handleArmiesSelected: (data: ArmiesSelectedEvent) => void;
  handleLineupSet: (data: LineupSetEvent) => void;
  handleBattleResolved: (data: BattleResolvedEvent) => void;
  handleGameOver: (data: GameOverEvent) => void;
}

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

const initialState = {
  tiles: new Map<string, Tile>(),
  tileIdToCoord: new Map<string, string>(),
  kingdoms: new Map<string, Kingdom>(),
  armies: new Map<string, Army>(),
  myKingdomId: null,
  gameId: null,
  status: 'Lobby' as GameStatus,
  roundNumber: 0,
  currentTurnKingdomId: null,
  winCondition: null,
  mapWidth: 0,
  mapHeight: 0,
  gameOver: null,
  lastIncomeApplied: null,
  currentPhase: null as GamePhase | null,
  actionPoints: null as number | null,
  declaredAttacks: [] as DeclaredAttack[],
  activeBattle: null as BattleStep | null,
  lastBattleResult: null as BattleResolvedEvent | null,
  buildingTypes: [] as BuildingTypeRef[],
  buildModeTypeId: null as string | null,
  armyTypes: [] as ArmyTypeRef[],
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
        isCastle: t.isCastle,
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
        status: k.status,
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
        buildingId: a.buildingId,
        kingdomId: a.kingdomId,
        armyTypeId: a.armyTypeId,
        currentHP: a.currentHP,
        maxHP: a.maxHP,
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
      roundNumber: snapshot.roundNumber,
      currentTurnKingdomId: snapshot.currentTurnKingdomId,
      winCondition: snapshot.winCondition,
      mapWidth: snapshot.mapWidth,
      mapHeight: snapshot.mapHeight,
      gameOver: null,
      currentPhase: null,
      actionPoints: null,
      declaredAttacks: [],
      activeBattle: null,
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
      roundNumber: 0,
      currentTurnKingdomId: null,
      winCondition: null,
      mapWidth: 0,
      mapHeight: 0,
      gameOver: null,
      lastIncomeApplied: null,
      currentPhase: null,
      actionPoints: null,
      declaredAttacks: [],
      activeBattle: null,
      lastBattleResult: null,
      buildingTypes: [],
      buildModeTypeId: null,
      armyTypes: [],
      connectionStatus: 'disconnected',
      // activeGameId intentionally preserved — cleared only by setActiveGameId(null)
    });
  },

  setBuildingTypes: (types) => set({ buildingTypes: types }),

  setBuildMode: (typeId) => set({ buildModeTypeId: typeId }),

  setArmyTypes: (types) => set({ armyTypes: types }),

  dismissBattleResult: () => set({ lastBattleResult: null }),

  setConnectionStatus: (status) => set({ connectionStatus: status }),

  setActiveGameId: (id) => set({ activeGameId: id }),

  handleTurnAdvanced: (data) => {
    const { kingdoms, currentTurnKingdomId, myKingdomId } = get();
    const isMyIncome = currentTurnKingdomId !== null && currentTurnKingdomId === myKingdomId;

    const newKingdoms = new Map(kingdoms);
    if (currentTurnKingdomId && data.incomeApplied) {
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
      roundNumber: data.roundNumber,
      currentTurnKingdomId: data.nextKingdomId,
      currentPhase: (data.currentPhase as GamePhase) ?? get().currentPhase,
      actionPoints: data.actionPoints ?? get().actionPoints,
      kingdoms: newKingdoms,
      gameOver: data.gameOver ?? get().gameOver,
      lastIncomeApplied: isMyIncome ? data.incomeApplied : null,
      declaredAttacks: [],
    });
  },

  handleBuildingPlaced: (data) => {
    const { tiles, tileIdToCoord, kingdoms } = get();
    const coord = tileIdToCoord.get(data.tileId);
    if (!coord) return;

    const newTiles = new Map(tiles);
    const tile = newTiles.get(coord);
    if (tile) {
      if (data.isUpgrade) {
        // Replace: remove all existing buildings on the tile, add the upgrade
        newTiles.set(coord, {
          ...tile,
          buildings: [
            { id: data.buildingId, buildingTypeId: data.buildingTypeId, buildingName: data.buildingName },
          ],
        });
      } else {
        newTiles.set(coord, {
          ...tile,
          buildings: [
            ...tile.buildings,
            { id: data.buildingId, buildingTypeId: data.buildingTypeId, buildingName: data.buildingName },
          ],
        });
      }
    }

    // Claim tiles from claimedTileIds
    for (const claimedTileId of data.claimedTileIds) {
      const claimedCoord = tileIdToCoord.get(claimedTileId);
      if (claimedCoord) {
        const claimedTile = newTiles.get(claimedCoord);
        if (claimedTile) {
          newTiles.set(claimedCoord, { ...claimedTile, kingdomId: data.kingdomId });
        }
      }
    }

    const newKingdoms = new Map(kingdoms);
    const kingdom = newKingdoms.get(data.kingdomId);
    if (kingdom) {
      newKingdoms.set(data.kingdomId, { ...kingdom, resources: { ...data.resourcesAfter } });
    }

    set({ tiles: newTiles, kingdoms: newKingdoms });
  },

  handlePhaseChanged: (data) => {
    set({ currentPhase: data.phase as GamePhase });
  },

  handleTurnStarted: (data) => {
    set({
      currentTurnKingdomId: data.kingdomId,
      actionPoints: data.actionPoints,
      roundNumber: data.roundNumber,
    });
  },

  handleRoundStarted: (data) => {
    set({ roundNumber: data.roundNumber, declaredAttacks: [] });
  },

  handleSlotMachineSpun: (data) => {
    const { kingdoms, myKingdomId } = get();
    const newKingdoms = new Map(kingdoms);
    if (myKingdomId) {
      const kingdom = newKingdoms.get(myKingdomId);
      if (kingdom) {
        newKingdoms.set(myKingdomId, {
          ...kingdom,
          resources: { ...kingdom.resources, Gold: data.goldAfter },
        });
      }
    }
    set({ actionPoints: data.actionPointsAfter, kingdoms: newKingdoms });
  },

  handleArmyTrained: (data) => {
    const { armies, kingdoms } = get();
    const newArmies = new Map(armies);
    newArmies.set(data.armyId, {
      id: data.armyId,
      buildingId: data.buildingId,
      kingdomId: data.kingdomId,
      armyTypeId: data.armyTypeId,
      currentHP: data.currentHP,
      maxHP: data.maxHP,
    });

    const newKingdoms = new Map(kingdoms);
    const kingdom = newKingdoms.get(data.kingdomId);
    if (kingdom) {
      newKingdoms.set(data.kingdomId, { ...kingdom, resources: { ...data.resourcesAfter } });
    }

    set({ armies: newArmies, kingdoms: newKingdoms });
  },

  handleAttackDeclared: (data) => {
    set({
      declaredAttacks: [
        ...get().declaredAttacks,
        {
          attackId: data.attackId,
          targetTileId: data.targetTileId,
          riskedTileId: data.riskedTileId,
          attackerKingdomId: data.attackerKingdomId,
          defenderKingdomId: data.defenderKingdomId,
        },
      ],
      activeBattle: 'SelectArmies',
    });
  },

  handleArmiesSelected: (_data) => {
    set({ activeBattle: 'RevealArmies' });
  },

  handleLineupSet: (_data) => {
    set({ activeBattle: 'Resolve' });
  },

  handleBattleResolved: (data) => {
    const { tiles, tileIdToCoord, armies, declaredAttacks } = get();
    const newTiles = new Map(tiles);

    // Capture tile if applicable
    if (data.tileCapturedId && data.tileCapturedId !== EMPTY_GUID) {
      const coord = tileIdToCoord.get(data.tileCapturedId);
      if (coord) {
        const tile = newTiles.get(coord);
        if (tile) {
          newTiles.set(coord, { ...tile, kingdomId: data.attackerKingdomId });
        }
      }
    }

    // Remove destroyed armies
    const newArmies = new Map(armies);
    for (const round of data.rounds) {
      if (round.armyDestroyedId) {
        newArmies.delete(round.armyDestroyedId);
      }
    }

    // Remove the corresponding declared attack
    const newDeclaredAttacks = declaredAttacks.filter(
      (da) => da.attackId !== data.battleId,
    );

    set({
      tiles: newTiles,
      armies: newArmies,
      lastBattleResult: data,
      activeBattle: null,
      declaredAttacks: newDeclaredAttacks,
    });
  },

  handleGameOver: (data) => {
    set({
      status: 'Completed',
      gameOver: data,
      lastBattleResult: null,
    });
  },
}));
