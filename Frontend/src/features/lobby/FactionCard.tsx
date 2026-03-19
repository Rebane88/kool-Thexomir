import { FACTION_METADATA, type FactionMeta } from './faction-constants';

interface FactionCardProps {
  factionTypeId: string;
  isSelected: boolean;
  isTaken: boolean;
  takenByName?: string;
  onSelect: (factionTypeId: string) => void;
  disabled?: boolean;
}

export function FactionCard({ factionTypeId, isSelected, isTaken, takenByName, onSelect, disabled }: FactionCardProps) {
  const meta: FactionMeta | undefined = FACTION_METADATA[factionTypeId.toLowerCase()];
  if (!meta) return null;

  const { name, color, bonusSummary, Icon } = meta;

  return (
    <button
      type="button"
      onClick={() => !isTaken && !disabled && onSelect(factionTypeId)}
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
          <div className="font-heading font-bold text-parchment-200 text-sm tracking-wide">{name}</div>
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
