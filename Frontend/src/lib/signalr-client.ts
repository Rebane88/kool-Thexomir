import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/features/auth/auth-store';

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL ?? '';

export function createGameHubConnection(gameId: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${HUB_BASE_URL}/hubs/game?gameId=${gameId}`, {
      accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
    })
    .withAutomaticReconnect()
    .build();
}
