import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { CombatResultModal } from './CombatResultModal';
import type { CombatResolvedEvent } from '../types/event-types';

function makeMockResult(
  overrides: Partial<CombatResolvedEvent> = {},
): CombatResolvedEvent {
  return {
    battleId: 'b-1',
    tileId: 't-1',
    attackerKingdomId: 'k-1',
    defenderKingdomId: 'k-2',
    winnerKingdomId: 'k-1',
    tileCaptured: true,
    attackerStrength: 30,
    defenderStrength: 15,
    attackerCasualties: [
      { unitTypeId: 'w', unitTypeName: 'Warrior', before: 10, lost: 3 },
    ],
    defenderCasualties: [
      { unitTypeId: 'a', unitTypeName: 'Archer', before: 5, lost: 5 },
    ],
    gameOver: null,
    ...overrides,
  };
}

describe('CombatResultModal', () => {
  it('shows Victory when player won', () => {
    render(
      <CombatResultModal
        open={true}
        onClose={() => {}}
        result={makeMockResult()}
        myKingdomId="k-1"
        attackerKingdomName="Kingdom A"
        defenderKingdomName="Kingdom B"
      />,
    );
    expect(screen.getByText('Victory!')).toBeDefined();
  });

  it('shows Defeat when player lost', () => {
    render(
      <CombatResultModal
        open={true}
        onClose={() => {}}
        result={makeMockResult()}
        myKingdomId="k-2"
        attackerKingdomName="Kingdom A"
        defenderKingdomName="Kingdom B"
      />,
    );
    expect(screen.getByText('Defeat!')).toBeDefined();
  });

  it('shows Draw when winnerKingdomId is null', () => {
    render(
      <CombatResultModal
        open={true}
        onClose={() => {}}
        result={makeMockResult({ winnerKingdomId: null })}
        myKingdomId="k-1"
        attackerKingdomName="Kingdom A"
        defenderKingdomName="Kingdom B"
      />,
    );
    expect(screen.getByText('Draw')).toBeDefined();
  });

  it('displays attacker and defender casualties', () => {
    render(
      <CombatResultModal
        open={true}
        onClose={() => {}}
        result={makeMockResult()}
        myKingdomId="k-1"
        attackerKingdomName="Kingdom A"
        defenderKingdomName="Kingdom B"
      />,
    );
    expect(screen.getByText('Warrior')).toBeDefined();
    expect(screen.getByText('Archer')).toBeDefined();
    // Attacker: 10 -> 7 (-3)
    expect(screen.getByText(/10/)).toBeDefined();
    expect(screen.getByText(/(-3)/)).toBeDefined();
  });

  it('shows tile captured status', () => {
    render(
      <CombatResultModal
        open={true}
        onClose={() => {}}
        result={makeMockResult({ tileCaptured: true })}
        myKingdomId="k-1"
        attackerKingdomName="Kingdom A"
        defenderKingdomName="Kingdom B"
      />,
    );
    expect(screen.getByText('Tile captured!')).toBeDefined();
  });

  it('calls onClose when Close button clicked', () => {
    const onClose = vi.fn();
    render(
      <CombatResultModal
        open={true}
        onClose={onClose}
        result={makeMockResult()}
        myKingdomId="k-1"
        attackerKingdomName="Kingdom A"
        defenderKingdomName="Kingdom B"
      />,
    );
    fireEvent.click(screen.getByText('Close'));
    expect(onClose).toHaveBeenCalled();
  });
});
