import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MilitaryPanel } from './MilitaryPanel';
import { useGameStore } from '../game-store';
import type { UnitTypeRef } from '../types/military-types';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';

vi.mock('../game-api', () => ({
  trainArmy: vi.fn(() => Promise.resolve()),
}));

function makeUnitType(overrides: Partial<UnitTypeRef> = {}): UnitTypeRef {
  return {
    id: 'ut-1',
    name: 'Swordsmen',
    baseStrength: 10,
    goldCost: 50,
    foodCost: 20,
    woodCost: 0,
    stoneCost: 0,
    manaCost: 0,
    upkeep: 5,
    description: null,
    producedByBuildingTypeIds: ['barracks-type'],
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
    isCapital: false,
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
    isEliminated: false,
    resources: { Gold: 200, Food: 100, Wood: 50, Stone: 50, Mana: 50 },
    ...overrides,
  };
}

function setupStore(opts: {
  unitTypes?: UnitTypeRef[];
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
    unitTypes: opts.unitTypes ?? [makeUnitType()],
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

  it('renders unit types trainable at the selected building', () => {
    setupStore({
      unitTypes: [
        makeUnitType({ id: 'ut-1', name: 'Swordsmen' }),
        makeUnitType({ id: 'ut-2', name: 'Archers', producedByBuildingTypeIds: ['barracks-type'] }),
      ],
    });

    render(<MilitaryPanel selectedTileKey="0,0" />);
    expect(screen.getByText('Swordsmen')).toBeTruthy();
    expect(screen.getByText('Archers')).toBeTruthy();
  });

  it('dims unaffordable unit rows', () => {
    setupStore({
      unitTypes: [makeUnitType({ goldCost: 9999 })],
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
      armyTypeId: 'ut-1',
    });
  });

  it("shows 'No units trainable here' when no unit types match building", () => {
    setupStore({
      unitTypes: [makeUnitType({ producedByBuildingTypeIds: ['other-building-type'] })],
    });

    render(<MilitaryPanel selectedTileKey="0,0" />);
    expect(screen.getByText('No units trainable here')).toBeTruthy();
  });
});
