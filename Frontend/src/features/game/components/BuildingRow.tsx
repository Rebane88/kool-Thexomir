import type { ComponentType, SVGProps } from 'react';
import type { BuildingTypeRef } from '../types/building-types';
import { GoldIcon, FoodIcon, WoodIcon, StoneIcon, ManaIcon } from '@/assets/icons';
import { ApCostBadge, useCanAffordAp } from './ApCostBadge';
import castlePng from '@/assets/images/building-castle.png';
import farmPng from '@/assets/images/building-farm.png';
import windmillPng from '@/assets/images/building-windmill.png';
import granaryPng from '@/assets/images/building-granary.png';
import lumberCampPng from '@/assets/images/building-lumber-camp.png';
import sawmillPng from '@/assets/images/building-sawmill.png';
import timberHallPng from '@/assets/images/building-timber-hall.png';
import quarryPng from '@/assets/images/building-quarry.png';
import masonPng from '@/assets/images/building-mason.png';
import stoneworksPng from '@/assets/images/building-stoneworks.png';
import marketPng from '@/assets/images/building-market.png';
import tradingPostPng from '@/assets/images/building-trading-post.png';
import bankPng from '@/assets/images/building-bank.png';
import shrinePng from '@/assets/images/building-shrine.png';
import wizardTowerPng from '@/assets/images/building-wizard-tower.png';
import arcaneSanctumPng from '@/assets/images/building-arcane-sanctum.png';
import barracksPng from '@/assets/images/building-barracks.png';
import stablesPng from '@/assets/images/building-stables.png';
import warAcademyPng from '@/assets/images/building-war-academy.png';

const BUILDING_CARD_IMAGES: Record<string, string> = {
  'Castle': castlePng,
  'Farm': farmPng,
  'Windmill': windmillPng,
  'Granary': granaryPng,
  'Lumber Camp': lumberCampPng,
  'Sawmill': sawmillPng,
  'Timber Hall': timberHallPng,
  'Quarry': quarryPng,
  'Mason': masonPng,
  'Stoneworks': stoneworksPng,
  'Market': marketPng,
  'Trading Post': tradingPostPng,
  'Bank': bankPng,
  'Shrine': shrinePng,
  'Wizard Tower': wizardTowerPng,
  'Arcane Sanctum': arcaneSanctumPng,
  'Barracks': barracksPng,
  'Stables': stablesPng,
  'War Academy': warAcademyPng,
};

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

const RESOURCE_ICONS: Record<string, ComponentType<IconProps>> = {
  Gold: GoldIcon,
  Food: FoodIcon,
  Wood: WoodIcon,
  Stone: StoneIcon,
  Mana: ManaIcon,
};

const COST_RESOURCE_MAP: { key: keyof BuildingTypeRef; resource: string }[] = [
  { key: 'goldCost', resource: 'Gold' },
  { key: 'woodCost', resource: 'Wood' },
  { key: 'stoneCost', resource: 'Stone' },
  { key: 'manaCost', resource: 'Mana' },
];

const YIELD_RESOURCE_MAP: { key: keyof BuildingTypeRef; resource: string }[] = [
  { key: 'goldYield', resource: 'Gold' },
  { key: 'foodYield', resource: 'Food' },
  { key: 'woodYield', resource: 'Wood' },
  { key: 'stoneYield', resource: 'Stone' },
  { key: 'manaYield', resource: 'Mana' },
];

interface BuildingRowProps {
  buildingType: BuildingTypeRef;
  canAfford: boolean;
  hasPrerequisite: boolean;
  insufficientResources: string[];
  isActive: boolean;
  onSelect: (typeId: string) => void;
}

export function BuildingRow({
  buildingType,
  canAfford,
  hasPrerequisite,
  insufficientResources,
  isActive,
  onSelect,
}: BuildingRowProps) {
  const canAffordAp = useCanAffordAp(1);
  const disabled = !canAfford || !hasPrerequisite || !canAffordAp;

  const rowClasses = [
    'px-3 py-2 rounded border transition-colors',
    disabled
      ? 'opacity-50 pointer-events-none border-transparent'
      : isActive
        ? 'bg-gold-500/20 border-gold-500 cursor-pointer'
        : 'hover:bg-ash-700 cursor-pointer border-transparent',
  ].join(' ');

  return (
    <div className={rowClasses} onClick={() => onSelect(buildingType.id)}>
      <div className="flex items-start gap-2">
        {/* Building art icon */}
        {BUILDING_CARD_IMAGES[buildingType.name] && (
          <img
            src={BUILDING_CARD_IMAGES[buildingType.name]}
            alt={buildingType.name}
            className="w-10 h-10 object-contain flex-shrink-0 rounded"
          />
        )}
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-1.5">
              <span className="text-parchment-100 font-medium text-sm">{buildingType.name}</span>
              <ApCostBadge cost={1} />
            </div>
            <div className="flex items-center gap-1.5">
              {YIELD_RESOURCE_MAP.map(({ key, resource }) => {
                const value = buildingType[key] as number;
                if (value <= 0) return null;
                const Icon = RESOURCE_ICONS[resource];
                return (
                  <span key={resource} className="flex items-center gap-0.5 text-green-400">
                    <Icon size={14} />
                    <span className="text-xs">{value}</span>
                  </span>
                );
              })}
            </div>
          </div>

          {buildingType.prerequisiteBuildingName && !hasPrerequisite && (
            <div className="text-xs text-ember-400 mt-0.5">
              Requires: {buildingType.prerequisiteBuildingName}
            </div>
          )}

          <div className="flex items-center gap-1.5 mt-1">
            {COST_RESOURCE_MAP.map(({ key, resource }) => {
              const value = buildingType[key] as number;
              if (value <= 0) return null;
              const Icon = RESOURCE_ICONS[resource];
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
        </div>
      </div>
    </div>
  );
}
