/**
 * Procedural terrain pattern generation for canvas hex map.
 *
 * Each terrain type gets a unique subtle pattern drawn on an offscreen canvas,
 * which can then be used as a CanvasPattern for repeating fills.
 * Colors are aligned with backend TerrainType.MapColor values.
 */

/** Backend-aligned terrain base colors — muted so tiles stay in the background */
export const TERRAIN_BASE_COLORS: Record<string, string> = {
  Plains: '#5a7a4a',
  Forest: '#2a5a2a',
  Mountain: '#4a4a50',
  Desert: '#7a6e50',
  'Magic Grove': '#5a3a6a',
};

const FALLBACK_COLOR = '#333333';

// ---------------------------------------------------------------------------
// Deterministic pseudo-random number generator (splitmix32)
// ---------------------------------------------------------------------------

function splitmix32(seed: number): () => number {
  return () => {
    seed |= 0;
    seed = (seed + 0x9e3779b9) | 0;
    let t = seed ^ (seed >>> 16);
    t = Math.imul(t, 0x21f0aaad);
    t = t ^ (t >>> 15);
    t = Math.imul(t, 0x735a2d97);
    t = t ^ (t >>> 15);
    return (t >>> 0) / 4294967296;
  };
}

// ---------------------------------------------------------------------------
// Per-terrain overlay painters — organic, noise-driven patterns
// ---------------------------------------------------------------------------

function drawPlainsOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Scattered short grass strokes at random angles
  const rng = splitmix32(17);
  const count = Math.floor(size * size * 0.04);
  ctx.strokeStyle = 'rgba(255,255,255,0.08)';
  ctx.lineWidth = 1;
  for (let i = 0; i < count; i++) {
    const x = rng() * size;
    const y = rng() * size;
    const angle = rng() * Math.PI;
    const len = 2 + rng() * 3;
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.lineTo(x + Math.cos(angle) * len, y + Math.sin(angle) * len);
    ctx.stroke();
  }
}

function drawForestOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Randomly placed small blobs of varying darkness
  const rng = splitmix32(42);
  const count = Math.floor(size * size * 0.025);
  for (let i = 0; i < count; i++) {
    const x = rng() * size;
    const y = rng() * size;
    const r = 1 + rng() * 2.5;
    const alpha = 0.06 + rng() * 0.1;
    ctx.fillStyle = `rgba(0,0,0,${alpha})`;
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fill();
  }
}

function drawMountainOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Scattered small cracks / short jagged lines
  const rng = splitmix32(73);
  const count = Math.floor(size * size * 0.02);
  ctx.lineWidth = 1;
  for (let i = 0; i < count; i++) {
    const x = rng() * size;
    const y = rng() * size;
    const alpha = 0.08 + rng() * 0.1;
    ctx.strokeStyle = `rgba(0,0,0,${alpha})`;
    ctx.beginPath();
    ctx.moveTo(x, y);
    // 2-segment jagged line
    const mx = x + (rng() - 0.5) * 4;
    const my = y + (rng() - 0.5) * 4;
    ctx.lineTo(mx, my);
    ctx.lineTo(mx + (rng() - 0.5) * 4, my + (rng() - 0.5) * 4);
    ctx.stroke();
  }
}

function drawDesertOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Scattered tiny dots of varying size — like sand grains
  const rng = splitmix32(101);
  const count = Math.floor(size * size * 0.03);
  for (let i = 0; i < count; i++) {
    const x = rng() * size;
    const y = rng() * size;
    const r = 0.5 + rng() * 1.5;
    const alpha = 0.05 + rng() * 0.08;
    ctx.fillStyle = `rgba(0,0,0,${alpha})`;
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fill();
  }
}

function drawMagicGroveOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Faint glowing specks at random positions
  const rng = splitmix32(137);
  const count = Math.floor(size * size * 0.015);
  for (let i = 0; i < count; i++) {
    const x = rng() * size;
    const y = rng() * size;
    const r = 0.8 + rng() * 2;
    const alpha = 0.05 + rng() * 0.1;
    ctx.fillStyle = `rgba(255,255,255,${alpha})`;
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fill();
  }
}

const OVERLAY_PAINTERS: Record<string, (ctx: CanvasRenderingContext2D, size: number) => void> = {
  Plains: drawPlainsOverlay,
  Forest: drawForestOverlay,
  Mountain: drawMountainOverlay,
  Desert: drawDesertOverlay,
  'Magic Grove': drawMagicGroveOverlay,
};

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/**
 * Create an offscreen canvas with a procedural terrain pattern tile.
 *
 * The canvas is filled with the terrain's base color, then a subtle overlay
 * pattern is drawn on top to visually distinguish terrain types without
 * overwhelming building icons or other map elements.
 *
 * @param terrainName - Backend terrain name (e.g. 'Plains', 'Forest')
 * @param tileSize - Square dimension of the pattern tile in pixels
 * @returns An HTMLCanvasElement ready to be used with ctx.createPattern()
 */
export function createTerrainPatternCanvas(terrainName: string, tileSize: number): HTMLCanvasElement {
  const canvas = document.createElement('canvas');
  canvas.width = tileSize;
  canvas.height = tileSize;
  const ctx = canvas.getContext('2d');
  if (!ctx) return canvas;

  // Base fill
  const baseColor = TERRAIN_BASE_COLORS[terrainName] ?? FALLBACK_COLOR;
  ctx.fillStyle = baseColor;
  ctx.fillRect(0, 0, tileSize, tileSize);

  // Terrain-specific overlay
  const painter = OVERLAY_PAINTERS[terrainName];
  if (painter) {
    painter(ctx, tileSize);
  }

  return canvas;
}
