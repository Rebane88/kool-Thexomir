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
  ArmyReveal,
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
  maxActionPoints: number | null;
  declaredAttacks: DeclaredAttack[];
  activeBattle: BattleStep | null;
  lastBattleResult: BattleResolvedEvent | null;

  // Bilateral readiness tracking for multi-kingdom battle steps
  battleReadiness: Map<string, { attackerReady: boolean; defenderReady: boolean }>;
  lineupReadiness: Map<string, { attackerReady: boolean; defenderReady: boolean }>;

  // Declare-attack flow state
  declareAttackMode: 'idle' | 'selectRiskedTile';
  declareAttackTargetTileId: string | null;
  declareAttackTargetTileKey: string | null;
  declareAttackDefenderKingdomId: string | null;

  // Battle flow local state
  battleSelections: Map<string, string[]>; // Map<attackId, armyId[]>
  battleReveals: Map<string, ArmyReveal>; // Map<attackId, reveal data>
  battleLineups: Map<string, string[]>; // Map<attackId, armyId[] in order>

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

  // Declare-attack actions
  setDeclareAttackTarget: (tileId: string, tileKey: string, defenderKingdomId: string) => void;
  cancelDeclareAttack: () => void;
  completeDeclareAttack: () => void;

  // Battle flow actions
  toggleArmySelection: (attackId: string, armyId: string) => void;
  clearBattleSelections: () => void;
  setBattleReveal: (attackId: string, reveal: ArmyReveal) => void;
  setBattleLineup: (attackId: string, armyIds: string[]) => void;
  moveLineupArmy: (attackId: string, armyId: string, direction: 'up' | 'down') => void;
  clearBattleFlow: () => void;

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
  maxActionPoints: null as number | null,
  declaredAttacks: [] as DeclaredAttack[],
  activeBattle: null as BattleStep | null,
  lastBattleResult: null as BattleResolvedEvent | null,
  battleReadiness: new Map<string, { attackerReady: boolean; defenderReady: boolean }>(),
  lineupReadiness: new Map<string, { attackerReady: boolean; defenderReady: boolean }>(),
  declareAttackMode: 'idle' as 'idle' | 'selectRiskedTile',
  declareAttackTargetTileId: null as string | null,
  declareAttackTargetTileKey: null as string | null,
  declareAttackDefenderKingdomId: null as string | null,
  battleSelections: new Map<string, string[]>(),
  battleReveals: new Map<string, ArmyReveal>(),
  battleLineups: new Map<string, string[]>(),
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
      maxActionPoints: null,
      declaredAttacks: [],
      activeBattle: null,
      battleReadiness: new Map(),
      lineupReadiness: new Map(),
      declareAttackMode: 'idle' as const,
      declareAttackTargetTileId: null,
      declareAttackTargetTileKey: null,
      declareAttackDefenderKingdomId: null,
      battleSelections: new Map(),
      battleReveals: new Map(),
      battleLineups: new Map(),
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
      maxActionPoints: null,
      declaredAttacks: [],
      activeBattle: null,
      lastBattleResult: null,
      battleReadiness: new Map(),
      lineupReadiness: new Map(),
      declareAttackMode: 'idle' as const,
      declareAttackTargetTileId: null,
      declareAttackTargetTileKey: null,
      declareAttackDefenderKingdomId: null,
      battleSelections: new Map(),
      battleReveals: new Map(),
      battleLineups: new Map(),
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

  setDeclareAttackTarget: (tileId, tileKey, defenderKingdomId) => set({
    declareAttackMode: 'selectRiskedTile',
    declareAttackTargetTileId: tileId,
    declareAttackTargetTileKey: tileKey,
    declareAttackDefenderKingdomId: defenderKingdomId,
  }),

  cancelDeclareAttack: () => set({
    declareAttackMode: 'idle',
    declareAttackTargetTileId: null,
    declareAttackTargetTileKey: null,
    declareAttackDefenderKingdomId: null,
  }),

  completeDeclareAttack: () => set({
    declareAttackMode: 'idle',
    declareAttackTargetTileId: null,
    declareAttackTargetTileKey: null,
    declareAttackDefenderKingdomId: null,
  }),

  toggleArmySelection: (attackId, armyId) => {
    const { battleSelections } = get();
    const current = battleSelections.get(attackId) ?? [];
    const newSelections = new Map(battleSelections);
    if (current.includes(armyId)) {
      // Remove from this battle
      newSelections.set(attackId, current.filter((id) => id !== armyId));
    } else {
      // Check army is not assigned to another battle
      const isAssignedElsewhere = [...battleSelections.entries()].some(
        ([bid, ids]) => bid !== attackId && ids.includes(armyId)
      );
      if (!isAssignedElsewhere) {
        newSelections.set(attackId, [...current, armyId]);
      }
    }
    set({ battleSelections: newSelections });
  },

  clearBattleSelections: () => set({ battleSelections: new Map() }),

  setBattleReveal: (attackId, reveal) => {
    const newReveals = new Map(get().battleReveals);
    newReveals.set(attackId, reveal);
    set({ battleReveals: newReveals });
  },

  setBattleLineup: (attackId, armyIds) => {
    const newLineups = new Map(get().battleLineups);
    newLineups.set(attackId, armyIds);
    set({ battleLineups: newLineups });
  },

  moveLineupArmy: (attackId, armyId, direction) => {
    const { battleLineups } = get();
    const lineup = [...(battleLineups.get(attackId) ?? [])];
    const index = lineup.indexOf(armyId);
    if (index === -1) return;
    if (direction === 'up' && index > 0) {
      [lineup[index - 1], lineup[index]] = [lineup[index], lineup[index - 1]];
    } else if (direction === 'down' && index < lineup.length - 1) {
      [lineup[index], lineup[index + 1]] = [lineup[index + 1], lineup[index]];
    }
    const newLineups = new Map(battleLineups);
    newLineups.set(attackId, lineup);
    set({ battleLineups: newLineups });
  },

  clearBattleFlow: () => set({
    battleSelections: new Map(),
    battleReveals: new Map(),
    battleLineups: new Map(),
    battleReadiness: new Map(),
    lineupReadiness: new Map(),
    activeBattle: null,
  }),

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
      battleReadiness: new Map(),
      lineupReadiness: new Map(),
      declareAttackMode: 'idle' as const,
      declareAttackTargetTileId: null,
      declareAttackTargetTileKey: null,
      declareAttackDefenderKingdomId: null,
      battleSelections: new Map(),
      battleReveals: new Map(),
      battleLineups: new Map(),
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
    const updates: Partial<ReturnType<typeof get>> = {
      currentPhase: data.phase as GamePhase,
      // Clear declare-attack mode whenever phase changes
      declareAttackMode: 'idle' as const,
      declareAttackTargetTileId: null,
      declareAttackTargetTileKey: null,
      declareAttackDefenderKingdomId: null,
    };
    if (data.phase === 'Battle') {
      updates.activeBattle = 'SelectArmies';
      updates.battleReadiness = new Map();
      updates.lineupReadiness = new Map();
      updates.battleSelections = new Map();
      updates.battleReveals = new Map();
      updates.battleLineups = new Map();
    }
    set(updates);
  },

  handleTurnStarted: (data) => {
    set({
      currentTurnKingdomId: data.kingdomId,
      actionPoints: data.actionPoints,
      maxActionPoints: data.actionPoints,
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
      // Do NOT set activeBattle here — battle step advances when Phase changes to Battle
    });
  },

  handleArmiesSelected: (data) => {
    const { declaredAttacks, battleReadiness } = get();
    const attack = declaredAttacks.find((da) => da.attackId === data.declaredAttackId);
    if (!attack) return;

    const existing = battleReadiness.get(data.declaredAttackId) ?? { attackerReady: false, defenderReady: false };
    const isAttacker = data.kingdomId === attack.attackerKingdomId;
    const updated = isAttacker
      ? { ...existing, attackerReady: true }
      : { ...existing, defenderReady: true };

    const newReadiness = new Map(battleReadiness);
    newReadiness.set(data.declaredAttackId, updated);

    // All battles ready when every declared attack has both sides confirmed
    const allReady = declaredAttacks.every((da) => {
      const r = da.attackId === data.declaredAttackId ? updated : battleReadiness.get(da.attackId);
      return r?.attackerReady && r?.defenderReady;
    });

    set({
      battleReadiness: newReadiness,
      ...(allReady ? { activeBattle: 'RevealArmies' } : {}),
    });
  },

  handleLineupSet: (data) => {
    const { declaredAttacks, lineupReadiness } = get();
    const attack = declaredAttacks.find((da) => da.attackId === data.declaredAttackId);
    if (!attack) return;

    const existing = lineupReadiness.get(data.declaredAttackId) ?? { attackerReady: false, defenderReady: false };
    const isAttacker = data.kingdomId === attack.attackerKingdomId;
    const updated = isAttacker
      ? { ...existing, attackerReady: true }
      : { ...existing, defenderReady: true };

    const newReadiness = new Map(lineupReadiness);
    newReadiness.set(data.declaredAttackId, updated);

    // All battles ready when every declared attack has both sides confirmed
    const allReady = declaredAttacks.every((da) => {
      const r = da.attackId === data.declaredAttackId ? updated : lineupReadiness.get(da.attackId);
      return r?.attackerReady && r?.defenderReady;
    });

    set({
      lineupReadiness: newReadiness,
      ...(allReady ? { activeBattle: 'Resolve' } : {}),
    });
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

    // Only clear activeBattle when ALL battles are resolved
    const allResolved = newDeclaredAttacks.length === 0;

    set({
      tiles: newTiles,
      armies: newArmies,
      lastBattleResult: data,
      activeBattle: allResolved ? null : get().activeBattle,
      declaredAttacks: newDeclaredAttacks,
      ...(allResolved ? {
        battleSelections: new Map(),
        battleReveals: new Map(),
        battleLineups: new Map(),
        battleReadiness: new Map(),
        lineupReadiness: new Map(),
      } : {}),
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
