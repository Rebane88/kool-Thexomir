/**
 * Procedural terrain pattern generation for canvas hex map.
 *
 * Each terrain type gets a unique subtle pattern drawn on an offscreen canvas,
 * which can then be used as a CanvasPattern for repeating fills.
 * Colors are aligned with backend TerrainType.MapColor values.
 */

/** Backend-aligned terrain base colors (TerrainType.MapColor) */
export const TERRAIN_BASE_COLORS: Record<string, string> = {
  Plains: '#90EE90',
  Forest: '#228B22',
  Mountain: '#808080',
  Desert: '#C2B280',
  'Magic Grove': '#9B59B6',
};

const FALLBACK_COLOR = '#333333';

// ---------------------------------------------------------------------------
// Per-terrain overlay painters
// ---------------------------------------------------------------------------

function drawPlainsOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Diagonal hatching lines at 45 degrees
  ctx.strokeStyle = 'rgba(255,255,255,0.08)';
  ctx.lineWidth = 1;
  const spacing = 8;
  ctx.beginPath();
  for (let offset = -size; offset < size * 2; offset += spacing) {
    ctx.moveTo(offset, 0);
    ctx.lineTo(offset + size, size);
  }
  ctx.stroke();
}

function drawForestOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Small triangle/tree shapes scattered in a grid
  ctx.fillStyle = 'rgba(0,0,0,0.15)';
  const spacing = 10;
  const triSize = 4;
  for (let y = spacing / 2; y < size; y += spacing) {
    for (let x = spacing / 2; x < size; x += spacing) {
      ctx.beginPath();
      ctx.moveTo(x, y - triSize);
      ctx.lineTo(x - triSize / 2, y + triSize / 2);
      ctx.lineTo(x + triSize / 2, y + triSize / 2);
      ctx.closePath();
      ctx.fill();
    }
  }
}

function drawMountainOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Small inverted-V peak shapes
  ctx.strokeStyle = 'rgba(0,0,0,0.2)';
  ctx.lineWidth = 1;
  const spacing = 8;
  const peakHeight = 5;
  for (let y = spacing; y < size; y += spacing) {
    for (let x = spacing / 2; x < size; x += spacing) {
      ctx.beginPath();
      ctx.moveTo(x - 3, y);
      ctx.lineTo(x, y - peakHeight);
      ctx.lineTo(x + 3, y);
      ctx.stroke();
    }
  }
}

function drawDesertOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Stippled dots in a semi-random but deterministic pattern
  ctx.fillStyle = 'rgba(0,0,0,0.1)';
  const spacing = 6;
  const radius = 1.5;
  for (let y = spacing / 2; y < size; y += spacing) {
    for (let x = spacing / 2; x < size; x += spacing) {
      // Deterministic offset based on position for natural look
      const ox = ((x * 7 + y * 13) % 5) - 2;
      const oy = ((x * 11 + y * 3) % 5) - 2;
      ctx.beginPath();
      ctx.arc(x + ox, y + oy, radius, 0, Math.PI * 2);
      ctx.fill();
    }
  }
}

function drawMagicGroveOverlay(ctx: CanvasRenderingContext2D, size: number): void {
  // Small 4-pointed star/sparkle shapes
  ctx.fillStyle = 'rgba(255,255,255,0.15)';
  const spacing = 10;
  const arm = 3;
  const center = 1;
  for (let y = spacing / 2; y < size; y += spacing) {
    for (let x = spacing / 2; x < size; x += spacing) {
      ctx.beginPath();
      // 4-pointed star
      ctx.moveTo(x, y - arm);
      ctx.lineTo(x + center, y - center);
      ctx.lineTo(x + arm, y);
      ctx.lineTo(x + center, y + center);
      ctx.lineTo(x, y + arm);
      ctx.lineTo(x - center, y + center);
      ctx.lineTo(x - arm, y);
      ctx.lineTo(x - center, y - center);
      ctx.closePath();
      ctx.fill();
    }
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
