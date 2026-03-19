import { describe, it, expect } from 'vitest';
import { createTerrainPatternCanvas, TERRAIN_BASE_COLORS } from './terrain-patterns';

describe('TERRAIN_BASE_COLORS', () => {
  it('contains all 5 backend terrain types', () => {
    expect(Object.keys(TERRAIN_BASE_COLORS)).toHaveLength(5);
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Plains', '#90EE90');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Forest', '#228B22');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Mountain', '#808080');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Desert', '#C2B280');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Magic Grove', '#9B59B6');
  });

  it('does not contain River terrain', () => {
    expect(TERRAIN_BASE_COLORS).not.toHaveProperty('River');
  });
});

describe('createTerrainPatternCanvas', () => {
  it('returns an HTMLCanvasElement', () => {
    const canvas = createTerrainPatternCanvas('Plains', 32);
    expect(canvas).toBeInstanceOf(HTMLCanvasElement);
  });

  it('returns canvas with requested tile size dimensions', () => {
    const canvas = createTerrainPatternCanvas('Forest', 64);
    expect(canvas.width).toBe(64);
    expect(canvas.height).toBe(64);
  });

  it('returns a canvas for unknown terrain without throwing', () => {
    const canvas = createTerrainPatternCanvas('UnknownTerrain', 32);
    expect(canvas).toBeInstanceOf(HTMLCanvasElement);
  });
});
