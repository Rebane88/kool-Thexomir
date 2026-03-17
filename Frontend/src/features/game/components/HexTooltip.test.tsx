import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { HexTooltip } from './HexTooltip';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';
import type { Army } from '../types/military-types';

function makeTile(overrides: Partial<Tile> = {}): Tile {
  return {
    id: 'tile-1',
    coordQ: 0,
    coordR: 0,
    terrainTypeId: 'plains',
    terrainName: 'Plains',
    kingdomId: null,
    isCapital: false,
    buildings: [],
    ...overrides,
  };
}

function makeKingdom(overrides: Partial<Kingdom> = {}): Kingdom {
  return {
    id: 'k-1',
    name: 'Northern Realm',
    userId: 'u-1',
    factionTypeId: 'f-1',
    factionName: 'Elves',
    isEliminated: false,
    resources: {},
    ...overrides,
  };
}

function makeArmy(overrides: Partial<Army> = {}): Army {
  return {
    id: 'a-1',
    tileId: 'tile-1',
    kingdomId: 'k-1',
    units: [
      { unitTypeId: 'ut-1', unitTypeName: 'Swordsmen', quantity: 5 },
      { unitTypeId: 'ut-2', unitTypeName: 'Archers', quantity: 3 },
    ],
    ...overrides,
  };
}

describe('HexTooltip', () => {
  it('renders terrain name text', () => {
    render(
      <HexTooltip tile={makeTile()} kingdom={null} armies={[]} position={{ x: 100, y: 100 }} />,
    );
    expect(screen.getByText('Plains')).toBeTruthy();
  });

  it('renders kingdom name when provided', () => {
    render(
      <HexTooltip
        tile={makeTile({ kingdomId: 'k-1' })}
        kingdom={makeKingdom()}
        armies={[]}
        position={{ x: 100, y: 100 }}
      />,
    );
    expect(screen.getByText('Northern Realm')).toBeTruthy();
  });

  it('renders "Unowned" when kingdom is null', () => {
    render(
      <HexTooltip tile={makeTile()} kingdom={null} armies={[]} position={{ x: 100, y: 100 }} />,
    );
    expect(screen.getByText('Unowned')).toBeTruthy();
  });

  it('renders "Capital" text when tile.isCapital is true', () => {
    render(
      <HexTooltip
        tile={makeTile({ isCapital: true })}
        kingdom={null}
        armies={[]}
        position={{ x: 100, y: 100 }}
      />,
    );
    expect(screen.getByText('Capital')).toBeTruthy();
  });

  it('does not render "Capital" when tile.isCapital is false', () => {
    render(
      <HexTooltip
        tile={makeTile({ isCapital: false })}
        kingdom={null}
        armies={[]}
        position={{ x: 100, y: 100 }}
      />,
    );
    expect(screen.queryByText('Capital')).toBeNull();
  });

  it('renders building names when tile has buildings', () => {
    const tile = makeTile({
      buildings: [
        { id: 'b-1', buildingTypeId: 'bt-1', buildingName: 'Barracks' },
        { id: 'b-2', buildingTypeId: 'bt-2', buildingName: 'Farm' },
      ],
    });
    render(
      <HexTooltip tile={tile} kingdom={null} armies={[]} position={{ x: 100, y: 100 }} />,
    );
    expect(screen.getByText('Barracks')).toBeTruthy();
    expect(screen.getByText('Farm')).toBeTruthy();
  });

  it('renders army unit breakdown', () => {
    render(
      <HexTooltip
        tile={makeTile()}
        kingdom={null}
        armies={[makeArmy()]}
        position={{ x: 100, y: 100 }}
      />,
    );
    expect(screen.getByText('Swordsmen: 5')).toBeTruthy();
    expect(screen.getByText('Archers: 3')).toBeTruthy();
  });

  it('renders Panel component with border class', () => {
    const { container } = render(
      <HexTooltip tile={makeTile()} kingdom={null} armies={[]} position={{ x: 100, y: 100 }} />,
    );
    const panel = container.querySelector('.border-bronze-700');
    expect(panel).toBeTruthy();
  });
});
