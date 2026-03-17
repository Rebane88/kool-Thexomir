import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { AttackConfirmModal } from './AttackConfirmModal';
import type { Army } from '../types/military-types';

const attackerArmy: Army = {
  id: 'a1',
  tileId: 't1',
  kingdomId: 'k1',
  units: [{ unitTypeId: 'w', unitTypeName: 'Warrior', quantity: 10 }],
};

const defenderArmy: Army = {
  id: 'a2',
  tileId: 't2',
  kingdomId: 'k2',
  units: [{ unitTypeId: 'a', unitTypeName: 'Archer', quantity: 5 }],
};

const defaultProps = {
  open: true,
  onClose: vi.fn(),
  onConfirm: vi.fn(),
  attackerArmy,
  defenderArmy,
  attackerKingdomName: 'Kingdom Alpha',
  defenderKingdomName: 'Kingdom Beta',
};

describe('AttackConfirmModal', () => {
  it('renders attacker and defender army compositions', () => {
    render(<AttackConfirmModal {...defaultProps} />);
    expect(screen.getByText('10x Warrior')).toBeDefined();
    expect(screen.getByText('5x Archer')).toBeDefined();
  });

  it("renders 'Unknown forces' when defenderArmy is null", () => {
    render(<AttackConfirmModal {...defaultProps} defenderArmy={null} />);
    expect(screen.getByText('Unknown forces')).toBeDefined();
  });

  it('calls onConfirm when Attack button clicked', () => {
    const onConfirm = vi.fn();
    render(<AttackConfirmModal {...defaultProps} onConfirm={onConfirm} />);
    fireEvent.click(screen.getByText('Attack'));
    expect(onConfirm).toHaveBeenCalledOnce();
  });

  it('calls onClose when Cancel button clicked', () => {
    const onClose = vi.fn();
    render(<AttackConfirmModal {...defaultProps} onClose={onClose} />);
    fireEvent.click(screen.getByText('Cancel'));
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('displays kingdom names', () => {
    render(<AttackConfirmModal {...defaultProps} />);
    expect(screen.getByText('Kingdom Alpha')).toBeDefined();
    expect(screen.getByText('Kingdom Beta')).toBeDefined();
  });
});
