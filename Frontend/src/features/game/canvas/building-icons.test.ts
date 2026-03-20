import { describe, it, expect } from 'vitest';
import { BUILDING_PNG_IMPORTS, ICON_RENDER_SIZE } from './building-icons';

describe('BUILDING_PNG_IMPORTS', () => {
  it('contains all 19 building types (18 buildings + Castle)', () => {
    expect(Object.keys(BUILDING_PNG_IMPORTS)).toHaveLength(19);
  });

  it('has entries for all backend building names', () => {
    const expectedNames = [
      'Castle', 'Farm', 'Windmill', 'Granary', 'Lumber Camp', 'Sawmill',
      'Timber Hall', 'Quarry', 'Mason', 'Stoneworks', 'Market',
      'Trading Post', 'Bank', 'Shrine', 'Wizard Tower', 'Arcane Sanctum',
      'Barracks', 'Stables', 'War Academy',
    ];
    for (const name of expectedNames) {
      expect(BUILDING_PNG_IMPORTS).toHaveProperty(name);
    }
  });

  it('maps each name to a non-empty string URL', () => {
    for (const [name, url] of Object.entries(BUILDING_PNG_IMPORTS)) {
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
