import { apiFetch } from '@/lib/api-client';
import type { ProblemDetails } from '@/shared/types/api';
import type { BuildingTypeRef } from './types/building-types';
import type { UnitTypeRef } from './types/military-types';

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

export async function fetchUnitTypes(gameId: string): Promise<UnitTypeRef[]> {
  const res = await apiFetch(`/game/${gameId}/unit-types`);
  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to fetch unit types');
  }
  return res.json();
}

export async function trainTroops(
  gameId: string,
  request: { buildingId: string; unitTypeId: string; quantity: number },
): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/train`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to train troops');
  }
  // Response body discarded -- TroopsTrained SignalR event is authoritative
}

export async function moveArmy(
  gameId: string,
  request: { armyId: string; targetTileId: string },
): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/move`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to move army');
  }
  // Response body discarded -- ArmyMoved SignalR event is authoritative
}

export async function attackTile(
  gameId: string,
  request: { attackerArmyId: string; defenderTileId: string },
): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/attack`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to attack');
  }
  // Response body discarded -- CombatResolved SignalR event is authoritative
}
