/**
 * PNG building icon loading and rasterization pipeline.
 *
 * All 19 building PNG icons are imported statically via Vite and can be
 * pre-rasterized to offscreen canvases for fast drawImage() blitting
 * during the render loop.
 */

import castlePng from '@/assets/images/building-castle.png';
import farmPng from '@/assets/images/building-farm.png';
import windmillPng from '@/assets/images/building-windmill.png';
import granaryPng from '@/assets/images/building-granary.png';
import lumberCampPng from '@/assets/images/building-lumber-camp.png';
import sawmillPng from '@/assets/images/building-sawmill.png';
import timberHallPng from '@/assets/images/building-timber-hall.png';
import quarryPng from '@/assets/images/building-quarry.png';
import masonPng from '@/assets/images/building-mason.png';
import stoneworksPng from '@/assets/images/building-stoneworks.png';
import marketPng from '@/assets/images/building-market.png';
import tradingPostPng from '@/assets/images/building-trading-post.png';
import bankPng from '@/assets/images/building-bank.png';
import shrinePng from '@/assets/images/building-shrine.png';
import wizardTowerPng from '@/assets/images/building-wizard-tower.png';
import arcaneSanctumPng from '@/assets/images/building-arcane-sanctum.png';
import barracksPng from '@/assets/images/building-barracks.png';
import stablesPng from '@/assets/images/building-stables.png';
import warAcademyPng from '@/assets/images/building-war-academy.png';

/**
 * Map of backend building names to their Vite-resolved PNG URLs.
 * Keys must exactly match BuildingType.Name values from the backend seeder.
 */
export const BUILDING_PNG_IMPORTS: Record<string, string> = {
  'Castle': castlePng,
  'Farm': farmPng,
  'Windmill': windmillPng,
  'Granary': granaryPng,
  'Lumber Camp': lumberCampPng,
  'Sawmill': sawmillPng,
  'Timber Hall': timberHallPng,
  'Quarry': quarryPng,
  'Mason': masonPng,
  'Stoneworks': stoneworksPng,
  'Market': marketPng,
  'Trading Post': tradingPostPng,
  'Bank': bankPng,
  'Shrine': shrinePng,
  'Wizard Tower': wizardTowerPng,
  'Arcane Sanctum': arcaneSanctumPng,
  'Barracks': barracksPng,
  'Stables': stablesPng,
  'War Academy': warAcademyPng,
};

/** Icon rasterization size in logical pixels (higher = sharper on tiles) */
export const ICON_RASTER_SIZE = 48;

/** Icon display size in logical pixels within a hex tile */
export const ICON_RENDER_SIZE = 22;

/**
 * Load a single image URL and rasterize it to a DPI-aware offscreen canvas.
 *
 * @param url - Resolved image URL (from Vite import)
 * @param size - Logical pixel size (square)
 * @returns Canvas with the image rendered at devicePixelRatio scale
 */
export async function loadIcon(url: string, size: number): Promise<HTMLCanvasElement> {
  const img = new Image();

  await new Promise<void>((resolve, reject) => {
    img.onload = () => resolve();
    img.onerror = () => reject(new Error(`Failed to load icon: ${url}`));
    img.src = url;
  });

  const dpr = window.devicePixelRatio || 1;
  const canvas = document.createElement('canvas');
  canvas.width = size * dpr;
  canvas.height = size * dpr;

  const ctx = canvas.getContext('2d');
  if (ctx) {
    ctx.scale(dpr, dpr);
    ctx.drawImage(img, 0, 0, size, size);
  }

  return canvas;
}

/**
 * Load and rasterize all building PNG icons in parallel.
 *
 * @param onProgress - Optional callback invoked after each icon loads
 * @returns Map of building name to pre-rasterized canvas
 */
export async function loadAllBuildingIcons(
  onProgress?: (loaded: number, total: number) => void,
): Promise<Map<string, HTMLCanvasElement>> {
  const entries = Object.entries(BUILDING_PNG_IMPORTS);
  const total = entries.length;
  let loaded = 0;

  const results = await Promise.all(
    entries.map(async ([name, url]) => {
      const canvas = await loadIcon(url, ICON_RASTER_SIZE);
      loaded++;
      onProgress?.(loaded, total);
      return [name, canvas] as const;
    }),
  );

  return new Map(results);
}
