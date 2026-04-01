import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button';
import { endTurn } from '../game-api';
import { useGameStore } from '../game-store';
import type { GamePhase } from '../types/enums';

export function EndTurnButton() {
  const { t } = useTranslation();
  const gameId = useGameStore((s) => s.gameId);
  const currentPhase = useGameStore((s) => s.currentPhase);
  const isMyTurn = useGameStore(
    (s) => s.myKingdomId !== null && s.currentTurnKingdomId === s.myKingdomId,
  );

  const roundNumber = useGameStore((s) => s.roundNumber);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset submitting state when turn or round changes
  useEffect(() => {
    if (isMyTurn) setIsSubmitting(false);
  }, [isMyTurn, roundNumber]);

  const PHASE_LABEL_KEYS: Record<GamePhase, string> = {
    Action: 'game.endActions',
    Battle: 'game.endBattle',
    Income: 'game.endTurn',
    RoundEnd: 'game.nextRound',
  };

  const label = currentPhase ? t(PHASE_LABEL_KEYS[currentPhase]) : t('game.endTurn');

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
