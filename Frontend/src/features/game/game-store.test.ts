import { describe, it, expect, beforeEach } from 'vitest';
import { useGameStore } from './game-store';
import type {
  GameStateSnapshot,
  TurnAdvancedEvent,
  BuildingPlacedEvent,
  ArmyTrainedEvent,
  SlotMachineSpunEvent,
  AttackDeclaredEvent,
  ArmiesSelectedEvent,
  LineupSetEvent,
  BattleResolvedEvent,
  PhaseChangedEvent,
  TurnStartedEvent,
  RoundStartedEvent,
  GameOverEvent,
} from './types';

function createMockSnapshot(): GameStateSnapshot {
  return {
    gameId: 'game-1',
    status: 'InProgress',
    roundNumber: 3,
    winCondition: 'Elimination',
    mapWidth: 10,
    mapHeight: 8,
    currentTurnKingdomId: 'k-1',
    tiles: [
      {
        id: 'tile-1',
        coordQ: 0,
        coordR: 0,
        terrainTypeId: 'plains',
        terrainName: 'Plains',
        kingdomId: 'k-1',
        isCastle: true,
        buildings: [{ id: 'b-1', buildingTypeId: 'castle', buildingName: 'Castle' }],
      },
      {
        id: 'tile-2',
        coordQ: 1,
        coordR: 0,
        terrainTypeId: 'forest',
        terrainName: 'Forest',
        kingdomId: 'k-2',
        isCastle: false,
        buildings: [],
      },
      {
        id: 'tile-3',
        coordQ: 0,
        coordR: 1,
        terrainTypeId: 'mountain',
        terrainName: 'Mountain',
        kingdomId: null,
        isCastle: false,
        buildings: [],
      },
    ],
    kingdoms: [
      {
        id: 'k-1',
        name: 'Kingdom Alpha',
        userId: 'user-1',
        factionTypeId: 'human',
        factionName: 'Humans',
        status: 'Active',
        resources: [
          { resourceType: 'Gold', amount: 100 },
          { resourceType: 'Food', amount: 50 },
        ],
      },
      {
        id: 'k-2',
        name: 'Kingdom Beta',
        userId: 'user-2',
        factionTypeId: 'elf',
        factionName: 'Elves',
        status: 'Active',
        resources: [{ resourceType: 'Gold', amount: 80 }],
      },
    ],
    armies: [
      {
        id: 'army-1',
        buildingId: 'b-1',
        kingdomId: 'k-1',
        armyTypeId: 'warrior',
        currentHP: 100,
        maxHP: 100,
      },
    ],
  };
}

describe('loadSnapshot', () => {
  beforeEach(() => {
    useGameStore.getState().resetState();
  });

  it('populates tiles map keyed by q,r', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const tile = useGameStore.getState().tiles.get('0,0');
    expect(tile).toBeDefined();
    expect(tile!.id).toBe('tile-1');
    expect(tile!.terrainName).toBe('Plains');
    expect(tile!.isCastle).toBe(true);
    expect(useGameStore.getState().tiles.size).toBe(3);
  });

  it('populates tileIdToCoord reverse map', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    expect(useGameStore.getState().tileIdToCoord.get('tile-1')).toBe('0,0');
    expect(useGameStore.getState().tileIdToCoord.get('tile-2')).toBe('1,0');
    expect(useGameStore.getState().tileIdToCoord.get('tile-3')).toBe('0,1');
  });

  it('populates kingdoms with status string (not isEliminated)', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const kingdom = useGameStore.getState().kingdoms.get('k-1');
    expect(kingdom).toBeDefined();
    expect(kingdom!.name).toBe('Kingdom Alpha');
    expect(kingdom!.status).toBe('Active');
    expect(kingdom!.factionTypeId).toBe('human');
    expect(useGameStore.getState().kingdoms.size).toBe(2);
  });

  it('converts kingdom resources array to record', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const kingdom = useGameStore.getState().kingdoms.get('k-1')!;
    expect(kingdom.resources).toEqual({ Gold: 100, Food: 50 });
  });

  it('populates armies with v6.0 shape (buildingId, armyTypeId, HP)', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const army = useGameStore.getState().armies.get('army-1');
    expect(army).toBeDefined();
    expect(army!.buildingId).toBe('b-1');
    expect(army!.armyTypeId).toBe('warrior');
    expect(army!.currentHP).toBe(100);
    expect(army!.maxHP).toBe(100);
  });

  it('identifies player kingdom', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    expect(useGameStore.getState().myKingdomId).toBe('k-1');
  });

  it('sets myKingdomId null when no match', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'no-such-user');

    expect(useGameStore.getState().myKingdomId).toBeNull();
  });

  it('sets game metadata with roundNumber, mapWidth, mapHeight', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const state = useGameStore.getState();
    expect(state.gameId).toBe('game-1');
    expect(state.status).toBe('InProgress');
    expect(state.roundNumber).toBe(3);
    expect(state.currentTurnKingdomId).toBe('k-1');
    expect(state.winCondition).toBe('Elimination');
    expect(state.mapWidth).toBe(10);
    expect(state.mapHeight).toBe(8);
  });

  it('initializes v6.0 phase/AP fields on snapshot load', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const state = useGameStore.getState();
    expect(state.currentPhase).toBeNull();
    expect(state.actionPoints).toBeNull();
    expect(state.declaredAttacks).toEqual([]);
    expect(state.activeBattle).toBeNull();
  });
});

