import { describe, it, expect, beforeEach } from 'vitest';
import { useGameStore } from './game-store';
import type {
  GameStateSnapshot,
  TurnAdvancedEvent,
  BuildingPlacedEvent,
  TroopsTrainedEvent,
  ArmyMovedEvent,
  CombatResolvedEvent,
  GameOverEvent,
} from './types';

function createMockSnapshot(): GameStateSnapshot {
  return {
    gameId: 'game-1',
    status: 'InProgress',
    turnNumber: 3,
    winCondition: 'Domination',
    mapRadius: 5,
    currentTurnKingdomId: 'k-1',
    tiles: [
      {
        id: 'tile-1',
        coordQ: 0,
        coordR: 0,
        terrainTypeId: 'plains',
        terrainName: 'Plains',
        kingdomId: 'k-1',
        isCapital: true,
        buildings: [{ id: 'b-1', buildingTypeId: 'castle', buildingName: 'Castle' }],
      },
      {
        id: 'tile-2',
        coordQ: 1,
        coordR: 0,
        terrainTypeId: 'forest',
        terrainName: 'Forest',
        kingdomId: 'k-2',
        isCapital: false,
        buildings: [],
      },
      {
        id: 'tile-3',
        coordQ: 0,
        coordR: 1,
        terrainTypeId: 'mountain',
        terrainName: 'Mountain',
        kingdomId: null,
        isCapital: false,
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
        isEliminated: false,
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
        isEliminated: false,
        resources: [{ resourceType: 'Gold', amount: 80 }],
      },
    ],
    armies: [
      {
        id: 'army-1',
        tileId: 'tile-1',
        kingdomId: 'k-1',
        units: [{ unitTypeId: 'warrior', unitTypeName: 'Warrior', quantity: 10 }],
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
    expect(tile!.isCapital).toBe(true);
    expect(useGameStore.getState().tiles.size).toBe(3);
  });

  it('populates tileIdToCoord reverse map', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    expect(useGameStore.getState().tileIdToCoord.get('tile-1')).toBe('0,0');
    expect(useGameStore.getState().tileIdToCoord.get('tile-2')).toBe('1,0');
    expect(useGameStore.getState().tileIdToCoord.get('tile-3')).toBe('0,1');
  });

  it('populates kingdoms map', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const kingdom = useGameStore.getState().kingdoms.get('k-1');
    expect(kingdom).toBeDefined();
    expect(kingdom!.name).toBe('Kingdom Alpha');
    expect(useGameStore.getState().kingdoms.size).toBe(2);
  });

  it('converts kingdom resources array to record', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const kingdom = useGameStore.getState().kingdoms.get('k-1')!;
    expect(kingdom.resources).toEqual({ Gold: 100, Food: 50 });
  });

  it('populates armies map', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const army = useGameStore.getState().armies.get('army-1');
    expect(army).toBeDefined();
    expect(army!.units).toHaveLength(1);
    expect(army!.units[0].unitTypeName).toBe('Warrior');
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

  it('sets game metadata', () => {
    const snapshot = createMockSnapshot();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const state = useGameStore.getState();
    expect(state.gameId).toBe('game-1');
    expect(state.status).toBe('InProgress');
    expect(state.turnNumber).toBe(3);
    expect(state.currentTurnKingdomId).toBe('k-1');
    expect(state.winCondition).toBe('Domination');
    expect(state.mapRadius).toBe(5);
  });
});

