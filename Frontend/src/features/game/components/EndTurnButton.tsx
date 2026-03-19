import { useState, useEffect } from 'react';
import { Button } from '@/shared/ui/Button';
import { endTurn } from '../game-api';
import { useGameStore } from '../game-store';
import type { GamePhase } from '../types/enums';

const PHASE_LABELS: Record<GamePhase, string> = {
  Action: 'End Actions',
  Battle: 'End Battle',
  Income: 'End Turn',
  RoundEnd: 'Next Round',
};

export function EndTurnButton() {
  const gameId = useGameStore((s) => s.gameId);
  const currentPhase = useGameStore((s) => s.currentPhase);
  const isMyTurn = useGameStore(
    (s) => s.myKingdomId !== null && s.currentTurnKingdomId === s.myKingdomId,
  );

  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset submitting state when a new turn starts for the player
  useEffect(() => {
    if (isMyTurn) setIsSubmitting(false);
  }, [isMyTurn]);

  const label = currentPhase ? PHASE_LABELS[currentPhase] : 'End Turn';

  async function handleEndTurn() {
    if (!gameId || !isMyTurn || isSubmitting) return;
    setIsSubmitting(true);
    try {
      await endTurn(gameId);
      // Do NOT re-enable on success -- TurnAdvanced event will flip isMyTurn to false
    } catch (err) {
      setIsSubmitting(false);
      console.error('Failed to end turn:', err);
    }
  }

  return (
    <Button
      variant="primary"
      size="sm"
      disabled={!isMyTurn || isSubmitting}
      onClick={handleEndTurn}
    >
      {label}
    </Button>
  );
}
