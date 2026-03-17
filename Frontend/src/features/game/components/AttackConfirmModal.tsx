import { Modal } from '@/shared/ui/Modal';
import { Button } from '@/shared/ui/Button';
import type { Army } from '../types/military-types';

interface AttackConfirmModalProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  attackerArmy: Army;
  defenderArmy: Army | null;
  attackerKingdomName: string;
  defenderKingdomName: string;
}

export function AttackConfirmModal({
  open,
  onClose,
  onConfirm,
  attackerArmy,
  defenderArmy,
  attackerKingdomName,
  defenderKingdomName,
}: AttackConfirmModalProps) {
  return (
    <Modal open={open} onClose={onClose}>
      <h2 className="text-parchment-100 font-heading text-lg font-semibold">
        Confirm Attack
      </h2>

      <div className="mt-3 flex gap-6">
        {/* Attacker */}
        <div className="flex-1">
          <h3 className="text-gold-400 font-heading text-sm font-semibold mb-1">
            {attackerKingdomName}
          </h3>
          <ul>
            {attackerArmy.units.map((unit) => (
              <li key={unit.unitTypeId} className="text-parchment-200 text-sm">
                {unit.quantity}x {unit.unitTypeName}
              </li>
            ))}
          </ul>
        </div>

        {/* Defender */}
        <div className="flex-1">
          <h3 className="text-ember-400 font-heading text-sm font-semibold mb-1">
            {defenderKingdomName}
          </h3>
          {defenderArmy ? (
            <ul>
              {defenderArmy.units.map((unit) => (
                <li key={unit.unitTypeId} className="text-parchment-200 text-sm">
                  {unit.quantity}x {unit.unitTypeName}
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-parchment-200 text-sm">Unknown forces</p>
          )}
        </div>
      </div>

      <div className="border-t border-bronze-700 my-3" />

      <div className="flex gap-3 justify-end">
        <Button variant="secondary" onClick={onClose}>
          Cancel
        </Button>
        <Button variant="primary" onClick={onConfirm}>
          Attack
        </Button>
      </div>
    </Modal>
  );
}
