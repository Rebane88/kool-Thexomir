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

/**
 * Round fractional axial coordinates to the nearest hex.
 * Uses the constraint q + r + s = 0 to pick the best integer triple.
 */
export function axialRound(q: number, r: number): AxialCoord {
  const s = -q - r;
  let rq = Math.round(q);
  let rr = Math.round(r);
  const rs = Math.round(s);
  const qDiff = Math.abs(rq - q);
  const rDiff = Math.abs(rr - r);
  const sDiff = Math.abs(rs - s);
  if (qDiff > rDiff && qDiff > sDiff) {
    rq = -rr - rs;
  } else if (rDiff > sDiff) {
    rr = -rq - rs;
  }
  return { q: rq || 0, r: rr || 0 };
}

/**
 * Convert pixel position back to axial hex coordinates (pointy-top layout).
 * Inverse of axialToPixel. Uses axialRound for snapping.
 */
export function pixelToAxial(point: Point2D, layout: HexLayoutConfig): AxialCoord {
  const { size, origin } = layout;
  const px = point.x - origin.x;
  const py = point.y - origin.y;
  const q = (Math.sqrt(3) / 3 * px - 1 / 3 * py) / size;
  const r = (2 / 3 * py) / size;
  return axialRound(q, r);
}
