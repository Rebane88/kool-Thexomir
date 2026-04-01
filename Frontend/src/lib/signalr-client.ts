import * as signalR from '@microsoft/signalr';
import i18next from 'i18next';
import { useAuthStore } from '@/features/auth/auth-store';

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL ?? '';

export function createGameHubConnection(gameId: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${HUB_BASE_URL}/hubs/game?gameId=${gameId}`, {
      accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
      headers: { 'Accept-Language': i18next.language },
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}