describe('delta events', () => {
  beforeEach(() => {
    useGameStore.getState().resetState();
    useGameStore.getState().loadSnapshot(createMockSnapshot(), 'user-1');
  });

  it('handleTurnAdvanced updates turn state with v6.0 fields', () => {
    const event: TurnAdvancedEvent = {
      nextKingdomId: 'k-2',
      roundNumber: 4,
      currentPhase: 'Action',
      actionPoints: 3,
      turnDeadline: null,
      incomeApplied: { Gold: 20 },
      battleResults: null,
      phaseChanged: false,
      gameOver: null,
    };
    useGameStore.getState().handleTurnAdvanced(event);

    const state = useGameStore.getState();
    expect(state.roundNumber).toBe(4);
    expect(state.currentTurnKingdomId).toBe('k-2');
    expect(state.currentPhase).toBe('Action');
    expect(state.actionPoints).toBe(3);
  });

  it('handleTurnAdvanced applies income to kingdom whose turn just ended', () => {
    const event: TurnAdvancedEvent = {
      nextKingdomId: 'k-2',
      roundNumber: 4,
      currentPhase: 'Action',
      actionPoints: 3,
      turnDeadline: null,
      incomeApplied: { Gold: 20, Food: 10 },
      battleResults: null,
      phaseChanged: false,
      gameOver: null,
    };
    useGameStore.getState().handleTurnAdvanced(event);

    const kingdom = useGameStore.getState().kingdoms.get('k-1')!;
    expect(kingdom.resources.Gold).toBe(120); // 100 + 20
    expect(kingdom.resources.Food).toBe(60); // 50 + 10
  });

  it('handleTurnAdvanced clears declaredAttacks on new turn', () => {
    // First add a declared attack
    useGameStore.getState().handleAttackDeclared({
      attackId: 'atk-1',
      targetTileId: 'tile-2',
      riskedTileId: 'tile-1',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
    });
    expect(useGameStore.getState().declaredAttacks.length).toBe(1);

    const event: TurnAdvancedEvent = {
      nextKingdomId: 'k-2',
      roundNumber: 4,
      currentPhase: 'Action',
      actionPoints: 3,
      turnDeadline: null,
      incomeApplied: null,
      battleResults: null,
      phaseChanged: false,
      gameOver: null,
    };
    useGameStore.getState().handleTurnAdvanced(event);
    expect(useGameStore.getState().declaredAttacks).toEqual([]);
  });

  it('handleTurnAdvanced sets gameOver when present', () => {
    const gameOver: GameOverEvent = {
      gameId: 'game-1',
      winnerKingdomId: 'k-1',
      winConditionType: 'Elimination',
      finalStandings: [],
      eliminationOrder: [],
    };
    const event: TurnAdvancedEvent = {
      nextKingdomId: 'k-2',
      roundNumber: 4,
      currentPhase: 'Action',
      actionPoints: 3,
      turnDeadline: null,
      incomeApplied: null,
      battleResults: null,
      phaseChanged: false,
      gameOver,
    };
    useGameStore.getState().handleTurnAdvanced(event);

    expect(useGameStore.getState().gameOver).toEqual(gameOver);
  });

  it('handleBuildingPlaced adds building to tile', () => {
    const event: BuildingPlacedEvent = {
      buildingId: 'b-new',
      tileId: 'tile-2',
      buildingTypeId: 'farm',
      buildingName: 'Farm',
      kingdomId: 'k-2',
      resourcesAfter: { Gold: 60 },
      claimedTileIds: [],
      isUpgrade: false,
    };
    useGameStore.getState().handleBuildingPlaced(event);

    const tile = useGameStore.getState().tiles.get('1,0')!;
    expect(tile.buildings).toHaveLength(1);
    expect(tile.buildings[0].id).toBe('b-new');
    expect(tile.buildings[0].buildingName).toBe('Farm');
  });

  it('handleBuildingPlaced handles upgrade by replacing existing building', () => {
    const event: BuildingPlacedEvent = {
      buildingId: 'b-upgraded',
      tileId: 'tile-1',
      buildingTypeId: 'fortress',
      buildingName: 'Fortress',
      kingdomId: 'k-1',
      resourcesAfter: { Gold: 50, Food: 30 },
      claimedTileIds: [],
      isUpgrade: true,
    };
    useGameStore.getState().handleBuildingPlaced(event);

    const tile = useGameStore.getState().tiles.get('0,0')!;
    // Upgrade replaces; still 1 building
    expect(tile.buildings).toHaveLength(1);
    expect(tile.buildings[0].id).toBe('b-upgraded');
    expect(tile.buildings[0].buildingName).toBe('Fortress');
  });

  it('handleBuildingPlaced claims tiles from claimedTileIds', () => {
    const event: BuildingPlacedEvent = {
      buildingId: 'b-new',
      tileId: 'tile-2',
      buildingTypeId: 'farm',
      buildingName: 'Farm',
      kingdomId: 'k-2',
      resourcesAfter: { Gold: 60 },
      claimedTileIds: ['tile-3'],
      isUpgrade: false,
    };
    useGameStore.getState().handleBuildingPlaced(event);

    const claimedTile = useGameStore.getState().tiles.get('0,1')!;
    expect(claimedTile.kingdomId).toBe('k-2');
  });

  it('handleBuildingPlaced updates kingdom resources', () => {
    const event: BuildingPlacedEvent = {
      buildingId: 'b-new',
      tileId: 'tile-2',
      buildingTypeId: 'farm',
      buildingName: 'Farm',
      kingdomId: 'k-2',
      resourcesAfter: { Gold: 60 },
      claimedTileIds: [],
      isUpgrade: false,
    };
    useGameStore.getState().handleBuildingPlaced(event);

    const kingdom = useGameStore.getState().kingdoms.get('k-2')!;
    expect(kingdom.resources).toEqual({ Gold: 60 });
  });

  it('handleArmyTrained creates new army with HP fields', () => {
    const event: ArmyTrainedEvent = {
      armyId: 'army-new',
      buildingId: 'b-1',
      armyTypeId: 'archer',
      armyTypeName: 'Archer',
      kingdomId: 'k-1',
      currentHP: 80,
      maxHP: 80,
      resourcesAfter: { Gold: 50, Food: 30 },
    };
    useGameStore.getState().handleArmyTrained(event);

    const army = useGameStore.getState().armies.get('army-new');
    expect(army).toBeDefined();
    expect(army!.buildingId).toBe('b-1');
    expect(army!.armyTypeId).toBe('archer');
    expect(army!.currentHP).toBe(80);
    expect(army!.maxHP).toBe(80);
    expect(army!.kingdomId).toBe('k-1');
  });

  it('handleArmyTrained updates kingdom resources', () => {
    const event: ArmyTrainedEvent = {
      armyId: 'army-new',
      buildingId: 'b-1',
      armyTypeId: 'archer',
      armyTypeName: 'Archer',
      kingdomId: 'k-1',
      currentHP: 80,
      maxHP: 80,
      resourcesAfter: { Gold: 50, Food: 30 },
    };
    useGameStore.getState().handleArmyTrained(event);

    const kingdom = useGameStore.getState().kingdoms.get('k-1')!;
    expect(kingdom.resources).toEqual({ Gold: 50, Food: 30 });
  });

  it('handleSlotMachineSpun updates actionPoints and gold', () => {
    // Set initial actionPoints
    useGameStore.getState().handleTurnStarted({
      kingdomId: 'k-1',
      kingdomName: 'Kingdom Alpha',
      actionPoints: 3,
      turnDeadline: null,
      roundNumber: 3,
    });

    const event: SlotMachineSpunEvent = {
      outcome: 7,
      actionPointsAfter: 4,
      goldAfter: 120,
      goldSpent: 10,
    };
    useGameStore.getState().handleSlotMachineSpun(event);

    const state = useGameStore.getState();
    expect(state.actionPoints).toBe(4);
    const kingdom = state.kingdoms.get('k-1')!;
    expect(kingdom.resources.Gold).toBe(120);
  });

  it('handleAttackDeclared appends to declaredAttacks but does not set activeBattle', () => {
    const event: AttackDeclaredEvent = {
      attackId: 'atk-1',
      targetTileId: 'tile-2',
      riskedTileId: 'tile-1',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
    };
    useGameStore.getState().handleAttackDeclared(event);

    const attacks = useGameStore.getState().declaredAttacks;
    expect(attacks).toHaveLength(1);
    expect(attacks[0].attackId).toBe('atk-1');
    expect(attacks[0].targetTileId).toBe('tile-2');
    // activeBattle is only set when Phase changes to Battle
    expect(useGameStore.getState().activeBattle).toBeNull();
  });

  it('handleArmiesSelected advances battle step when both sides confirm', () => {
    // Set up declared attack
    useGameStore.getState().handleAttackDeclared({
      attackId: 'atk-1',
      targetTileId: 'tile-2',
      riskedTileId: 'tile-1',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
    });

    // Attacker confirms
    useGameStore.getState().handleArmiesSelected({
      declaredAttackId: 'atk-1',
      kingdomId: 'k-1',
      armiesSelected: 2,
      maxArmies: 3,
    });
    expect(useGameStore.getState().activeBattle).toBeNull(); // Not yet — defender not ready

    // Defender confirms
    useGameStore.getState().handleArmiesSelected({
      declaredAttackId: 'atk-1',
      kingdomId: 'k-2',
      armiesSelected: 2,
      maxArmies: 3,
    });
    expect(useGameStore.getState().activeBattle).toBe('RevealArmies');
  });

  it('handleLineupSet advances battle step to Resolve when both sides confirm', () => {
    // Set up declared attack
    useGameStore.getState().handleAttackDeclared({
      attackId: 'atk-1',
      targetTileId: 'tile-2',
      riskedTileId: 'tile-1',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
    });

    // Attacker confirms
    useGameStore.getState().handleLineupSet({
      declaredAttackId: 'atk-1',
      kingdomId: 'k-1',
      armiesSelected: 2,
      maxArmies: 3,
    });
    expect(useGameStore.getState().activeBattle).toBeNull(); // Not yet

    // Defender confirms
    useGameStore.getState().handleLineupSet({
      declaredAttackId: 'atk-1',
      kingdomId: 'k-2',
      armiesSelected: 2,
      maxArmies: 3,
    });
    expect(useGameStore.getState().activeBattle).toBe('Resolve');
  });

  it('handleBattleResolved updates tiles and removes destroyed armies', () => {
    // First declare an attack so we can verify it gets removed
    useGameStore.getState().handleAttackDeclared({
      attackId: 'battle-1',
      targetTileId: 'tile-2',
      riskedTileId: 'tile-1',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
    });

    const event: BattleResolvedEvent = {
      battleId: 'battle-1',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
      outcome: 'AttackerWins',
      tileCapturedId: 'tile-2',
      tileCapturedFromKingdomId: 'k-2',
      rounds: [
        {
          roundNumber: 1,
          attackerArmyId: 'army-1',
          defenderArmyId: 'army-def',
          initiativeWinner: 'army-1',
          attackerInitiativeChance: 0.6,
          defenderInitiativeChance: 0.4,
          damageDealt: 50,
          chipDamageDealt: 5,
          attackerArmyHPAfter: 80,
          defenderArmyHPAfter: 0,
          armyDestroyedId: 'army-def',
        },
      ],
    };
    useGameStore.getState().handleBattleResolved(event);

    // Tile captured
    const tile = useGameStore.getState().tiles.get('1,0')!;
    expect(tile.kingdomId).toBe('k-1');

    // Destroyed army removed (army-def was never in our store, but the handler shouldn't crash)
    expect(useGameStore.getState().activeBattle).toBeNull();
    expect(useGameStore.getState().lastBattleResult).toEqual(event);

    // Declared attack removed
    expect(useGameStore.getState().declaredAttacks).toEqual([]);
  });

  it('handleBattleResolved does not capture tile when tileCapturedId is empty GUID', () => {
    const event: BattleResolvedEvent = {
      battleId: 'battle-2',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
      outcome: 'DefenderWins',
      tileCapturedId: '00000000-0000-0000-0000-000000000000',
      tileCapturedFromKingdomId: '00000000-0000-0000-0000-000000000000',
      rounds: [],
    };
    useGameStore.getState().handleBattleResolved(event);

    // Tile-2 should still belong to k-2
    const tile = useGameStore.getState().tiles.get('1,0')!;
    expect(tile.kingdomId).toBe('k-2');
  });

  it('handlePhaseChanged updates currentPhase', () => {
    const event: PhaseChangedEvent = {
      phase: 'Battle',
      previousPhase: 'Action',
      roundNumber: 3,
    };
    useGameStore.getState().handlePhaseChanged(event);

    expect(useGameStore.getState().currentPhase).toBe('Battle');
  });

  it('handleTurnStarted updates currentTurnKingdomId, actionPoints, roundNumber', () => {
    const event: TurnStartedEvent = {
      kingdomId: 'k-2',
      kingdomName: 'Kingdom Beta',
      actionPoints: 5,
      turnDeadline: null,
      roundNumber: 4,
    };
    useGameStore.getState().handleTurnStarted(event);

    const state = useGameStore.getState();
    expect(state.currentTurnKingdomId).toBe('k-2');
    expect(state.actionPoints).toBe(5);
    expect(state.roundNumber).toBe(4);
  });

  it('handleRoundStarted updates roundNumber and clears declaredAttacks', () => {
    // Add a declared attack first
    useGameStore.getState().handleAttackDeclared({
      attackId: 'atk-1',
      targetTileId: 'tile-2',
      riskedTileId: 'tile-1',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
    });

    const event: RoundStartedEvent = { roundNumber: 5 };
    useGameStore.getState().handleRoundStarted(event);

    expect(useGameStore.getState().roundNumber).toBe(5);
    expect(useGameStore.getState().declaredAttacks).toEqual([]);
  });

  it('handleGameOver sets status and gameOver', () => {
    const event: GameOverEvent = {
      gameId: 'game-1',
      winnerKingdomId: 'k-1',
      winConditionType: 'Elimination',
      finalStandings: [
        { kingdomId: 'k-1', kingdomName: 'Kingdom Alpha', tilesOwned: 5, status: 'Active' },
      ],
      eliminationOrder: ['k-2'],
    };
    useGameStore.getState().handleGameOver(event);

    expect(useGameStore.getState().status).toBe('Completed');
    expect(useGameStore.getState().gameOver).toEqual(event);
    expect(useGameStore.getState().lastBattleResult).toBeNull();
  });

  it('dismissBattleResult clears lastBattleResult', () => {
    // Set it via handleBattleResolved
    const event: BattleResolvedEvent = {
      battleId: 'battle-3',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
      outcome: 'AttackerWins',
      tileCapturedId: '00000000-0000-0000-0000-000000000000',
      tileCapturedFromKingdomId: '00000000-0000-0000-0000-000000000000',
      rounds: [],
    };
    useGameStore.getState().handleBattleResolved(event);
    expect(useGameStore.getState().lastBattleResult).not.toBeNull();

    useGameStore.getState().dismissBattleResult();
    expect(useGameStore.getState().lastBattleResult).toBeNull();
  });
});

describe('resetState', () => {
  beforeEach(() => {
    useGameStore.getState().resetState();
  });

  it('clears all v6.0 fields', () => {
    useGameStore.getState().loadSnapshot(createMockSnapshot(), 'user-1');
    useGameStore.getState().resetState();

    const state = useGameStore.getState();
    expect(state.tiles.size).toBe(0);
    expect(state.tileIdToCoord.size).toBe(0);
    expect(state.kingdoms.size).toBe(0);
    expect(state.armies.size).toBe(0);
    expect(state.myKingdomId).toBeNull();
    expect(state.gameId).toBeNull();
    expect(state.status).toBe('Lobby');
    expect(state.roundNumber).toBe(0);
    expect(state.currentTurnKingdomId).toBeNull();
    expect(state.winCondition).toBeNull();
    expect(state.mapWidth).toBe(0);
    expect(state.mapHeight).toBe(0);
    expect(state.gameOver).toBeNull();
    expect(state.currentPhase).toBeNull();
    expect(state.actionPoints).toBeNull();
    expect(state.declaredAttacks).toEqual([]);
    expect(state.activeBattle).toBeNull();
    expect(state.lastBattleResult).toBeNull();
    expect(state.armyTypes).toEqual([]);
  });
});
