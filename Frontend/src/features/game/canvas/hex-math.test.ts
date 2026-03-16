import { describe, it, expect } from 'vitest';
import { axialToPixel, hexCorners, generateAxialCoords } from './hex-math';
import type { HexLayoutConfig } from './types';

describe('axialToPixel', () => {
  const originLayout: HexLayoutConfig = {
    size: 30,
    origin: { x: 0, y: 0 },
  };

  const offsetLayout: HexLayoutConfig = {
    size: 30,
    origin: { x: 100, y: 100 },
  };

  it('maps center hex (0,0) to the layout origin', () => {
    const pixel = axialToPixel({ q: 0, r: 0 }, offsetLayout);
    expect(pixel.x).toBeCloseTo(100, 5);
    expect(pixel.y).toBeCloseTo(100, 5);
  });

  it('maps q=1, r=0 to one step east (sqrt(3)*size, 0)', () => {
    const pixel = axialToPixel({ q: 1, r: 0 }, originLayout);
    expect(pixel.x).toBeCloseTo(30 * Math.sqrt(3), 5);
    expect(pixel.y).toBeCloseTo(0, 5);
  });

  it('maps q=0, r=1 to one step south-east (sqrt(3)/2*size, 1.5*size)', () => {
    const pixel = axialToPixel({ q: 0, r: 1 }, originLayout);
    expect(pixel.x).toBeCloseTo(30 * Math.sqrt(3) / 2, 5);
    expect(pixel.y).toBeCloseTo(45, 5);
  });
});

describe('hexCorners', () => {
  const center = { x: 100, y: 100 };
  const size = 30;

  it('returns exactly 6 corner points', () => {
    const corners = hexCorners(center, size);
    expect(corners).toHaveLength(6);
  });

  it('places each corner at exactly `size` distance from center', () => {
    const corners = hexCorners(center, size);
    for (const corner of corners) {
      const dist = Math.sqrt(
        (corner.x - center.x) ** 2 + (corner.y - center.y) ** 2,
      );
      expect(dist).toBeCloseTo(size, 3);
    }
  });

  it('starts first corner at 30 degrees (pointy-top orientation)', () => {
    const corners = hexCorners(center, size);
    const angleRad = (Math.PI / 180) * 30;
    expect(corners[0].x).toBeCloseTo(center.x + size * Math.cos(angleRad), 5);
    expect(corners[0].y).toBeCloseTo(center.y + size * Math.sin(angleRad), 5);
  });
});

describe('generateAxialCoords', () => {
  it('returns 1 hex for radius 0', () => {
    const coords = generateAxialCoords(0);
    expect(coords).toHaveLength(1);
    expect(coords[0]).toEqual({ q: 0, r: 0 });
  });

  it('returns 7 hexes for radius 1 (center + 6 ring)', () => {
    const coords = generateAxialCoords(1);
    expect(coords).toHaveLength(7);
  });

  it('returns 169 hexes for radius 7', () => {
    const coords = generateAxialCoords(7);
    expect(coords).toHaveLength(169);
  });

  it('all coordinates satisfy hex distance constraint: max(|q|, |r|, |q+r|) <= radius', () => {
    const radius = 7;
    const coords = generateAxialCoords(radius);
    for (const { q, r } of coords) {
      const hexDist = Math.max(Math.abs(q), Math.abs(r), Math.abs(q + r));
      expect(hexDist).toBeLessThanOrEqual(radius);
    }
  });
});
