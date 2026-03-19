import type { Army, ArmyTypeRef } from '../types/military-types';
import { HpBar } from './HpBar';

interface ArmyCardProps {
  army: Army;
  armyType: ArmyTypeRef;
  selected?: boolean;
  onClick?: () => void;
  compact?: boolean;
}

export function ArmyCard({ army, armyType, selected, onClick, compact }: ArmyCardProps) {
  const borderClass = selected
    ? 'border-gold-500 shadow-ember'
    : 'border-ash-600 hover:border-bronze-500';

  const padding = compact ? 'p-1.5' : 'p-2';

  return (
    <div
      className={`bg-ash-800/80 border rounded-lg ${padding} cursor-pointer transition-all ${borderClass}`}
      onClick={onClick}
    >
      <div className="font-heading text-sm text-parchment-200 mb-1 truncate">
        {armyType.name}
      </div>

      <HpBar currentHP={army.currentHP} maxHP={army.maxHP} className="mb-1" />

      <div className="text-xs text-parchment-400 space-y-0.5">
        <div>ATK: {armyType.attack}</div>
        <div>INIT: {armyType.initiative}</div>
      </div>

      {!compact && armyType.situationalBonusStat && (
        <div className="mt-1">
          <span className="bg-amber-900/50 text-amber-300 text-[10px] px-1.5 py-0.5 rounded">
            {armyType.situationalBonusCondition}: +{armyType.situationalBonusValue} {armyType.situationalBonusStat}
          </span>
        </div>
      )}
    </div>
  );
}
