import { apiFetch } from '@/lib/api-client';
import type { ProblemDetails } from '@/shared/types/api';

export async function endTurn(gameId: string): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/end-turn`, {
    method: 'POST',
    body: JSON.stringify({}),
  });

  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to end turn');
  }
  // Response body intentionally discarded -- TurnAdvanced SignalR event is authoritative
}
