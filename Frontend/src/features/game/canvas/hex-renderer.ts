import type { HexLayoutConfig, MapRenderState, Point2D } from './types';
import {
  KINGDOM_COLORS,
  HEX_BORDER_COLOR,
  SELECTION_COLOR,
  HOVER_COLOR,
} from './types';
import { axialToPixel, hexCorners, getHexNeighbors } from './hex-math';
import { textureCache } from './texture-cache';
import { drawTerritoryBorders } from './territory-borders';
import { TERRAIN_BASE_COLORS } from './terrain-patterns';
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
 * 2. Terrain fills with hex borders (pattern fills from TextureCache)
 * 3. Territory borders (faction-colored edges + ember glow)
 * 2.5. Build mode overlays
 * 2.6. Movement/attack overlays
 * 4. Indicators (building icons, army badges)
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

  // Layer 1: Terrain fills (pattern fills from TextureCache)
  for (const [key, tile] of state.tiles) {
    const data = tileRenderData.get(key)!;
    const pattern = textureCache.getTerrainPattern(tile.terrainName);

    drawHexPath(ctx, data.corners);
    if (pattern) {
      ctx.save();
      ctx.clip();
      ctx.fillStyle = pattern;
      ctx.fillRect(
        data.center.x - layout.size,
        data.center.y - layout.size,
        layout.size * 2,
        layout.size * 2,
      );
      ctx.restore();
    } else {
      // Fallback to flat color from TERRAIN_BASE_COLORS
      ctx.fillStyle = TERRAIN_BASE_COLORS[tile.terrainName] ?? '#333333';
      ctx.fill();
    }

    // Hex border
    drawHexPath(ctx, data.corners);
    ctx.strokeStyle = HEX_BORDER_COLOR;
    ctx.lineWidth = 1;
    ctx.stroke();
  }

  // Layer 2: Territory borders (replaces kingdom color overlay)
  drawTerritoryBorders(ctx, state.tiles, state.kingdoms, layout, state.myKingdomId);

  // Layer 2.5: Build mode overlays
  if (renderState.buildModeTypeId) {
    for (const [key, tile] of state.tiles) {
      // Only overlay player's own tiles
      if (tile.kingdomId !== state.myKingdomId) continue;

      const data = tileRenderData.get(key)!;

      // Invalid: tile already has a building (backend enforces ONE building per tile)
      if (tile.buildings.length > 0) {
        drawHexPath(ctx, data.corners);
        ctx.fillStyle = 'rgba(220, 38, 38, 0.25)';
        ctx.fill();
      } else {
        // Valid: owned tile with no building -- green border only (no fill)
        drawHexPath(ctx, data.corners);
        ctx.strokeStyle = 'rgba(34, 197, 94, 0.7)';
        ctx.lineWidth = 2;
        ctx.stroke();
      }
    }

    // Hover highlight on valid tile: bold green fill
    if (renderState.hoveredTileKey) {
      const hoveredTile = state.tiles.get(renderState.hoveredTileKey);
      if (
        hoveredTile &&
        hoveredTile.kingdomId === state.myKingdomId &&
        hoveredTile.buildings.length === 0
      ) {
        const data = tileRenderData.get(renderState.hoveredTileKey)!;
        drawHexPath(ctx, data.corners);
        ctx.fillStyle = 'rgba(34, 197, 94, 0.35)';
        ctx.fill();
      }
    }
  }

  // Layer 2.6: Movement/attack overlays
  if (renderState.armyHighlightTileKey && !renderState.buildModeTypeId) {
    const armyTile = state.tiles.get(renderState.armyHighlightTileKey);
    if (armyTile) {
      const neighbors = getHexNeighbors(armyTile.coordQ, armyTile.coordR);
      for (const neighbor of neighbors) {
        const key = `${neighbor.q},${neighbor.r}`;
        const tile = state.tiles.get(key);
        if (!tile) continue;
        const data = tileRenderData.get(key);
        if (!data) continue;

        const isEnemy = tile.kingdomId !== null
          && tile.kingdomId !== state.myKingdomId;

        drawHexPath(ctx, data.corners);
        ctx.strokeStyle = isEnemy
          ? 'rgba(220, 38, 38, 0.7)'   // red for enemy tiles
          : 'rgba(34, 197, 94, 0.7)';   // green for friendly/empty
        ctx.lineWidth = 2.5;
        ctx.stroke();
      }
    }
  }

  // Layer 3: Indicators
  for (const [key, tile] of state.tiles) {
    const data = tileRenderData.get(key)!;
    const { center } = data;

    // Building icon (or Castle icon for castle tiles)
    const buildingName = tile.isCastle
      ? 'Castle'
      : tile.buildings.length > 0
        ? tile.buildings[0].buildingName
        : null;

    if (buildingName) {
      const icon = textureCache.getBuildingIcon(buildingName);
      if (icon) {
        const iconLogicalSize = 22; // matches ICON_RENDER_SIZE
        const drawX = center.x - iconLogicalSize / 2;
        const drawY = center.y - iconLogicalSize / 2;

        if (tile.kingdomId) {
          // Draw tinted icon using temp canvas compositing
          const tintColor = getKingdomColor(state.kingdoms, tile.kingdomId);
          const temp = document.createElement('canvas');
          temp.width = icon.width;
          temp.height = icon.height;
          const tctx = temp.getContext('2d')!;
          tctx.drawImage(icon, 0, 0);
          tctx.globalCompositeOperation = 'source-atop';
          tctx.fillStyle = tintColor;
          tctx.globalAlpha = 0.4;
          tctx.fillRect(0, 0, temp.width, temp.height);
          ctx.drawImage(temp, 0, 0, temp.width, temp.height, drawX, drawY, iconLogicalSize, iconLogicalSize);
        } else {
          ctx.drawImage(icon, 0, 0, icon.width, icon.height, drawX, drawY, iconLogicalSize, iconLogicalSize);
        }
      }
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
