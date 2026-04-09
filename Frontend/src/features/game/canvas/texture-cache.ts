/**
 * Centralized texture cache for pre-built terrain patterns and building icons.
 *
 * Provides init/invalidate lifecycle:
 * - init(): Creates terrain CanvasPatterns and loads+rasterizes SVG building icons
 * - invalidate(): Rebuilds terrain patterns (e.g. after canvas context change)
 * - getTerrainPattern() / getBuildingIcon(): Fast lookups during render loop
 *
 * Exported as a module-level singleton.
 */

import { createTerrainPatternCanvas, TERRAIN_BASE_COLORS } from './terrain-patterns';
import { loadAllBuildingIcons } from './building-icons';

const TERRAIN_TILE_SIZE = 32;

class TextureCache {
  private terrainPatterns = new Map<string, CanvasPattern>();
  private buildingIcons = new Map<string, HTMLCanvasElement>();
  private _initialized = false;

  get initialized(): boolean {
    return this._initialized;
  }

  /**
   * Initialize the cache: build terrain patterns and load all building icons.
   *
   * @param ctx - Canvas 2D context used to create repeating patterns
   * @param onProgress - Optional progress callback for icon loading
   */
  async init(
    ctx: CanvasRenderingContext2D,
    onProgress?: (loaded: number, total: number) => void,
  ): Promise<void> {
    // Build terrain patterns
    this.buildTerrainPatterns(ctx);

    // Load and rasterize building icons
    this.buildingIcons = await loadAllBuildingIcons(onProgress);

    this._initialized = true;
  }

  /**
   * Rebuild terrain patterns after a canvas context change (e.g. resize/DPI change).
   * Building icons remain valid since they use their own offscreen canvases.
   */
  invalidate(ctx: CanvasRenderingContext2D): void {
    this.buildTerrainPatterns(ctx);
  }

  /**
   * Get a repeating CanvasPattern for the given terrain Code slug (e.g. "plains").
   * Returns null if cache is not initialized or the code is unknown.
   */
  getTerrainPattern(code: string): CanvasPattern | null {
    return this.terrainPatterns.get(code) ?? null;
  }

  /**
   * Get a pre-rasterized building icon canvas for the given building Code slug (e.g. "farm").
   * Returns null if cache is not initialized or the code is unknown.
   */
  getBuildingIcon(code: string): HTMLCanvasElement | null {
    return this.buildingIcons.get(code) ?? null;
  }

  // -------------------------------------------------------------------------
  // Internal
  // -------------------------------------------------------------------------

  private buildTerrainPatterns(ctx: CanvasRenderingContext2D): void {
    this.terrainPatterns.clear();
    for (const terrainCode of Object.keys(TERRAIN_BASE_COLORS)) {
      const patternCanvas = createTerrainPatternCanvas(terrainCode, TERRAIN_TILE_SIZE);
      const pattern = ctx.createPattern(patternCanvas, 'repeat');
      if (pattern) {
        this.terrainPatterns.set(terrainCode, pattern);
      }
    }
  }
}

export const textureCache = new TextureCache();
