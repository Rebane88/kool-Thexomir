import { describe, it, expect, vi, beforeEach } from 'vitest';
import { drawGameMap, getKingdomColor } from './hex-renderer';
import {
  TERRAIN_COLORS,
  KINGDOM_COLORS,
  KINGDOM_OVERLAY_ALPHA,
  SELECTION_COLOR,
  HOVER_COLOR,
} from './types';
import type { MapRenderState } from './types';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';
import type { Army } from '../types/military-types';

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
    save: vi.fn(() => { tracker.saveCalls++; }),
    restore: vi.fn(() => { tracker.restoreCalls++; }),
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
    isCapital: false,
    buildings: [],
    ...overrides,
  };
}

function buildState(opts: {
  tiles?: Tile[];
  kingdoms?: Kingdom[];
  armies?: Army[];
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
  const armies = new Map<string, Army>();
  for (const a of opts.armies ?? []) {
    armies.set(a.id, a);
  }
  return {
    tiles,
    tileIdToCoord,
    kingdoms,
    armies,
    myKingdomId: opts.myKingdomId ?? null,
    mapRadius: 7,
  };
}

const noRender: MapRenderState = { hoveredTileKey: null, selectedTileKey: null };

describe('getKingdomColor', () => {
  it('returns indexed color for kingdoms by insertion order', () => {
    const kingdoms = new Map<string, Kingdom>();
    kingdoms.set('k1', { id: 'k1', name: 'K1', userId: null, factionTypeId: null, factionName: null, isEliminated: false, resources: {} });
    kingdoms.set('k2', { id: 'k2', name: 'K2', userId: null, factionTypeId: null, factionName: null, isEliminated: false, resources: {} });
    kingdoms.set('k3', { id: 'k3', name: 'K3', userId: null, factionTypeId: null, factionName: null, isEliminated: false, resources: {} });

    expect(getKingdomColor(kingdoms, 'k1')).toBe(KINGDOM_COLORS[0]); // red
    expect(getKingdomColor(kingdoms, 'k2')).toBe(KINGDOM_COLORS[1]); // blue
    expect(getKingdomColor(kingdoms, 'k3')).toBe(KINGDOM_COLORS[2]); // gold
  });
});

describe('drawGameMap', () => {
  let ctx: CanvasRenderingContext2D & { _tracker: CallTracker };

  beforeEach(() => {
    ctx = createMockCtx();
  });

  it('fills terrain with correct color for Plains tile', () => {
    const state = buildState({ tiles: [makeTile({ coordQ: 0, coordR: 0 })] });
    drawGameMap(ctx, 800, 600, state, noRender);
    expect(ctx._tracker.fillStyleHistory).toContain(TERRAIN_COLORS['Plains']);
  });

  it('sets globalAlpha to KINGDOM_OVERLAY_ALPHA for owned tiles', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0, kingdomId: 'k1' });
    const kingdom: Kingdom = { id: 'k1', name: 'K1', userId: null, factionTypeId: null, factionName: null, isEliminated: false, resources: {} };
    const state = buildState({ tiles: [tile], kingdoms: [kingdom] });
    drawGameMap(ctx, 800, 600, state, noRender);
    expect(ctx._tracker.globalAlphaHistory).toContain(KINGDOM_OVERLAY_ALPHA);
  });

  it('calls fill more than once for capital tiles (crown drawing)', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0, isCapital: true, kingdomId: 'k1' });
    const kingdom: Kingdom = { id: 'k1', name: 'K1', userId: null, factionTypeId: null, factionName: null, isEliminated: false, resources: {} };
    const state = buildState({ tiles: [tile], kingdoms: [kingdom] });
    drawGameMap(ctx, 800, 600, state, noRender);
    // At least 2 fill calls: terrain fill + crown fill
    expect(ctx._tracker.fillCalls).toBeGreaterThanOrEqual(2);
  });

  it('draws building count badge for tile with 2 buildings', () => {
    const tile = makeTile({
      coordQ: 0,
      coordR: 0,
      buildings: [
        { id: 'b1', buildingTypeId: 'bt1', buildingName: 'Farm' },
        { id: 'b2', buildingTypeId: 'bt2', buildingName: 'Mine' },
      ],
    });
    const state = buildState({ tiles: [tile] });
    drawGameMap(ctx, 800, 600, state, noRender);
    const buildingTextCall = ctx._tracker.fillTextCalls.find(([text]) => text === '2');
    expect(buildingTextCall).toBeDefined();
  });

  it('draws army unit count badge for tile with army totaling 3 units', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0 });
    const army: Army = {
      id: 'a1',
      tileId: tile.id,
      kingdomId: 'k1',
      units: [
        { unitTypeId: 'u1', unitTypeName: 'Soldier', quantity: 2 },
        { unitTypeId: 'u2', unitTypeName: 'Archer', quantity: 1 },
      ],
    };
    const kingdom: Kingdom = { id: 'k1', name: 'K1', userId: null, factionTypeId: null, factionName: null, isEliminated: false, resources: {} };
    const state = buildState({ tiles: [tile], kingdoms: [kingdom], armies: [army] });
    drawGameMap(ctx, 800, 600, state, noRender);
    const armyTextCall = ctx._tracker.fillTextCalls.find(([text]) => text === '3');
    expect(armyTextCall).toBeDefined();
  });

  it('draws thick gold 3px border for selected tile', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0 });
    const state = buildState({ tiles: [tile] });
    const renderState: MapRenderState = { hoveredTileKey: null, selectedTileKey: '0,0' };
    drawGameMap(ctx, 800, 600, state, renderState);
    expect(ctx._tracker.lineWidthHistory).toContain(3);
    expect(ctx._tracker.strokeStyleHistory).toContain(SELECTION_COLOR);
  });

  it('draws faint gold border for hovered tile', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0 });
    const state = buildState({ tiles: [tile] });
    const renderState: MapRenderState = { hoveredTileKey: '0,0', selectedTileKey: null };
    drawGameMap(ctx, 800, 600, state, renderState);
    expect(ctx._tracker.strokeStyleHistory).toContain(HOVER_COLOR);
  });

  it('draws ember glow border for player own kingdom tiles', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0, kingdomId: 'k1' });
    const kingdom: Kingdom = { id: 'k1', name: 'K1', userId: 'u1', factionTypeId: null, factionName: null, isEliminated: false, resources: {} };
    const state = buildState({ tiles: [tile], kingdoms: [kingdom], myKingdomId: 'k1' });
    drawGameMap(ctx, 800, 600, state, noRender);
    // Ember glow uses SELECTION_COLOR
    expect(ctx._tracker.strokeStyleHistory).toContain(SELECTION_COLOR);
  });

  it('uses save/restore around alpha changes', () => {
    const tile = makeTile({ coordQ: 0, coordR: 0, kingdomId: 'k1' });
    const kingdom: Kingdom = { id: 'k1', name: 'K1', userId: null, factionTypeId: null, factionName: null, isEliminated: false, resources: {} };
    const state = buildState({ tiles: [tile], kingdoms: [kingdom] });
    drawGameMap(ctx, 800, 600, state, noRender);
    expect(ctx._tracker.saveCalls).toBeGreaterThanOrEqual(1);
    expect(ctx._tracker.restoreCalls).toBeGreaterThanOrEqual(1);
  });
});
