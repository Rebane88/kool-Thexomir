import { Tooltip } from '@/shared/ui/Tooltip';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';
import type { Army } from '../types/military-types';

interface HexTooltipProps {
  tile: Tile;
  kingdom: Kingdom | null;
  armies: Army[];
  position: { x: number; y: number };
}

const terrainDotColor: Record<string, string> = {
  plains: 'bg-green-500',
  forest: 'bg-green-700',
  mountain: 'bg-stone-500',
  desert: 'bg-amber-500',
  'magic grove': 'bg-purple-500',
};

function getTerrainDotClass(terrainName: string): string {
  const key = terrainName.toLowerCase();
  return terrainDotColor[key] ?? 'bg-parchment-400';
}

export function HexTooltip({ tile, kingdom, armies, position }: HexTooltipProps) {
  const hasBuildings = tile.buildings.length > 0;
  const hasArmies = armies.length > 0;

  return (
    <div className="pointer-events-none">
      <Tooltip position={position} className="p-2.5" style={{ minWidth: 180 }}>
        <div className="flex items-center gap-1.5 mb-0.5">
          <span className={`inline-block w-2 h-2 rounded-full flex-shrink-0 ${getTerrainDotClass(tile.terrainName)}`} />
          <span className="text-parchment-200 font-heading font-semibold text-sm">{tile.terrainName}</span>
        </div>
        <div className="text-parchment-400 text-xs">
          {kingdom ? kingdom.name : 'Unowned'}
        </div>
        {tile.isCastle && (
          <div className="text-gold-400 text-xs font-heading">Castle</div>
        )}

        {hasBuildings && (
          <>
            <div className="border-t border-bronze-700/50 mt-1.5 pt-1.5">
              {tile.buildings.map((b) => (
                <div key={b.id} className="text-xs text-parchment-300">
                  {b.buildingName}
                </div>
              ))}
            </div>
          </>
        )}

        {hasArmies && (
          <div className="border-t border-bronze-700/50 mt-1.5 pt-1.5">
            <div className="text-xs text-parchment-400">
              {armies.length} {armies.length === 1 ? 'army' : 'armies'} in roster
            </div>
          </div>
        )}
      </Tooltip>
    </div>
  );
}
