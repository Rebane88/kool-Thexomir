import { apiFetch } from '@/lib/api-client';
import type { ProblemDetails } from '@/shared/types/api';
import type { CreateLobbyResponse, LobbyResponse } from './lobby-types';

export async function createLobby(
  maxPlayers: number,
  winCondition: number,
): Promise<CreateLobbyResponse> {
  const res = await apiFetch('/lobby', {
    method: 'POST',
    body: JSON.stringify({ maxPlayers, winCondition }),
  });

  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to create lobby');
  }

  return res.json();
}

export async function joinLobby(
  inviteCode: string,
): Promise<LobbyResponse> {
  const res = await apiFetch('/lobby/join', {
    method: 'POST',
    body: JSON.stringify({ inviteCode }),
  });

  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to join lobby');
  }

  return res.json();
}

export async function getLobby(id: string): Promise<LobbyResponse> {
  const res = await apiFetch(`/lobby/${id}`);

  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to get lobby');
  }

  return res.json();
}

export async function selectFaction(
  lobbyId: string,
  factionTypeId: string,
): Promise<void> {
  const res = await apiFetch(`/lobby/${lobbyId}/faction/${factionTypeId}`, {
    method: 'POST',
    body: JSON.stringify({}),
  });

  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to select faction');
  }
}

export async function leaveLobby(lobbyId: string): Promise<void> {
  const res = await apiFetch(`/lobby/${lobbyId}`, {
    method: 'DELETE',
  });

  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to leave lobby');
  }
}

export async function startGame(lobbyId: string): Promise<void> {
  const res = await apiFetch(`/lobby/${lobbyId}/start`, {
    method: 'POST',
    body: JSON.stringify({}),
  });

  if (!res.ok) {
    const problem: ProblemDetails = await res.json();
    throw new Error(problem.detail || 'Failed to start game');
  }
}
