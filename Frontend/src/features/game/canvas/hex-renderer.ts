import type { HexLayoutConfig, MapRenderState, Point2D } from './types';
import {
  TERRAIN_COLORS,
  KINGDOM_COLORS,
  KINGDOM_OVERLAY_ALPHA,
  HEX_BORDER_COLOR,
  SELECTION_COLOR,
  HOVER_COLOR,
  CAPITAL_COLOR,
} from './types';
import { axialToPixel, hexCorners } from './hex-math';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';
import type { Army } from '../types/military-types';

export interface DrawOptions {
  skipClear?: boolean;
}

interface GameMapState {
  tiles: Map<string, Tile>;
  tileIdToCoord: Map<string, string>;
  kingdoms: Map<string, Kingdom>;
  armies: Map<string, Army>;
  myKingdomId: string | null;
  mapRadius: number;
}

/**
 * Get the display color for a kingdom based on its insertion order in the kingdoms map.
 */
export function getKingdomColor(kingdoms: Map<string, Kingdom>, kingdomId: string): string {
  const keys = [...kingdoms.keys()];
  const index = keys.indexOf(kingdomId);
  if (index < 0) return KINGDOM_COLORS[0];
  return KINGDOM_COLORS[index % KINGDOM_COLORS.length];
}

// ---------------------------------------------------------------------------
// Internal helpers
// ---------------------------------------------------------------------------

function drawHexPath(ctx: CanvasRenderingContext2D, corners: Point2D[]): void {
  ctx.beginPath();
  ctx.moveTo(corners[0].x, corners[0].y);
  for (let i = 1; i < 6; i++) {
    ctx.lineTo(corners[i].x, corners[i].y);
  }
  ctx.closePath();
}

function drawFilledHex(
  ctx: CanvasRenderingContext2D,
  center: Point2D,
  size: number,
  fillColor: string,
  borderColor: string,
  borderWidth: number,
): void {
  const corners = hexCorners(center, size);
  drawHexPath(ctx, corners);
  ctx.fillStyle = fillColor;
  ctx.fill();
  ctx.strokeStyle = borderColor;
  ctx.lineWidth = borderWidth;
  ctx.stroke();
}

function drawCrown(ctx: CanvasRenderingContext2D, cx: number, cy: number, crownSize: number): void {
  const half = crownSize / 2;
  const baseY = cy + half * 0.4;
  const topY = cy - half * 0.6;

  ctx.beginPath();
  // Base of crown
  ctx.moveTo(cx - half, baseY);
  // Left peak
  ctx.lineTo(cx - half * 0.6, topY);
  // Left valley
  ctx.lineTo(cx - half * 0.2, baseY - half * 0.2);
  // Center peak
  ctx.lineTo(cx, topY - half * 0.2);
  // Right valley
  ctx.lineTo(cx + half * 0.2, baseY - half * 0.2);
  // Right peak
  ctx.lineTo(cx + half * 0.6, topY);
  // Base right
  ctx.lineTo(cx + half, baseY);
  ctx.closePath();

  ctx.fillStyle = CAPITAL_COLOR;
  ctx.fill();
  ctx.strokeStyle = CAPITAL_COLOR;
  ctx.lineWidth = 0.5;
  ctx.stroke();
}

function drawBadge(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number,
  text: string,
  textColor: string,
  bgColor: string,
): void {
  const radius = 7;
  ctx.beginPath();
  ctx.arc(x, y, radius, 0, Math.PI * 2);
  ctx.fillStyle = bgColor;
  ctx.fill();

  ctx.font = 'bold 10px sans-serif';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillStyle = textColor;
  ctx.fillText(text, x, y);
}

// ---------------------------------------------------------------------------
// Main draw pipeline
// ---------------------------------------------------------------------------

/**
 * Layered draw pipeline for the full game map.
 *
 * Layers (in order):
 * 1. Clear canvas
 * 2. Terrain fills with hex borders
 * 3. Kingdom ownership overlays
 * 4. Indicators (capitals, buildings, armies)
 * 5. Hover highlight
 * 6. Selection highlight
 */
