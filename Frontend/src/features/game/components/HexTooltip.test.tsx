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
    terrainCode: 'plains',
    terrainName: 'Plains',
    kingdomId: null,
    isCastle: false,
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
    status: 'Active',
    resources: {},
    ...overrides,
  };
}

function makeArmy(overrides: Partial<Army> = {}): Army {
  return {
    id: 'a-1',
    buildingId: 'b-1',
    kingdomId: 'k-1',
    armyTypeId: 'at-1',
    currentHP: 100,
    maxHP: 100,
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

  it('renders "Castle" text when tile.isCastle is true', () => {
    render(
      <HexTooltip
        tile={makeTile({ isCastle: true })}
        kingdom={null}
        armies={[]}
        position={{ x: 100, y: 100 }}
      />,
    );
    expect(screen.getByText('Castle')).toBeTruthy();
  });

  it('does not render "Castle" when tile.isCastle is false', () => {
    render(
      <HexTooltip
        tile={makeTile({ isCastle: false })}
        kingdom={null}
        armies={[]}
        position={{ x: 100, y: 100 }}
      />,
    );
    expect(screen.queryByText('Castle')).toBeNull();
  });

  it('renders building names when tile has buildings', () => {
    const tile = makeTile({
      buildings: [
        { id: 'b-1', buildingTypeId: 'bt-1', buildingCode: 'barracks', buildingName: 'Barracks' },
        { id: 'b-2', buildingTypeId: 'bt-2', buildingCode: 'farm', buildingName: 'Farm' },
      ],
    });
    render(
      <HexTooltip tile={tile} kingdom={null} armies={[]} position={{ x: 100, y: 100 }} />,
    );
    expect(screen.getByText('Barracks')).toBeTruthy();
    expect(screen.getByText('Farm')).toBeTruthy();
  });

  it('renders army roster count', () => {
    render(
      <HexTooltip
        tile={makeTile()}
        kingdom={null}
        armies={[makeArmy(), makeArmy({ id: 'a-2' })]}
        position={{ x: 100, y: 100 }}
      />,
    );
    expect(screen.getByText('2 armies in roster')).toBeTruthy();
  });

  it('renders Panel component with border class', () => {
    const { container } = render(
      <HexTooltip tile={makeTile()} kingdom={null} armies={[]} position={{ x: 100, y: 100 }} />,
    );
    const panel = container.querySelector('.border-bronze-700');
    expect(panel).toBeTruthy();
  });
});
