import { describe, it, expect } from 'vitest';
import { calculateKingdomScore } from './score';
import type { Tile } from './types/map-types';
import type { Army } from './types/military-types';
import type { BuildingTypeRef } from './types/building-types';

function makeTile(overrides: Partial<Tile> & { id: string }): Tile {
  return {
    coordQ: 0,
    coordR: 0,
    terrainTypeId: 't1',
    terrainName: 'Plains',
    kingdomId: null,
    isCapital: false,
    buildings: [],
    ...overrides,
  };
}

function makeArmy(overrides: Partial<Army> & { id: string; kingdomId: string }): Army {
  return {
    tileId: 'tile-1',
    units: [],
    ...overrides,
  };
}

function makeBuildingType(id: string, tier: number): BuildingTypeRef {
  return {
    id,
    name: `Building ${id}`,
    tier,
    chain: 'default',
    goldCost: 0,
    woodCost: 0,
    stoneCost: 0,
    manaCost: 0,
    foodYield: 0,
    woodYield: 0,
    stoneYield: 0,
    goldYield: 0,
    manaYield: 0,
    description: null,
    prerequisiteBuildingTypeId: null,
    prerequisiteBuildingName: null,
  };
}

describe('calculateKingdomScore', () => {
  const kingdomId = 'k1';

  it('returns 0 for a kingdom with no tiles and no armies', () => {
    const tiles = new Map<string, Tile>();
    const armies = new Map<string, Army>();
    expect(calculateKingdomScore(kingdomId, tiles, armies, [])).toBe(0);
  });

  it('scores 1 per owned tile (3 tiles = 3)', () => {
    const tiles = new Map<string, Tile>([
      ['0,0', makeTile({ id: 't1', kingdomId, coordQ: 0, coordR: 0 })],
      ['1,0', makeTile({ id: 't2', kingdomId, coordQ: 1, coordR: 0 })],
      ['0,1', makeTile({ id: 't3', kingdomId, coordQ: 0, coordR: 1 })],
    ]);
    const armies = new Map<string, Army>();
    expect(calculateKingdomScore(kingdomId, tiles, armies, [])).toBe(3);
  });

  it('scores tile + building tier * 2 (1 tile + 1 tier-2 building = 5)', () => {
    const tiles = new Map<string, Tile>([
      [
        '0,0',
        makeTile({
          id: 't1',
          kingdomId,
          buildings: [{ id: 'b1', buildingTypeId: 'bt1', buildingName: 'Forge' }],
        }),
      ],
    ]);
    const armies = new Map<string, Army>();
    const types = [makeBuildingType('bt1', 2)];
    expect(calculateKingdomScore(kingdomId, tiles, armies, types)).toBe(5);
  });

  it('scores tile + army unit quantities (1 tile + 5 units = 6)', () => {
    const tiles = new Map<string, Tile>([
      ['0,0', makeTile({ id: 't1', kingdomId })],
    ]);
    const armies = new Map<string, Army>([
      ['a1', makeArmy({ id: 'a1', kingdomId, units: [{ unitTypeId: 'u1', unitTypeName: 'Soldier', quantity: 5 }] })],
    ]);
    expect(calculateKingdomScore(kingdomId, tiles, armies, [])).toBe(6);
  });

  it('computes full combo: 3 tiles + 1 tier-2 building + 5 units = 12', () => {
    const tiles = new Map<string, Tile>([
      [
        '0,0',
        makeTile({
          id: 't1',
          kingdomId,
          buildings: [{ id: 'b1', buildingTypeId: 'bt1', buildingName: 'Forge' }],
        }),
      ],
      ['1,0', makeTile({ id: 't2', kingdomId, coordQ: 1, coordR: 0 })],
      ['0,1', makeTile({ id: 't3', kingdomId, coordQ: 0, coordR: 1 })],
    ]);
    const armies = new Map<string, Army>([
      ['a1', makeArmy({ id: 'a1', kingdomId, units: [{ unitTypeId: 'u1', unitTypeName: 'Soldier', quantity: 5 }] })],
    ]);
    const types = [makeBuildingType('bt1', 2)];
    expect(calculateKingdomScore(kingdomId, tiles, armies, types)).toBe(12);
  });

  it('defaults building tier to 1 when buildingTypes lookup misses', () => {
    const tiles = new Map<string, Tile>([
      [
        '0,0',
        makeTile({
          id: 't1',
          kingdomId,
          buildings: [{ id: 'b1', buildingTypeId: 'unknown', buildingName: 'Mystery' }],
        }),
      ],
    ]);
    const armies = new Map<string, Army>();
    // tile(1) + building(1*2) = 3
    expect(calculateKingdomScore(kingdomId, tiles, armies, [])).toBe(3);
  });

  it('ignores tiles and armies belonging to other kingdoms', () => {
    const tiles = new Map<string, Tile>([
      ['0,0', makeTile({ id: 't1', kingdomId })],
      ['1,0', makeTile({ id: 't2', kingdomId: 'other-kingdom', coordQ: 1, coordR: 0 })],
    ]);
    const armies = new Map<string, Army>([
      ['a1', makeArmy({ id: 'a1', kingdomId, units: [{ unitTypeId: 'u1', unitTypeName: 'Soldier', quantity: 3 }] })],
      ['a2', makeArmy({ id: 'a2', kingdomId: 'other-kingdom', units: [{ unitTypeId: 'u1', unitTypeName: 'Soldier', quantity: 10 }] })],
    ]);
    // Only k1: 1 tile + 3 units = 4
    expect(calculateKingdomScore(kingdomId, tiles, armies, [])).toBe(4);
  });
});
