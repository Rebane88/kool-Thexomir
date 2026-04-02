import { apiFetch } from '@/lib/api-client';
import type { ProblemDetails } from '@/shared/types/api';
import type { BuildingTypeRef } from './types/building-types';
import type { ArmyTypeRef } from './types/military-types';
import type { ArmyReveal } from './types/event-types';

// === Turn ===

export async function endTurn(gameId: string): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/end-turn`, {
    method: 'POST',
    body: JSON.stringify({}),
  });
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to end turn');
  }
  // Response body discarded -- TurnAdvanced SignalR event is authoritative
}

// === Building ===

export async function fetchBuildingTypes(gameId: string): Promise<BuildingTypeRef[]> {
  const res = await apiFetch(`/game/${gameId}/building-types`);
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to fetch building types');
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
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to place building');
  }
  // Response body discarded -- BuildingPlaced SignalR event is authoritative
}

// === Army ===

export async function fetchArmyTypes(gameId: string): Promise<ArmyTypeRef[]> {
  const res = await apiFetch(`/game/${gameId}/army-types`);
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to fetch army types');
  }
  return res.json();
}

export async function trainArmy(
  gameId: string,
  request: { buildingId: string; armyTypeId: string },
): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/train`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to train army');
  }
  // Response body discarded -- ArmyTrained SignalR event is authoritative
}

// === Slot Machine ===

export async function spinSlotMachine(gameId: string): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/spin`, {
    method: 'POST',
    body: JSON.stringify({}),
  });
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to spin slot machine');
  }
  // Response body discarded -- SlotMachineSpun SignalR event is authoritative
}

// === Combat ===

export async function declareAttack(
  gameId: string,
  request: { targetTileId: string; riskedTileId: string },
): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/declare-attack`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to declare attack');
  }
  // Response body discarded -- AttackDeclared SignalR event is authoritative
}

export async function selectArmies(
  gameId: string,
  request: { declaredAttackId: string; armyIds: string[] },
): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/select-armies`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to select armies');
  }
  // Response body discarded -- ArmiesSelected SignalR event is authoritative
}

export async function revealArmies(
  gameId: string,
  declaredAttackId: string,
): Promise<ArmyReveal> {
  const res = await apiFetch(`/game/${gameId}/reveal-armies/${declaredAttackId}`);
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to reveal armies');
  }
  return res.json();
}

export async function setLineup(
  gameId: string,
  request: { declaredAttackId: string; armyIdsInOrder: string[] },
): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/set-lineup`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to set lineup');
  }
  // Response body discarded -- LineupSet SignalR event is authoritative
}

// === Abandon ===

export async function abandonGame(gameId: string): Promise<void> {
  const res = await apiFetch(`/game/${gameId}/abandon`, {
    method: 'POST',
    body: JSON.stringify({}),
  });
  if (!res.ok) {
    const p: ProblemDetails = await res.json();
    throw new Error(p.detail || 'Failed to abandon game');
  }
}
