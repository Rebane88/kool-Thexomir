import { describe, it, expect, vi } from 'vitest';

// Mock hex-renderer to avoid circular dependency issues in test
vi.mock('./hex-renderer', () => ({
  getKingdomColor: vi.fn((_kingdoms: unknown, _id: string) => '#ff0000'),
}));

import { drawTerritoryBorders } from './territory-borders';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';
import type { HexLayoutConfig } from './types';

function makeTile(q: number, r: number, kingdomId: string | null = null): Tile {
  return {
    id: `tile-${q}-${r}`,
    coordQ: q, coordR: r,
    terrainTypeId: 't1', terrainName: 'Plains',
    kingdomId, isCastle: false, buildings: [],
  };
}

const layout: HexLayoutConfig = { size: 30, origin: { x: 400, y: 300 } };

describe('drawTerritoryBorders', () => {
  it('is a callable function', () => {
    expect(typeof drawTerritoryBorders).toBe('function');
  });

  it('draws borders on edges where owned tile meets unowned tile', () => {
    const owned = makeTile(0, 0, 'k1');
    const unowned = makeTile(1, 0, null);
    const tiles = new Map<string, Tile>();
    tiles.set('0,0', owned);
    tiles.set('1,0', unowned);
    const kingdoms = new Map<string, Kingdom>();
    kingdoms.set('k1', { id: 'k1', name: 'K1', userId: null, factionTypeId: 'f1', factionName: null, status: 'Active', resources: {} });

    const ctx = {
      beginPath: vi.fn(), moveTo: vi.fn(), lineTo: vi.fn(),
      stroke: vi.fn(), save: vi.fn(), restore: vi.fn(),
      closePath: vi.fn(),
      fillStyle: '', strokeStyle: '', globalAlpha: 1, lineWidth: 1, lineCap: 'butt',
    } as unknown as CanvasRenderingContext2D;

    drawTerritoryBorders(ctx, tiles, kingdoms, layout, null);

    // Should have stroked at least once for the boundary edge
    expect(ctx.stroke).toHaveBeenCalled();
  });

  it('does not draw borders between two tiles owned by the same kingdom', () => {
    const tile1 = makeTile(0, 0, 'k1');
    const tile2 = makeTile(1, 0, 'k1');
    const tiles = new Map<string, Tile>();
    tiles.set('0,0', tile1);
    tiles.set('1,0', tile2);
    const kingdoms = new Map<string, Kingdom>();
    kingdoms.set('k1', { id: 'k1', name: 'K1', userId: null, factionTypeId: 'f1', factionName: null, status: 'Active', resources: {} });

    const strokeFn = vi.fn();
    const ctx = {
      beginPath: vi.fn(), moveTo: vi.fn(), lineTo: vi.fn(),
      stroke: strokeFn, save: vi.fn(), restore: vi.fn(),
      closePath: vi.fn(),
      fillStyle: '', strokeStyle: '', globalAlpha: 1, lineWidth: 1, lineCap: 'butt',
    } as unknown as CanvasRenderingContext2D;

    drawTerritoryBorders(ctx, tiles, kingdoms, layout, null);

    // The shared edge between tile1 and tile2 should NOT be drawn
    // But exterior edges (where neighbor is absent) SHOULD be drawn
    // With 2 adjacent tiles, each has 6 edges. They share 1 edge.
    // tile1 has 5 exterior edges (1 shared with tile2, 5 missing neighbors)
    // tile2 has 5 exterior edges (1 shared with tile1, 5 missing neighbors)
    // Total boundary edges: 10 (not 12)
    expect(strokeFn).toHaveBeenCalledTimes(10);
  });
});
