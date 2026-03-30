import { describe, it, expect } from 'vitest';
import { createTerrainPatternCanvas, TERRAIN_BASE_COLORS } from './terrain-patterns';

describe('TERRAIN_BASE_COLORS', () => {
  it('contains all 5 backend terrain types', () => {
    expect(Object.keys(TERRAIN_BASE_COLORS)).toHaveLength(5);
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Plains', '#5a7a4a');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Forest', '#2a5a2a');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Mountain', '#4a4a50');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Desert', '#7a6e50');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('Magic Grove', '#5a3a6a');
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
