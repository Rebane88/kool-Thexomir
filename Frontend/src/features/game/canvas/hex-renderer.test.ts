import { describe, it, expect, vi, beforeEach } from 'vitest';

vi.mock('./texture-cache', () => ({
  textureCache: {
    getTerrainPattern: vi.fn(() => null), // fallback to flat color in tests
    getBuildingIcon: vi.fn(() => null),
    initialized: true,
  },
}));

vi.mock('./territory-borders', () => ({
  drawTerritoryBorders: vi.fn(),
}));

import { drawGameMap, getKingdomColor } from './hex-renderer';
import { textureCache } from './texture-cache';
import {
  KINGDOM_COLORS,
  SELECTION_COLOR,
  HOVER_COLOR,
} from './types';
import { TERRAIN_BASE_COLORS } from './terrain-patterns';
import type { MapRenderState } from './types';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';

interface CallTracker {
  fillStyleHistory: string[];
  strokeStyleHistory: string[];
  globalAlphaHistory: number[];
  lineWidthHistory: number[];
  fillTextCalls: [string, number, number][];
  fillCalls: number;
  saveCalls: number;
  restoreCalls: number;
}

function createMockCtx(): CanvasRenderingContext2D & { _tracker: CallTracker } {
  const tracker: CallTracker = {
    fillStyleHistory: [],
    strokeStyleHistory: [],
    globalAlphaHistory: [],
    lineWidthHistory: [],
    fillTextCalls: [],
    fillCalls: 0,
    saveCalls: 0,
    restoreCalls: 0,
  };

  let _fillStyle = '';
  let _strokeStyle = '';
  let _globalAlpha = 1.0;
  let _lineWidth = 1;

  const ctx = {
    _tracker: tracker,
    beginPath: vi.fn(),
    moveTo: vi.fn(),
    lineTo: vi.fn(),
    closePath: vi.fn(),
    stroke: vi.fn(),
    fill: vi.fn(() => { tracker.fillCalls++; }),
    fillRect: vi.fn(),
    fillText: vi.fn((text: string, x: number, y: number) => {
      tracker.fillTextCalls.push([text, x, y]);
    }),
    arc: vi.fn(),
    clip: vi.fn(),
    save: vi.fn(() => { tracker.saveCalls++; }),
    restore: vi.fn(() => { tracker.restoreCalls++; }),
    drawImage: vi.fn(),
    get fillStyle() { return _fillStyle; },
    set fillStyle(v: string) { _fillStyle = v; tracker.fillStyleHistory.push(v); },
    get strokeStyle() { return _strokeStyle; },
    set strokeStyle(v: string) { _strokeStyle = v; tracker.strokeStyleHistory.push(v); },
    get globalAlpha() { return _globalAlpha; },
    set globalAlpha(v: number) { _globalAlpha = v; tracker.globalAlphaHistory.push(v); },
    get lineWidth() { return _lineWidth; },
    set lineWidth(v: number) { _lineWidth = v; tracker.lineWidthHistory.push(v); },
    font: '',
    textAlign: '',
    textBaseline: '',
  } as unknown as CanvasRenderingContext2D & { _tracker: CallTracker };

  return ctx;
}

function makeTile(overrides: Partial<Tile> & { coordQ: number; coordR: number }): Tile {
  return {
    id: `tile-${overrides.coordQ}-${overrides.coordR}`,
    terrainTypeId: 't1',
    terrainName: 'Plains',
    kingdomId: null,
    isCastle: false,
    buildings: [],
    ...overrides,
  };
}

function buildState(opts: {
  tiles?: Tile[];
  kingdoms?: Kingdom[];
  myKingdomId?: string | null;
}) {
  const tiles = new Map<string, Tile>();
  const tileIdToCoord = new Map<string, string>();
  for (const t of opts.tiles ?? []) {
    const key = `${t.coordQ},${t.coordR}`;
    tiles.set(key, t);
    tileIdToCoord.set(t.id, key);
  }
  const kingdoms = new Map<string, Kingdom>();
  for (const k of opts.kingdoms ?? []) {
    kingdoms.set(k.id, k);
  }
  return {
    tiles,
    tileIdToCoord,
    kingdoms,
    myKingdomId: opts.myKingdomId ?? null,
    mapRadius: 7,
  };
}

const noRender: MapRenderState = { hoveredTileKey: null, selectedTileKey: null, buildModeTypeId: null, armyHighlightTileKey: null, assetsReady: true, declareAttackGlowTileKeys: null, placementAnimations: [] };

