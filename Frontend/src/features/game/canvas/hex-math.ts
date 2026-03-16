import type { AxialCoord, HexLayoutConfig, Point2D } from './types';

/**
 * Convert axial hex coordinates to pixel position (pointy-top layout).
 * Formula: redblobgames.com/grids/hexagons
 */
export function axialToPixel(
  coord: AxialCoord,
  layout: HexLayoutConfig,
): Point2D {
  const { q, r } = coord;
  const { size, origin } = layout;
  return {
    x: origin.x + size * (Math.sqrt(3) * q + (Math.sqrt(3) / 2) * r),
    y: origin.y + size * (1.5 * r),
  };
}

/**
 * Calculate the 6 corner vertices of a pointy-top hex.
 * First corner at 30 degrees, then every 60 degrees.
 */
export function hexCorners(center: Point2D, size: number): Point2D[] {
  const corners: Point2D[] = [];
  for (let i = 0; i < 6; i++) {
    const angleDeg = 30 + 60 * i;
    const angleRad = (Math.PI / 180) * angleDeg;
    corners.push({
      x: center.x + size * Math.cos(angleRad),
      y: center.y + size * Math.sin(angleRad),
    });
  }
  return corners;
}

/**
 * Generate all axial coordinates within a hex radius.
 * radius=0 -> 1 hex, radius=1 -> 7, radius=7 -> 169.
 */
export function generateAxialCoords(radius: number): AxialCoord[] {
  const coords: AxialCoord[] = [];
  for (let q = -radius; q <= radius; q++) {
    const r1 = Math.max(-radius, -q - radius);
    const r2 = Math.min(radius, -q + radius);
    for (let r = r1; r <= r2; r++) {
      coords.push({ q: q || 0, r: r || 0 });
    }
  }
  return coords;
}
