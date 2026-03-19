import { Button } from '@/shared/ui/Button';
import { useGameStore } from '../game-store';
import { useAnimationStore } from '../animation-store';
import { spinSlotMachine } from '../game-api';

const SPIN_COST_GOLD = 10;

export function GambleButton() {
  const gameId = useGameStore((s) => s.gameId);
  const currentPhase = useGameStore((s) => s.currentPhase);
  const actionPoints = useGameStore((s) => s.actionPoints);
  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const currentTurnKingdomId = useGameStore((s) => s.currentTurnKingdomId);
  const kingdoms = useGameStore((s) => s.kingdoms);
  const spinning = useAnimationStore((s) => s.slotSpinning);

  const isMyTurn = myKingdomId !== null && currentTurnKingdomId === myKingdomId;

  // Only visible during Action phase on player's turn
  if (currentPhase !== 'Action' || !isMyTurn) return null;

  const myKingdom = myKingdomId ? kingdoms.get(myKingdomId) : undefined;
  const gold = myKingdom?.resources.Gold ?? 0;
  const canAfford = gold >= SPIN_COST_GOLD && (actionPoints ?? 0) >= 1;

  async function handleGamble() {
    if (!gameId || spinning) return;
    useAnimationStore.getState().startSlotSpin();
    try {
      await spinSlotMachine(gameId);
    } catch (err) {
      useAnimationStore.getState().clearSlot();
      console.error('Spin failed:', err);
    }
  }

  return (
    <Button
      variant="secondary"
      size="sm"
      disabled={spinning || !canAfford}
      onClick={handleGamble}
      className="border-gold-400/40 text-gold-300"
    >
      Gamble ({SPIN_COST_GOLD}g)
    </Button>
  );
}
