import { useState, useEffect } from 'react';
import { Button } from '@/shared/ui/Button';
import { endTurn } from '../game-api';
import { useGameStore } from '../game-store';

export function EndTurnButton() {
  const gameId = useGameStore((s) => s.gameId);
  const isMyTurn = useGameStore(
    (s) => s.myKingdomId !== null && s.currentTurnKingdomId === s.myKingdomId,
  );

  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset submitting state when a new turn starts for the player
  useEffect(() => {
    if (isMyTurn) setIsSubmitting(false);
  }, [isMyTurn]);

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
      End Turn
    </Button>
  );
}
