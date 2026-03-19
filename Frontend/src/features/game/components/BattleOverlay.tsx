import { useGameStore } from '../game-store';
import type { BattleStep } from '../types/enums';
import { BattleSelectStep } from './BattleSelectStep';
import { BattleRevealStep } from './BattleRevealStep';
import { BattleLineupStep } from './BattleLineupStep';
import { BattleResolveStep } from './BattleResolveStep';

const STEPS: BattleStep[] = ['SelectArmies', 'RevealArmies', 'SetLineup', 'Resolve'];

const STEP_LABELS: Record<BattleStep, string> = {
  DeclareAttack: 'Declare',
  SelectArmies: 'Select',
  RevealArmies: 'Reveal',
  SetLineup: 'Lineup',
  Resolve: 'Resolve',
};

function isComplete(current: BattleStep | null, step: BattleStep): boolean {
  if (!current) return false;
  return STEPS.indexOf(step) < STEPS.indexOf(current);
}

function isActive(current: BattleStep | null, step: BattleStep): boolean {
  return current === step;
}

export function BattleOverlay() {
  const activeBattle = useGameStore((s) => s.activeBattle);
  const declaredAttacks = useGameStore((s) => s.declaredAttacks);
  const myKingdomId = useGameStore((s) => s.myKingdomId);

  if (!activeBattle) return null;

  const isSpectator = myKingdomId !== null && !declaredAttacks.some(
    (da) => da.attackerKingdomId === myKingdomId || da.defenderKingdomId === myKingdomId,
  );

  return (
    <div className="fixed inset-0 z-50 bg-black/80 flex flex-col">
      {/* Step progress indicator */}
      <div className="flex items-center justify-center gap-2 py-4 border-b border-ash-700">
        {STEPS.map((step, i) => (
          <div key={step} className="flex items-center gap-2">
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-heading
                ${isActive(activeBattle, step)
                  ? 'bg-gold-500 text-ash-900'
                  : isComplete(activeBattle, step)
                    ? 'bg-green-600 text-white'
                    : 'bg-ash-700 text-parchment-500'
                }`}
            >
              {i + 1}
            </div>
            <span
              className={`text-sm font-heading ${
                isActive(activeBattle, step) ? 'text-gold-400' : 'text-parchment-500'
              }`}
            >
              {STEP_LABELS[step]}
            </span>
            {i < STEPS.length - 1 && <div className="w-8 h-0.5 bg-ash-600" />}
          </div>
        ))}
      </div>

      {/* Step content */}
      <div className="flex-1 overflow-y-auto">
        {isSpectator ? (
          <div className="flex items-center justify-center h-full">
            <p className="text-parchment-400 text-sm animate-pulse">Watching battle...</p>
          </div>
        ) : (
          <>
            {activeBattle === 'SelectArmies' && <BattleSelectStep />}
            {activeBattle === 'RevealArmies' && <BattleRevealStep />}
            {activeBattle === 'SetLineup' && <BattleLineupStep />}
            {activeBattle === 'Resolve' && <BattleResolveStep />}
          </>
        )}
      </div>
    </div>
  );
}
