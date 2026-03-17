import { apiFetch } from '@/lib/api-client';
import type { ProblemDetails } from '@/shared/types/api';
import type { BuildingTypeRef } from './types/building-types';

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

export async function fetchBuildingTypes(gameId: string): Promise<BuildingTypeRef[]> {
  const res = await apiFetch(`/game/${gameId}/building-types`);
  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to fetch building types');
  }
  return res.json();
}

export async function placeBuilding(
  gameId: string,
  request: { tileId: string; buildingTypeId: string },
): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/build`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to place building');
  }
  // Response body discarded -- BuildingPlaced SignalR event is authoritative
}
