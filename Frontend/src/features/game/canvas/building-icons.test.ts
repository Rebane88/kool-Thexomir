import { describe, it, expect } from 'vitest';
import { BUILDING_ICON_IMPORTS, ICON_RENDER_SIZE } from './building-icons';

describe('BUILDING_ICON_IMPORTS', () => {
  it('contains all 20 building types (19 buildings + Castle)', () => {
    expect(Object.keys(BUILDING_ICON_IMPORTS)).toHaveLength(20);
  });

  it('has entries for all backend building names', () => {
    const expectedNames = [
      'Castle', 'Farm', 'Windmill', 'Granary', 'Lumber Camp', 'Sawmill',
      'Timber Hall', 'Quarry', 'Mason', 'Stoneworks', 'Market',
      'Trading Post', 'Bank', 'Shrine', 'Wizard Tower', 'Arcane Sanctum',
      'Barracks', 'Stables', 'War Academy',
    ];
    for (const name of expectedNames) {
      expect(BUILDING_ICON_IMPORTS).toHaveProperty(name);
    }
  });

  it('maps each name to a non-empty string URL', () => {
    for (const [name, url] of Object.entries(BUILDING_ICON_IMPORTS)) {
      expect(typeof url).toBe('string');
      expect(url.length).toBeGreaterThan(0);
    }
  });
});

describe('ICON_RENDER_SIZE', () => {
  it('is 22 pixels', () => {
    expect(ICON_RENDER_SIZE).toBe(22);
  });
});
