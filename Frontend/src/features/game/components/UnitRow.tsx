import { useState } from 'react';
import type { ComponentType, SVGProps } from 'react';
import type { ArmyTypeRef } from '../types/military-types';
import { GoldIcon, FoodIcon, StoneIcon, ManaIcon } from '@/assets/icons';
import { Button } from '@/shared/ui/Button';
import { QuantityStepper } from './QuantityStepper';
import { ApCostBadge, useCanAffordAp } from './ApCostBadge';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

const RESOURCE_ICONS: Record<string, ComponentType<IconProps>> = {
  Gold: GoldIcon,
  Food: FoodIcon,
  Stone: StoneIcon,
  Mana: ManaIcon,
};

const ARMY_COST_RESOURCE_MAP: { key: keyof ArmyTypeRef; resource: string }[] = [
  { key: 'trainingCostGold', resource: 'Gold' },
  { key: 'trainingCostFood', resource: 'Food' },
  { key: 'trainingCostStone', resource: 'Stone' },
  { key: 'trainingCostMana', resource: 'Mana' },
];

interface UnitRowProps {
  armyType: ArmyTypeRef;
  canAfford: boolean;
  insufficientResources: string[];
  maxAffordable: number;
  onTrain: (armyTypeId: string, quantity: number) => void;
}

export function UnitRow({
  armyType,
  canAfford,
  insufficientResources,
  maxAffordable,
  onTrain,
}: UnitRowProps) {
  const [quantity, setQuantity] = useState(1);
  const canAffordAp = useCanAffordAp(1);
  const disabled = !canAfford || !canAffordAp;

  const rowClasses = [
    'px-3 py-2 rounded border transition-colors',
    !disabled
      ? 'border-transparent hover:bg-ash-700'
      : 'opacity-50 pointer-events-none border-transparent',
  ].join(' ');

  return (
    <div className={rowClasses}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-1.5">
          <span className="text-parchment-100 font-medium text-sm">{armyType.name}</span>
          <ApCostBadge cost={1} />
        </div>
        <span className="text-ember-300 text-xs">{armyType.attack} atk</span>
      </div>

      <div className="flex items-center gap-1.5 mt-1">
        {ARMY_COST_RESOURCE_MAP.map(({ key, resource }) => {
          const value = armyType[key] as number;
          if (value <= 0) return null;
          const Icon = RESOURCE_ICONS[resource];
          if (!Icon) return null;
          const isInsufficient = insufficientResources.includes(resource);
          return (
            <span
              key={resource}
              className={`flex items-center gap-0.5 text-xs ${isInsufficient ? 'text-ember-400' : 'text-parchment-300'}`}
            >
              <Icon size={14} />
              <span>{value}</span>
            </span>
          );
        })}
      </div>

      {!disabled && (
        <div className="mt-2 space-y-1.5">
          <QuantityStepper
            value={quantity}
            min={1}
            max={maxAffordable}
            onChange={setQuantity}
          />
          <Button
            variant="primary"
            size="sm"
            onClick={() => {
              onTrain(armyType.id, quantity);
              setQuantity(1);
            }}
          >
            Train {quantity}
          </Button>
        </div>
      )}
    </div>
  );
}