export function drawGameMap(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  state: GameMapState,
  renderState: MapRenderState,
  options?: DrawOptions,
): void {
  // 1. Clear canvas (skipped when caller handles clearing before camera transform)
  if (!options?.skipClear) {
    ctx.fillStyle = '#0a0a0f';
    ctx.fillRect(0, 0, width, height);
  }


  // 2. Compute layout
  const layout: HexLayoutConfig = {
    size: 30,
    origin: { x: width / 2, y: height / 2 },
  };

  // 3. Build armies by coord lookup
  const armiesByCoord = new Map<string, Army[]>();
  for (const army of state.armies.values()) {
    const coord = state.tileIdToCoord.get(army.tileId);
    if (coord) {
      const existing = armiesByCoord.get(coord);
      if (existing) {
        existing.push(army);
      } else {
        armiesByCoord.set(coord, [army]);
      }
    }
  }

  // Pre-compute centers and corners for each tile
  const tileRenderData = new Map<string, { center: Point2D; corners: Point2D[] }>();
  for (const [key, tile] of state.tiles) {
    const center = axialToPixel({ q: tile.coordQ, r: tile.coordR }, layout);
    const corners = hexCorners(center, layout.size);
    tileRenderData.set(key, { center, corners });
  }

  // Layer 1: Terrain fills
  for (const [key, tile] of state.tiles) {
    const data = tileRenderData.get(key)!;
    const terrainColor = TERRAIN_COLORS[tile.terrainName] ?? '#333333';
    drawHexPath(ctx, data.corners);
    ctx.fillStyle = terrainColor;
    ctx.fill();
    ctx.strokeStyle = HEX_BORDER_COLOR;
    ctx.lineWidth = 1;
    ctx.stroke();
  }

  // Layer 2: Kingdom overlays
  for (const [key, tile] of state.tiles) {
    if (tile.kingdomId === null) continue;
    const data = tileRenderData.get(key)!;
    const kingdomColor = getKingdomColor(state.kingdoms, tile.kingdomId);

    ctx.save();
    ctx.globalAlpha = KINGDOM_OVERLAY_ALPHA;
    drawHexPath(ctx, data.corners);
    ctx.fillStyle = kingdomColor;
    ctx.fill();
    ctx.restore();

    // Ember glow for player's own kingdom
    if (tile.kingdomId === state.myKingdomId) {
      ctx.save();
      ctx.globalAlpha = 0.6;
      drawHexPath(ctx, data.corners);
      ctx.strokeStyle = SELECTION_COLOR;
      ctx.lineWidth = 1.5;
      ctx.stroke();
      ctx.restore();
    }
  }

  // Layer 3: Indicators
  for (const [key, tile] of state.tiles) {
    const data = tileRenderData.get(key)!;
    const { center } = data;

    // Capital crown
    if (tile.isCapital) {
      drawCrown(ctx, center.x, center.y, 12);
    }

    // Building count badge (bottom-right)
    if (tile.buildings.length > 0) {
      drawBadge(
        ctx,
        center.x + layout.size * 0.3,
        center.y + layout.size * 0.35,
        tile.buildings.length.toString(),
        '#ffffff',
        'rgba(0, 0, 0, 0.7)',
      );
    }

    // Army unit count badge (top-left)
    const tileArmies = armiesByCoord.get(key);
    if (tileArmies && tileArmies.length > 0) {
      let totalUnits = 0;
      let armyKingdomId = tileArmies[0].kingdomId;
      for (const army of tileArmies) {
        for (const unit of army.units) {
          totalUnits += unit.quantity;
        }
        armyKingdomId = army.kingdomId;
      }
      if (totalUnits > 0) {
        const armyColor = getKingdomColor(state.kingdoms, armyKingdomId);
        drawBadge(
          ctx,
          center.x - layout.size * 0.3,
          center.y - layout.size * 0.35,
          totalUnits.toString(),
          armyColor,
          'rgba(0, 0, 0, 0.7)',
        );
      }
    }
  }

  // Layer 4: Hover highlight
  if (renderState.hoveredTileKey) {
    const tile = state.tiles.get(renderState.hoveredTileKey);
    if (tile) {
      const data = tileRenderData.get(renderState.hoveredTileKey)!;
      drawHexPath(ctx, data.corners);
      ctx.strokeStyle = HOVER_COLOR;
      ctx.lineWidth = 1.5;
      ctx.stroke();
    }
  }

  // Layer 5: Selection highlight
  if (renderState.selectedTileKey) {
    const tile = state.tiles.get(renderState.selectedTileKey);
    if (tile) {
      const data = tileRenderData.get(renderState.selectedTileKey)!;
      drawHexPath(ctx, data.corners);
      ctx.strokeStyle = SELECTION_COLOR;
      ctx.lineWidth = 3;
      ctx.stroke();
    }
  }
}
