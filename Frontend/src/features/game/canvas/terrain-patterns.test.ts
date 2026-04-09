import { describe, it, expect } from 'vitest';
import { createTerrainPatternCanvas, TERRAIN_BASE_COLORS } from './terrain-patterns';

describe('TERRAIN_BASE_COLORS', () => {
  it('contains all 5 backend terrain types keyed by Code slug', () => {
    expect(Object.keys(TERRAIN_BASE_COLORS)).toHaveLength(5);
    expect(TERRAIN_BASE_COLORS).toHaveProperty('plains', '#5a7a4a');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('forest', '#2a5a2a');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('mountain', '#4a4a50');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('desert', '#7a6e50');
    expect(TERRAIN_BASE_COLORS).toHaveProperty('magic-grove', '#5a3a6a');
  });

  it('does not contain River terrain', () => {
    expect(TERRAIN_BASE_COLORS).not.toHaveProperty('river');
  });
});

describe('createTerrainPatternCanvas', () => {
  it('returns an HTMLCanvasElement', () => {
    const canvas = createTerrainPatternCanvas('plains', 32);
    expect(canvas).toBeInstanceOf(HTMLCanvasElement);
  });

  it('returns canvas with requested tile size dimensions', () => {
    const canvas = createTerrainPatternCanvas('forest', 64);
    expect(canvas.width).toBe(64);
    expect(canvas.height).toBe(64);
  });

  it('returns a canvas for unknown terrain without throwing', () => {
    const canvas = createTerrainPatternCanvas('unknown-terrain', 32);
    expect(canvas).toBeInstanceOf(HTMLCanvasElement);
  });
});
