import type { AxialCoord, HexLayoutConfig } from './types';
import { axialToPixel, hexCorners } from './hex-math';

function drawHex(
  ctx: CanvasRenderingContext2D,
  center: { x: number; y: number },
  size: number,
  coord: AxialCoord,
): void {
  const corners = hexCorners(center, size);

  ctx.beginPath();
  ctx.moveTo(corners[0].x, corners[0].y);
  for (let i = 1; i < 6; i++) {
    ctx.lineTo(corners[i].x, corners[i].y);
  }
  ctx.closePath();
  ctx.stroke();

  ctx.fillText(`${coord.q},${coord.r}`, center.x, center.y);
}

export function drawHexGrid(
  ctx: CanvasRenderingContext2D,
  coords: AxialCoord[],
  layout: HexLayoutConfig,
): void {
  ctx.strokeStyle = '#d97706';
  ctx.lineWidth = 1;
  ctx.fillStyle = '#9ca3af';
  ctx.font = '10px monospace';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';

  for (const coord of coords) {
    const center = axialToPixel(coord, layout);
    drawHex(ctx, center, layout.size, coord);
  }
}
