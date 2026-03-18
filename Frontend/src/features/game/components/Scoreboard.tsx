import { useGameStore } from '../game-store';
import { calculateKingdomScore } from '../score';
import { getKingdomColor } from '../canvas/hex-renderer';
import { Panel } from '@/shared/ui/Panel';
import { Badge } from '@/shared/ui/Badge';

interface ScoreboardProps {
  open: boolean;
  onClose: () => void;
}

export function Scoreboard({ open, onClose }: ScoreboardProps) {
  const kingdoms = useGameStore((s) => s.kingdoms);
  const tiles = useGameStore((s) => s.tiles);
  const armies = useGameStore((s) => s.armies);
  const buildingTypes = useGameStore((s) => s.buildingTypes);

  if (!open) return null;

  const entries = [...kingdoms.values()].map((k) => {
    const score = calculateKingdomScore(k.id, tiles, armies, buildingTypes);
    const tileCount = [...tiles.values()].filter((t) => t.kingdomId === k.id).length;
    return { kingdom: k, score, tileCount };
  });

  const active = entries
    .filter((e) => !e.kingdom.isEliminated)
    .sort((a, b) => b.score - a.score || a.kingdom.name.localeCompare(b.kingdom.name));

  const eliminated = entries
    .filter((e) => e.kingdom.isEliminated)
    .sort((a, b) => b.score - a.score || a.kingdom.name.localeCompare(b.kingdom.name));

  const sorted = [...active, ...eliminated];

  return (
    <Panel className="absolute top-14 right-4 z-30 w-64 p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-heading text-gold-400 text-sm font-bold">Standings</h3>
        <button
          onClick={onClose}
          className="text-parchment-400 hover:text-parchment-200 text-xs px-1"
          aria-label="Close scoreboard"
        >
          X
        </button>
      </div>
      <div className="space-y-1.5">
        {sorted.map((entry, index) => (
          <div
            key={entry.kingdom.id}
            className={`flex items-center gap-2 text-xs ${entry.kingdom.isEliminated ? 'opacity-50' : ''}`}
          >
            <span className="text-parchment-500 w-4">{index + 1}.</span>
            <span
              className="inline-block w-2.5 h-2.5 rounded-full flex-shrink-0"
              style={{ backgroundColor: getKingdomColor(kingdoms, entry.kingdom.id) }}
            />
            <span className="text-parchment-200 truncate flex-1">
              {entry.kingdom.name}
            </span>
            {entry.kingdom.isEliminated && (
              <Badge variant="danger">Eliminated</Badge>
            )}
            <span className="text-parchment-400 tabular-nums">{entry.tileCount}t</span>
            <span className="text-gold-400 font-bold tabular-nums w-6 text-right">{entry.score}</span>
          </div>
        ))}
      </div>
    </Panel>
  );
}
