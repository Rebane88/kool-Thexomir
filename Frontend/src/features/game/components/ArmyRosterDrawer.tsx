import { useState, useMemo } from 'react';
import { useGameStore } from '../game-store';
import { ArmyCard } from './ArmyCard';

export function ArmyRosterDrawer() {
  const [isOpen, setIsOpen] = useState(false);

  const armies = useGameStore((s) => s.armies);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const armyTypes = useGameStore((s) => s.armyTypes);

  const myArmies = useMemo(() => {
    if (!myKingdomId) return [];
    return [...armies.values()]
      .filter((a) => a.kingdomId === myKingdomId)
      .map((a) => ({
        army: a,
        type: armyTypes.find((t) => t.id === a.armyTypeId),
      }))
      .filter((item): item is { army: typeof item.army; type: NonNullable<typeof item.type> } =>
        item.type !== undefined,
      );
  }, [armies, myKingdomId, armyTypes]);

  const countLabel = myArmies.length === 0 ? 'No armies' : `${myArmies.length} ${myArmies.length === 1 ? 'Army' : 'Armies'}`;

  return (
    <div
      className="fixed bottom-0 left-0 right-0 z-30 transition-transform duration-300 ease-out"
      style={{ transform: isOpen ? 'translateY(0)' : 'translateY(calc(100% - 40px))' }}
    >
      {/* Collapsed bar / header row */}
      <div
        className="bg-ash-900 border-t border-bronze-700 px-4 py-1.5 flex items-center justify-between cursor-pointer"
        onClick={() => setIsOpen((o) => !o)}
      >
        <div className="flex items-center gap-2">
          <span className="text-parchment-300 text-sm font-heading">Army Roster</span>
          <span className="bg-ash-700 text-parchment-400 text-xs px-1.5 py-0.5 rounded">
            {countLabel}
          </span>
        </div>
        {/* Chevron icon */}
        <svg viewBox="0 0 24 24" className={`w-4 h-4 text-parchment-400 transition-transform duration-300 ${isOpen ? '' : 'rotate-180'}`}>
          <path d="M18 15l-6-6-6 6" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>

      {/* Expanded army list */}
      <div className="bg-ash-900 border-t border-ash-600 max-h-[200px] overflow-y-hidden">
        <div className="flex gap-3 px-4 pb-3 pt-2 overflow-x-auto">
          {myArmies.length === 0 ? (
            <div className="text-parchment-500 text-sm py-2">No armies trained yet</div>
          ) : (
            myArmies.map(({ army, type }) => (
              <div key={army.id} className="flex-shrink-0 w-36">
                <ArmyCard army={army} armyType={type} compact={false} />
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