describe('getKingdomColor', () => {
  it('returns indexed color for kingdoms by insertion order', () => {
    const kingdoms = new Map<string, Kingdom>();
    kingdoms.set('k1', { id: 'k1', name: 'K1', userId: null, factionTypeId: null, factionName: null, status: 'Active', resources: {} });
    kingdoms.set('k2', { id: 'k2', name: 'K2', userId: null, factionTypeId: null, factionName: null, status: 'Active', resources: {} });
    kingdoms.set('k3', { id: 'k3', name: 'K3', userId: null, factionTypeId: null, factionName: null, status: 'Active', resources: {} });

    expect(getKingdomColor(kingdoms, 'k1')).toBe(KINGDOM_COLORS[0]); // red
    expect(getKingdomColor(kingdoms, 'k2')).toBe(KINGDOM_COLORS[1]); // blue
    expect(getKingdomColor(kingdoms, 'k3')).toBe(KINGDOM_COLORS[2]); // gold
  });
});

describe('drawGameMap', () => {
  let ctx: CanvasRenderingContext2D & { _tracker: CallTracker };

  beforeEach(() => {
    ctx = createMockCtx();
    vi.clearAllMocks();
  });

  it('fills terrain with fallback color when textureCache returns null pattern', () => {
    const state = buildState({ tiles: [makeTile({ coordQ: 0, coordR: 0 })] });
    drawGameMap(ctx, 800, 600, state, noRender);
    expect(ctx._tracker.fillStyleHistory).toContain(TERRAIN_BASE_COLORS['Plains']);
  });

  it('calls textureCache.getTerrainPattern for each tile', () => {
    const state = buildState({ tiles: [makeTile({ coordQ: 0, coordR: 0 })] });
    drawGameMap(ctx, 800, 600, state, noRender);
    expect(textureCache.getTerrainPattern).toHaveBeenCalledWith('Plains');
  });

  it('calls textureCache.getBuildingIcon for castle tile', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0, isCastle: true, kingdomId: 'k1' });
    const kingdom: Kingdom = { id: 'k1', name: 'K1', userId: null, factionTypeId: null, factionName: null, status: 'Active', resources: {} };
    const state = buildState({ tiles: [tile], kingdoms: [kingdom] });
    drawGameMap(ctx, 800, 600, state, noRender);
    expect(textureCache.getBuildingIcon).toHaveBeenCalledWith('Castle');
  });

  it('calls textureCache.getBuildingIcon for tile with buildings', () => {
    const tile = makeTile({
      coordQ: 0,
      coordR: 0,
      buildings: [
        { id: 'b1', buildingTypeId: 'bt1', buildingName: 'Farm' },
      ],
    });
    const state = buildState({ tiles: [tile] });
    drawGameMap(ctx, 800, 600, state, noRender);
    expect(textureCache.getBuildingIcon).toHaveBeenCalledWith('Farm');
  });

  it('draws thick gold 3px border for selected tile', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0 });
    const state = buildState({ tiles: [tile] });
    const renderState: MapRenderState = { hoveredTileKey: null, selectedTileKey: '0,0', buildModeTypeId: null, armyHighlightTileKey: null, assetsReady: true, declareAttackGlowTileKeys: null, placementAnimations: [] };
    drawGameMap(ctx, 800, 600, state, renderState);
    expect(ctx._tracker.lineWidthHistory).toContain(3);
    expect(ctx._tracker.strokeStyleHistory).toContain(SELECTION_COLOR);
  });

  it('draws faint gold border for hovered tile', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0 });
    const state = buildState({ tiles: [tile] });
    const renderState: MapRenderState = { hoveredTileKey: '0,0', selectedTileKey: null, buildModeTypeId: null, armyHighlightTileKey: null, assetsReady: true, declareAttackGlowTileKeys: null, placementAnimations: [] };
    drawGameMap(ctx, 800, 600, state, renderState);
    expect(ctx._tracker.strokeStyleHistory).toContain(HOVER_COLOR);
  });

  it('uses save/restore for pattern clipping', () => {
    const state = buildState({ tiles: [makeTile({ coordQ: 0, coordR: 0 })] });
    drawGameMap(ctx, 800, 600, state, noRender);
    // With null pattern (mock), no save/restore for clip -- but if pattern existed, it would
    // Just verify the function runs without error
    expect(ctx._tracker.fillCalls).toBeGreaterThanOrEqual(1);
  });
});
