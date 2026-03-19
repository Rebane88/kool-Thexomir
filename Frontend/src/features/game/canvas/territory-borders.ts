/**
 * Territory border drawing for kingdom boundaries and player ember glow.
 *
 * Draws faction-colored edges on hex boundaries where owned territory meets
 * unowned or enemy territory. Interior edges between same-kingdom tiles
 * get no special border. Player's own tiles get a subtle gold ember glow.
 */

import { axialToPixel, hexCorners, getHexNeighbors } from './hex-math';
import { getKingdomColor } from './hex-renderer';
import type { HexLayoutConfig } from './types';
import { SELECTION_COLOR } from './types';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';

/**
 * Draw faction-colored territory borders on kingdom boundary edges,
 * plus ember glow on the player's own tiles.
 *
 * @param ctx - Canvas 2D rendering context
 * @param tiles - Map of "q,r" keys to Tile objects
 * @param kingdoms - Map of kingdom IDs to Kingdom objects
 * @param layout - Hex layout configuration (size, origin)
 * @param myKingdomId - The current player's kingdom ID (for ember glow)
 */
export function drawTerritoryBorders(
  ctx: CanvasRenderingContext2D,
  tiles: Map<string, Tile>,
  kingdoms: Map<string, Kingdom>,
  layout: HexLayoutConfig,
  myKingdomId: string | null,
): void {
  // Pass 1: Territory borders on kingdom boundaries
  for (const [, tile] of tiles) {
    if (tile.kingdomId === null) continue;

    const center = axialToPixel({ q: tile.coordQ, r: tile.coordR }, layout);
    const corners = hexCorners(center, layout.size);
    const neighbors = getHexNeighbors(tile.coordQ, tile.coordR);
    const color = getKingdomColor(kingdoms, tile.kingdomId);

    for (let i = 0; i < 6; i++) {
      const neighborKey = `${neighbors[i].q},${neighbors[i].r}`;
      const neighborTile = tiles.get(neighborKey);

      if (!neighborTile || neighborTile.kingdomId !== tile.kingdomId) {
        ctx.beginPath();
        ctx.moveTo(corners[i].x, corners[i].y);
        ctx.lineTo(corners[(i + 1) % 6].x, corners[(i + 1) % 6].y);
        ctx.strokeStyle = color;
        ctx.lineWidth = 3;
        ctx.lineCap = 'round';
        ctx.stroke();
      }
    }
  }

  // Pass 2: Ember glow for player's own kingdom tiles
  if (myKingdomId === null) return;

  for (const [, tile] of tiles) {
    if (tile.kingdomId !== myKingdomId) continue;

    const center = axialToPixel({ q: tile.coordQ, r: tile.coordR }, layout);
    const corners = hexCorners(center, layout.size);

    ctx.save();
    ctx.globalAlpha = 0.6;
    ctx.strokeStyle = SELECTION_COLOR;
    ctx.lineWidth = 1.5;

    ctx.beginPath();
    ctx.moveTo(corners[0].x, corners[0].y);
    for (let i = 1; i < 6; i++) {
      ctx.lineTo(corners[i].x, corners[i].y);
    }
    ctx.closePath();
    ctx.stroke();

    ctx.restore();
  }
}
