import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MilitaryPanel } from './MilitaryPanel';
import { useGameStore } from '../game-store';
import type { ArmyTypeRef } from '../types/military-types';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';

vi.mock('../game-api', () => ({
  trainArmy: vi.fn(() => Promise.resolve()),
}));

function makeArmyType(overrides: Partial<ArmyTypeRef> = {}): ArmyTypeRef {
  return {
    id: 'at-1',
    name: 'Warrior',
    attack: 10,
    hp: 100,
    initiative: 50,
    damageRangeMin: 0.8,
    damageRangeMax: 1.2,
    chipDamageRangeMin: 0.1,
    chipDamageRangeMax: 0.2,
    situationalBonusStat: null,
    situationalBonusValue: null,
    situationalBonusCondition: null,
    trainingCostGold: 50,
    trainingCostFood: 20,
    trainingCostStone: 0,
    trainingCostMana: 0,
    upkeepGold: 5,
    upkeepFood: 3,
    upkeepMana: 0,
    requiredBuildingTypeId: 'barracks-type',
    requiredBuildingName: 'Barracks',
    ...overrides,
  };
}

function makeTile(overrides: Partial<Tile> = {}): Tile {
  return {
    id: 'tile-1',
    coordQ: 0,
    coordR: 0,
    terrainTypeId: 'plains',
    terrainName: 'Plains',
    kingdomId: 'k-1',
    isCastle: false,
    buildings: [{ id: 'building-instance-1', buildingTypeId: 'barracks-type', buildingName: 'Barracks' }],
    ...overrides,
  };
}

function makeKingdom(overrides: Partial<Kingdom> = {}): Kingdom {
  return {
    id: 'k-1',
    name: 'Northern Realm',
    userId: 'u-1',
    factionTypeId: 'f-1',
    factionName: 'Elves',
    status: 'Active',
    resources: { Gold: 200, Food: 100, Wood: 50, Stone: 50, Mana: 50 },
    ...overrides,
  };
}

function setupStore(opts: {
  armyTypes?: ArmyTypeRef[];
  tile?: Tile;
  kingdom?: Kingdom;
  gameId?: string;
}) {
  const tile = opts.tile ?? makeTile();
  const kingdom = opts.kingdom ?? makeKingdom();
  const tiles = new Map<string, Tile>();
  tiles.set('0,0', tile);
  const kingdoms = new Map<string, Kingdom>();
  kingdoms.set(kingdom.id, kingdom);

  useGameStore.setState({
    armyTypes: opts.armyTypes ?? [makeArmyType()],
    myKingdomId: kingdom.id,
    tiles,
    kingdoms,
    gameId: opts.gameId ?? 'game-1',
    actionPoints: 3,
    maxActionPoints: 3,
  });
}

describe('MilitaryPanel', () => {
  beforeEach(() => {
    useGameStore.getState().resetState();
    vi.clearAllMocks();
  });

  it('renders army types trainable at the selected building', () => {
    setupStore({
      armyTypes: [
        makeArmyType({ id: 'at-1', name: 'Warrior' }),
        makeArmyType({ id: 'at-2', name: 'Scout', requiredBuildingTypeId: 'barracks-type' }),
      ],
    });

    render(<MilitaryPanel selectedTileKey="0,0" />);
    expect(screen.getByText('Warrior')).toBeTruthy();
    expect(screen.getByText('Scout')).toBeTruthy();
  });

  it('dims unaffordable unit rows', () => {
    setupStore({
      armyTypes: [makeArmyType({ trainingCostGold: 9999 })],
      kingdom: makeKingdom({ resources: { Gold: 10, Food: 100 } }),
    });

    const { container } = render(<MilitaryPanel selectedTileKey="0,0" />);
    const row = container.querySelector('.opacity-50');
    expect(row).toBeTruthy();
  });

  it('calls trainArmy with correct building.id on train click', async () => {
    const { trainArmy } = await import('../game-api');

    setupStore({});

    render(<MilitaryPanel selectedTileKey="0,0" />);
    const trainButton = screen.getByText('Train 1');
    fireEvent.click(trainButton);

    expect(trainArmy).toHaveBeenCalledWith('game-1', {
      buildingId: 'building-instance-1',
      armyTypeId: 'at-1',
    });
  });

  it("shows 'No units trainable here' when no army types match building", () => {
    setupStore({
      armyTypes: [makeArmyType({ requiredBuildingTypeId: 'other-building-type' })],
    });

    render(<MilitaryPanel selectedTileKey="0,0" />);
    expect(screen.getByText('No units trainable here')).toBeTruthy();
  });
});
