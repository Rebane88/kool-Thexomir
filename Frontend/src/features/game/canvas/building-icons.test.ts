import { describe, it, expect } from 'vitest';
import { BUILDING_PNG_IMPORTS, ICON_RENDER_SIZE } from './building-icons';

describe('BUILDING_PNG_IMPORTS', () => {
  it('contains all 19 building types (18 buildings + Castle)', () => {
    expect(Object.keys(BUILDING_PNG_IMPORTS)).toHaveLength(19);
  });

  it('has entries for all backend building Code slugs', () => {
    const expectedCodes = [
      'castle', 'farm', 'windmill', 'granary', 'lumber-camp', 'sawmill',
      'timber-hall', 'quarry', 'mason', 'stoneworks', 'market',
      'trading-post', 'bank', 'shrine', 'wizard-tower', 'arcane-sanctum',
      'barracks', 'stables', 'war-academy',
    ];
    for (const code of expectedCodes) {
      expect(BUILDING_PNG_IMPORTS).toHaveProperty(code);
    }
  });

  it('maps each code to a non-empty string URL', () => {
    for (const [_code, url] of Object.entries(BUILDING_PNG_IMPORTS)) {
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
