/**
 * Territory border drawing for kingdom boundaries.
 *
 * Draws faction-colored edges on hex boundaries where owned territory meets
 * unowned or enemy territory. Interior edges between same-kingdom tiles
 * get no special border.
 */

import { axialToPixel, hexCorners, getHexNeighbors } from './hex-math';
import { getKingdomColor } from './hex-renderer';
import type { HexLayoutConfig } from './types';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';

/**
 * Draw faction-colored territory borders on kingdom boundary edges.
 */
export function drawTerritoryBorders(
  ctx: CanvasRenderingContext2D,
  tiles: Map<string, Tile>,
  kingdoms: Map<string, Kingdom>,
  layout: HexLayoutConfig,
  _myKingdomId: string | null,
): void {
  // Pass 1: Territory borders on kingdom boundaries
  for (const [, tile] of tiles) {
    if (tile.kingdomId === null) continue;

    const center = axialToPixel({ q: tile.coordQ, r: tile.coordR }, layout);
    const corners = hexCorners(center, layout.size);
    const neighbors = getHexNeighbors(tile.coordQ, tile.coordR);
    const color = getKingdomColor(kingdoms, tile.kingdomId);

    // Map from AXIAL_DIRECTIONS index to pointy-top edge index.
    // AXIAL_DIRECTIONS: [E, NE, NW, W, SW, SE]
    // Pointy-top edges (corner i → i+1): [SE(0), SW(1), W(2), NW(3), NE(4), E(5)]
    const dirToEdge = [5, 4, 3, 2, 1, 0];

    for (let i = 0; i < 6; i++) {
      const neighborKey = `${neighbors[i].q},${neighbors[i].r}`;
      const neighborTile = tiles.get(neighborKey);

      if (!neighborTile || neighborTile.kingdomId !== tile.kingdomId) {
        const edge = dirToEdge[i];
        ctx.beginPath();
        ctx.moveTo(corners[edge].x, corners[edge].y);
        ctx.lineTo(corners[(edge + 1) % 6].x, corners[(edge + 1) % 6].y);
        ctx.strokeStyle = color;
        ctx.lineWidth = 3;
        ctx.lineCap = 'round';
        ctx.stroke();
      }
    }
  }

}