describe('delta events', () => {
  beforeEach(() => {
    useGameStore.getState().resetState();
    useGameStore.getState().loadSnapshot(createMockSnapshot(), 'user-1');
  });

  it('handleTurnAdvanced updates turn state', () => {
    const event: TurnAdvancedEvent = {
      newKingdomId: 'k-2',
      turnNumber: 4,
      incomeApplied: { Gold: 20 },
      gameOver: null,
    };
    useGameStore.getState().handleTurnAdvanced(event);

    expect(useGameStore.getState().turnNumber).toBe(4);
    expect(useGameStore.getState().currentTurnKingdomId).toBe('k-2');
  });

  it('handleTurnAdvanced applies income to kingdom resources', () => {
    // currentTurnKingdomId is k-1 before advance, so income applies to k-1
    const event: TurnAdvancedEvent = {
      newKingdomId: 'k-2',
      turnNumber: 4,
      incomeApplied: { Gold: 20, Food: 10 },
      gameOver: null,
    };
    useGameStore.getState().handleTurnAdvanced(event);

    const kingdom = useGameStore.getState().kingdoms.get('k-1')!;
    expect(kingdom.resources.Gold).toBe(120); // 100 + 20
    expect(kingdom.resources.Food).toBe(60); // 50 + 10
  });

  it('handleTurnAdvanced sets gameOver when present', () => {
    const gameOver: GameOverEvent = {
      gameId: 'game-1',
      winnerKingdomId: 'k-1',
      winConditionType: 'Domination',
      finalStandings: [],
      eliminationOrder: [],
    };
    const event: TurnAdvancedEvent = {
      newKingdomId: 'k-2',
      turnNumber: 4,
      incomeApplied: {},
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
    };
    useGameStore.getState().handleBuildingPlaced(event);

    const tile = useGameStore.getState().tiles.get('1,0')!;
    expect(tile.buildings).toHaveLength(1);
    expect(tile.buildings[0].id).toBe('b-new');
    expect(tile.buildings[0].buildingName).toBe('Farm');
  });

  it('handleBuildingPlaced updates kingdom resources', () => {
    const event: BuildingPlacedEvent = {
      buildingId: 'b-new',
      tileId: 'tile-2',
      buildingTypeId: 'farm',
      buildingName: 'Farm',
      kingdomId: 'k-2',
      resourcesAfter: { Gold: 60 },
    };
    useGameStore.getState().handleBuildingPlaced(event);

    const kingdom = useGameStore.getState().kingdoms.get('k-2')!;
    expect(kingdom.resources).toEqual({ Gold: 60 });
  });

  it('handleTroopsTrained updates existing unit quantity', () => {
    const event: TroopsTrainedEvent = {
      armyId: 'army-1',
      tileId: 'tile-1',
      unitTypeId: 'warrior',
      unitTypeName: 'Warrior',
      quantityTrained: 5,
      totalQuantity: 15,
      kingdomId: 'k-1',
      resourcesAfter: { Gold: 50, Food: 30 },
    };
    useGameStore.getState().handleTroopsTrained(event);

    const army = useGameStore.getState().armies.get('army-1')!;
    const unit = army.units.find((u) => u.unitTypeId === 'warrior')!;
    expect(unit.quantity).toBe(15);
  });

  it('handleTroopsTrained creates new army when armyId not in store', () => {
    const event: TroopsTrainedEvent = {
      armyId: 'army-new',
      tileId: 'tile-2',
      unitTypeId: 'archer',
      unitTypeName: 'Archer',
      quantityTrained: 5,
      totalQuantity: 5,
      kingdomId: 'k-2',
      resourcesAfter: { Gold: 50 },
    };
    useGameStore.getState().handleTroopsTrained(event);

    const army = useGameStore.getState().armies.get('army-new');
    expect(army).toBeDefined();
    expect(army!.units).toHaveLength(1);
    expect(army!.units[0].unitTypeName).toBe('Archer');
    expect(army!.units[0].quantity).toBe(5);
  });

  it('handleArmyMoved updates army tileId', () => {
    const event: ArmyMovedEvent = {
      armyId: 'army-1',
      fromTileId: 'tile-1',
      toTileId: 'tile-3',
      kingdomId: 'k-1',
      tileClaimed: false,
      armyMerged: false,
      mergedIntoArmyId: null,
    };
    useGameStore.getState().handleArmyMoved(event);

    expect(useGameStore.getState().armies.get('army-1')!.tileId).toBe('tile-3');
  });

  it('handleArmyMoved claims tile when tileClaimed', () => {
    const event: ArmyMovedEvent = {
      armyId: 'army-1',
      fromTileId: 'tile-1',
      toTileId: 'tile-3',
      kingdomId: 'k-1',
      tileClaimed: true,
      armyMerged: false,
      mergedIntoArmyId: null,
    };
    useGameStore.getState().handleArmyMoved(event);

    const tile = useGameStore.getState().tiles.get('0,1')!;
    expect(tile.kingdomId).toBe('k-1');
  });

  it('handleArmyMoved deletes absorbed army when merged', () => {
    // Add a second army to merge into
    const snapshot = createMockSnapshot();
    snapshot.armies.push({
      id: 'army-2',
      tileId: 'tile-3',
      kingdomId: 'k-1',
      units: [{ unitTypeId: 'warrior', unitTypeName: 'Warrior', quantity: 5 }],
    });
    useGameStore.getState().resetState();
    useGameStore.getState().loadSnapshot(snapshot, 'user-1');

    const event: ArmyMovedEvent = {
      armyId: 'army-1',
      fromTileId: 'tile-1',
      toTileId: 'tile-3',
      kingdomId: 'k-1',
      tileClaimed: false,
      armyMerged: true,
      mergedIntoArmyId: 'army-2',
    };
    useGameStore.getState().handleArmyMoved(event);

    expect(useGameStore.getState().armies.has('army-1')).toBe(false);
    expect(useGameStore.getState().armies.has('army-2')).toBe(true);
  });

  it('handleCombatResolved captures tile', () => {
    const event: CombatResolvedEvent = {
      battleId: 'battle-1',
      tileId: 'tile-2',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
      winnerKingdomId: 'k-1',
      tileCaptured: true,
      attackerStrength: 100,
      defenderStrength: 50,
      attackerCasualties: [],
      defenderCasualties: [],
      gameOver: null,
    };
    useGameStore.getState().handleCombatResolved(event);

    const tile = useGameStore.getState().tiles.get('1,0')!;
    expect(tile.kingdomId).toBe('k-1');
  });

  it('handleCombatResolved sets gameOver when present', () => {
    const gameOver: GameOverEvent = {
      gameId: 'game-1',
      winnerKingdomId: 'k-1',
      winConditionType: 'Domination',
      finalStandings: [],
      eliminationOrder: [],
    };
    const event: CombatResolvedEvent = {
      battleId: 'battle-1',
      tileId: 'tile-2',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
      winnerKingdomId: 'k-1',
      tileCaptured: true,
      attackerStrength: 100,
      defenderStrength: 50,
      attackerCasualties: [],
      defenderCasualties: [],
      gameOver,
    };
    useGameStore.getState().handleCombatResolved(event);

    expect(useGameStore.getState().gameOver).toEqual(gameOver);
  });

  it('handleCombatResolved sets lastCombatResult when myKingdomId is attacker', () => {
    const event: CombatResolvedEvent = {
      battleId: 'battle-1',
      tileId: 'tile-2',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
      winnerKingdomId: 'k-1',
      tileCaptured: false,
      attackerStrength: 100,
      defenderStrength: 50,
      attackerCasualties: [],
      defenderCasualties: [],
      gameOver: null,
    };
    useGameStore.getState().handleCombatResolved(event);

    expect(useGameStore.getState().lastCombatResult).toEqual(event);
  });

  it('handleCombatResolved sets lastCombatResult when myKingdomId is defender', () => {
    // Reload snapshot as user-2 (myKingdomId='k-2')
    useGameStore.getState().resetState();
    useGameStore.getState().loadSnapshot(createMockSnapshot(), 'user-2');

    const event: CombatResolvedEvent = {
      battleId: 'battle-2',
      tileId: 'tile-2',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
      winnerKingdomId: 'k-1',
      tileCaptured: false,
      attackerStrength: 100,
      defenderStrength: 50,
      attackerCasualties: [],
      defenderCasualties: [],
      gameOver: null,
    };
    useGameStore.getState().handleCombatResolved(event);

    expect(useGameStore.getState().lastCombatResult).toEqual(event);
  });

  it('handleCombatResolved does NOT set lastCombatResult for uninvolved player', () => {
    const event: CombatResolvedEvent = {
      battleId: 'battle-3',
      tileId: 'tile-2',
      attackerKingdomId: 'k-3',
      defenderKingdomId: 'k-4',
      winnerKingdomId: 'k-3',
      tileCaptured: false,
      attackerStrength: 100,
      defenderStrength: 50,
      attackerCasualties: [],
      defenderCasualties: [],
      gameOver: null,
    };
    useGameStore.getState().handleCombatResolved(event);

    expect(useGameStore.getState().lastCombatResult).toBeNull();
  });

  it('dismissCombatResult clears lastCombatResult', () => {
    // First set it via handleCombatResolved
    const event: CombatResolvedEvent = {
      battleId: 'battle-4',
      tileId: 'tile-2',
      attackerKingdomId: 'k-1',
      defenderKingdomId: 'k-2',
      winnerKingdomId: 'k-1',
      tileCaptured: false,
      attackerStrength: 100,
      defenderStrength: 50,
      attackerCasualties: [],
      defenderCasualties: [],
      gameOver: null,
    };
    useGameStore.getState().handleCombatResolved(event);
    expect(useGameStore.getState().lastCombatResult).not.toBeNull();

    useGameStore.getState().dismissCombatResult();
    expect(useGameStore.getState().lastCombatResult).toBeNull();
  });

  it('handleGameOver sets status and gameOver', () => {
    const event: GameOverEvent = {
      gameId: 'game-1',
      winnerKingdomId: 'k-1',
      winConditionType: 'Domination',
      finalStandings: [
        { kingdomId: 'k-1', kingdomName: 'Kingdom Alpha', tilesOwned: 5, status: 'Active' },
      ],
      eliminationOrder: ['k-2'],
    };
    useGameStore.getState().handleGameOver(event);

    expect(useGameStore.getState().status).toBe('Completed');
    expect(useGameStore.getState().gameOver).toEqual(event);
  });
});

describe('resetState', () => {
  beforeEach(() => {
    useGameStore.getState().resetState();
  });

  it('clears all state', () => {
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
    expect(state.turnNumber).toBe(0);
    expect(state.currentTurnKingdomId).toBeNull();
    expect(state.winCondition).toBeNull();
    expect(state.mapRadius).toBe(0);
    expect(state.gameOver).toBeNull();
    expect(state.unitTypes).toEqual([]);
    expect(state.lastCombatResult).toBeNull();
  });
});
