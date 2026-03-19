/**
 * SVG building icon loading and rasterization pipeline.
 *
 * All 19 building SVG icons are imported statically via Vite and can be
 * pre-rasterized to offscreen canvases for fast drawImage() blitting
 * during the render loop.
 */

import castleSvg from '../../../assets/building-icons/castle.svg';
import farmSvg from '../../../assets/building-icons/farm.svg';
import windmillSvg from '../../../assets/building-icons/windmill.svg';
import granarySvg from '../../../assets/building-icons/granary.svg';
import lumberCampSvg from '../../../assets/building-icons/lumber-camp.svg';
import sawmillSvg from '../../../assets/building-icons/sawmill.svg';
import timberHallSvg from '../../../assets/building-icons/timber-hall.svg';
import quarrySvg from '../../../assets/building-icons/quarry.svg';
import masonSvg from '../../../assets/building-icons/mason.svg';
import stoneworksSvg from '../../../assets/building-icons/stoneworks.svg';
import marketSvg from '../../../assets/building-icons/market.svg';
import tradingPostSvg from '../../../assets/building-icons/trading-post.svg';
import bankSvg from '../../../assets/building-icons/bank.svg';
import shrineSvg from '../../../assets/building-icons/shrine.svg';
import wizardTowerSvg from '../../../assets/building-icons/wizard-tower.svg';
import arcaneSanctumSvg from '../../../assets/building-icons/arcane-sanctum.svg';
import barracksSvg from '../../../assets/building-icons/barracks.svg';
import stablesSvg from '../../../assets/building-icons/stables.svg';
import warAcademySvg from '../../../assets/building-icons/war-academy.svg';

/**
 * Map of backend building names to their Vite-resolved SVG URLs.
 * Keys must exactly match BuildingType.Name values from the backend seeder.
 */
export const BUILDING_ICON_IMPORTS: Record<string, string> = {
  'Castle': castleSvg,
  'Farm': farmSvg,
  'Windmill': windmillSvg,
  'Granary': granarySvg,
  'Lumber Camp': lumberCampSvg,
  'Sawmill': sawmillSvg,
  'Timber Hall': timberHallSvg,
  'Quarry': quarrySvg,
  'Mason': masonSvg,
  'Stoneworks': stoneworksSvg,
  'Market': marketSvg,
  'Trading Post': tradingPostSvg,
  'Bank': bankSvg,
  'Shrine': shrineSvg,
  'Wizard Tower': wizardTowerSvg,
  'Arcane Sanctum': arcaneSanctumSvg,
  'Barracks': barracksSvg,
  'Stables': stablesSvg,
  'War Academy': warAcademySvg,
};

/** Icon display size in logical pixels within a hex tile */
export const ICON_RENDER_SIZE = 22;

/**
 * Load a single SVG URL and rasterize it to a DPI-aware offscreen canvas.
 *
 * @param url - Resolved SVG URL (from Vite import)
 * @param size - Logical pixel size (square)
 * @returns Canvas with the SVG rendered at devicePixelRatio scale
 */
export async function loadSvgIcon(url: string, size: number): Promise<HTMLCanvasElement> {
  const img = new Image();

  await new Promise<void>((resolve, reject) => {
    img.onload = () => resolve();
    img.onerror = () => reject(new Error(`Failed to load SVG icon: ${url}`));
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
 * Load and rasterize all building SVG icons in parallel.
 *
 * @param onProgress - Optional callback invoked after each icon loads
 * @returns Map of building name to pre-rasterized canvas
 */
export async function loadAllBuildingIcons(
  onProgress?: (loaded: number, total: number) => void,
): Promise<Map<string, HTMLCanvasElement>> {
  const entries = Object.entries(BUILDING_ICON_IMPORTS);
  const total = entries.length;
  let loaded = 0;

  const results = await Promise.all(
    entries.map(async ([name, url]) => {
      const canvas = await loadSvgIcon(url, ICON_RENDER_SIZE);
      loaded++;
      onProgress?.(loaded, total);
      return [name, canvas] as const;
    }),
  );

  return new Map(results);
}
