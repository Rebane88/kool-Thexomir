import { useGameStore } from '../game-store';
import { getKingdomColor } from '../canvas/hex-renderer';
import { Panel } from '@/shared/ui/Panel';
import { Badge } from '@/shared/ui/Badge';
import { FACTION_METADATA } from '../../lobby/faction-constants';

interface StandingsProps {
  open: boolean;
  onClose: () => void;
}

export function Standings({ open, onClose }: StandingsProps) {
  const kingdoms = useGameStore((s) => s.kingdoms);
  const tiles = useGameStore((s) => s.tiles);

  if (!open) return null;

  const entries = [...kingdoms.values()].map((k) => {
    const tileCount = [...tiles.values()].filter((t) => t.kingdomId === k.id).length;
    return { kingdom: k, tileCount };
  });

  const active = entries
    .filter((e) => e.kingdom.status !== 'Defeated')
    .sort((a, b) => b.tileCount - a.tileCount || a.kingdom.name.localeCompare(b.kingdom.name));
  const fallen = entries
    .filter((e) => e.kingdom.status === 'Defeated')
    .sort((a, b) => a.kingdom.name.localeCompare(b.kingdom.name));

  return (
    <Panel className="absolute top-14 right-4 z-30 w-64 p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-heading text-gold-400 text-sm font-bold">Standings</h3>
        <button onClick={onClose} className="text-parchment-400 hover:text-parchment-200 text-xs px-1" aria-label="Close standings">X</button>
      </div>

      {/* Active Kingdoms */}
      <div className="mb-3">
        <div className="flex items-center gap-1.5 mb-2">
          <span className="text-gold-400 text-xs">&#x1F6E1;</span>
          <span className="font-heading text-gold-400 text-xs tracking-wide uppercase">Active Kingdoms</span>
        </div>
        <div className="space-y-1.5">
          {active.map((entry, index) => {
            const meta = entry.kingdom.factionTypeId
              ? FACTION_METADATA[entry.kingdom.factionTypeId.toLowerCase()]
              : undefined;
            return (
              <div key={entry.kingdom.id} className="flex items-center gap-2 text-xs">
                <span className="text-parchment-500 w-4">{index + 1}.</span>
                {meta?.crestImage ? (
                  <img src={meta.crestImage} alt="" className="w-5 h-5 object-contain flex-shrink-0" />
                ) : (
                  <span className="inline-block w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: getKingdomColor(kingdoms, entry.kingdom.id) }} />
                )}
                <span className="text-parchment-200 truncate flex-1">{entry.kingdom.name}</span>
                <span className="text-parchment-400 tabular-nums">{entry.tileCount} tiles</span>
              </div>
            );
          })}
          {active.length === 0 && (
            <span className="text-parchment-500 text-xs italic">None</span>
          )}
        </div>
      </div>

      {/* Fallen Kingdoms */}
      {fallen.length > 0 && (
        <div>
          <div className="flex items-center gap-1.5 mb-2">
            <span className="text-parchment-500 text-xs">&#x2620;</span>
            <span className="font-heading text-parchment-500 text-xs tracking-wide uppercase">Fallen Kingdoms</span>
          </div>
          <div className="space-y-1.5">
            {fallen.map((entry) => {
              const meta = entry.kingdom.factionTypeId
                ? FACTION_METADATA[entry.kingdom.factionTypeId.toLowerCase()]
                : undefined;
              return (
                <div key={entry.kingdom.id} className="flex items-center gap-2 text-xs opacity-50">
                  <span className="w-4" />
                  {meta?.crestImage ? (
                    <img src={meta.crestImage} alt="" className="w-5 h-5 object-contain flex-shrink-0 opacity-50" />
                  ) : (
                    <span className="inline-block w-2.5 h-2.5 rounded-full flex-shrink-0 opacity-50" style={{ backgroundColor: getKingdomColor(kingdoms, entry.kingdom.id) }} />
                  )}
                  <span className="text-parchment-400 truncate flex-1 line-through">{entry.kingdom.name}</span>
                  <Badge variant="danger">Eliminated</Badge>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </Panel>
  );
}
