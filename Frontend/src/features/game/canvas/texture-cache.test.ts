import { describe, it, expect } from 'vitest';
import { textureCache } from './texture-cache';

describe('TextureCache', () => {
  it('exports a singleton instance', () => {
    expect(textureCache).toBeDefined();
    expect(textureCache).toHaveProperty('init');
    expect(textureCache).toHaveProperty('invalidate');
    expect(textureCache).toHaveProperty('getTerrainPattern');
    expect(textureCache).toHaveProperty('getBuildingIcon');
  });

  it('is not initialized before init() is called', () => {
    // Fresh import -- not yet initialized
    expect(textureCache.initialized).toBe(false);
  });

  it('getTerrainPattern returns null before init', () => {
    expect(textureCache.getTerrainPattern('plains')).toBeNull();
  });

  it('getBuildingIcon returns null before init', () => {
    expect(textureCache.getBuildingIcon('castle')).toBeNull();
  });
});
