import { Panel } from '@/shared/ui/Panel';
import type { Tile } from '../types/map-types';
import type { Kingdom } from '../types/kingdom-types';
import type { Army } from '../types/military-types';

interface HexTooltipProps {
  tile: Tile;
  kingdom: Kingdom | null;
  armies: Army[];
  position: { x: number; y: number };
}

export function HexTooltip({ tile, kingdom, armies, position }: HexTooltipProps) {
  return (
    <Panel
      variant="elevated"
      className="pointer-events-none"
      style={{
        position: 'absolute',
        left: position.x,
        top: position.y,
        zIndex: 50,
        minWidth: 160,
        padding: '8px 12px',
        fontSize: 13,
      }}
    >
      <div className="text-parchment-300 font-semibold">{tile.terrainName}</div>
      <div className="text-parchment-500 text-xs">{kingdom ? kingdom.name : 'Unowned'}</div>

      {tile.isCapital && <div className="text-xs text-gold-400">Capital</div>}

      {tile.buildings.length > 0 &&
        tile.buildings.map((b) => (
          <div key={b.id} className="text-xs text-parchment-400">
            {b.buildingName}
          </div>
        ))}

      {armies.length > 0 &&
        armies.map((army) =>
          army.units.map((u) => (
            <div key={`${army.id}-${u.unitTypeId}`} className="text-xs text-parchment-400">
              {u.unitTypeName}: {u.quantity}
            </div>
          )),
        )}
    </Panel>
  );
}
