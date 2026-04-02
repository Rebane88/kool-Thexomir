import { FACTION_VISUALS } from './faction-constants';
import type { FactionAvailabilityDto } from './lobby-types';

interface FactionCardProps {
  faction: FactionAvailabilityDto;
  isSelected: boolean;
  isTaken: boolean;
  takenByName?: string;
  onSelect: (factionTypeId: string) => void;
  disabled?: boolean;
}

/** Generate a short human-readable bonus summary from modifier values. */
function buildBonusSummary(faction: FactionAvailabilityDto): string {
  const parts: string[] = [];

  if (faction.attackModifier !== 1) {
    const pct = Math.round((faction.attackModifier - 1) * 100);
    parts.push(`ATK ${pct > 0 ? '+' : ''}${pct}%`);
  }
  if (faction.hpModifier !== 1) {
    const pct = Math.round((faction.hpModifier - 1) * 100);
    parts.push(`HP ${pct > 0 ? '+' : ''}${pct}%`);
  }
  if (faction.initiativeModifier !== 1) {
    const pct = Math.round((faction.initiativeModifier - 1) * 100);
    parts.push(`Initiative ${pct > 0 ? '+' : ''}${pct}%`);
  }
  if (faction.chipDamageModifier !== 1) {
    const pct = Math.round((faction.chipDamageModifier - 1) * 100);
    parts.push(`Chip ${pct > 0 ? '+' : ''}${pct}%`);
  }
  if (faction.resourceProductionModifier !== 1) {
    const pct = Math.round((faction.resourceProductionModifier - 1) * 100);
    parts.push(`Resources ${pct > 0 ? '+' : ''}${pct}%`);
  }
  if (faction.buildingCostModifier !== 1) {
    const pct = Math.round((faction.buildingCostModifier - 1) * 100);
    parts.push(`Buildings ${pct > 0 ? '+' : ''}${pct}%`);
  }
  if (faction.trainingCostModifier !== 1) {
    const pct = Math.round((faction.trainingCostModifier - 1) * 100);
    parts.push(`Training ${pct > 0 ? '+' : ''}${pct}%`);
  }
  if (faction.actionPointModifier !== 0) {
    parts.push(`+${faction.actionPointModifier} AP`);
  }
  if (faction.healRateModifier !== 1) {
    const pct = Math.round((faction.healRateModifier - 1) * 100);
    parts.push(`Heal ${pct > 0 ? '+' : ''}${pct}%`);
  }

  return parts.join(' \u00b7 ');
}

export function FactionCard({ faction, isSelected, isTaken, takenByName, onSelect, disabled }: FactionCardProps) {
  const visuals = FACTION_VISUALS[faction.factionTypeId.toLowerCase()];
  if (!visuals) return null;

  const { color, Icon } = visuals;
  const bonusSummary = buildBonusSummary(faction);

  return (
    <button
      type="button"
      onClick={() => !isTaken && !disabled && onSelect(faction.factionTypeId)}
      disabled={isTaken || disabled}
      className={`
        relative text-left p-4 border-2 transition-all w-full
        ${isSelected ? 'border-gold-500 shadow-ember' : 'border-bronze-700 hover:border-bronze-500'}
        ${isTaken ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
        bg-ash-800
      `}
      aria-pressed={isSelected}
    >
      <div className="flex items-start gap-3">
        <div className={`w-10 h-10 ${color} rounded-sm flex items-center justify-center shrink-0`}>
          <Icon size={20} className="text-parchment-200" />
        </div>
        <div className="min-w-0">
          <div className="font-heading font-bold text-parchment-200 text-sm tracking-wide">{faction.name}</div>
          <div className="text-parchment-400 text-xs mt-0.5">{bonusSummary}</div>
        </div>
      </div>

      {isTaken && takenByName && (
        <div className="absolute inset-0 bg-ash-950/60 flex items-center justify-center">
          <span className="text-parchment-400 text-xs font-heading">{takenByName}</span>
        </div>
      )}
    </button>
  );
}
