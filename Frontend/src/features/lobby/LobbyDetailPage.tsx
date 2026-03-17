import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router';
import { useAuthStore } from '@/features/auth/auth-store';
import { createGameHubConnection } from '@/lib/signalr-client';
import type * as signalR from '@microsoft/signalr';
import { Panel, Button, Badge, Select } from '@/shared/ui';
import { CrownIcon } from '@/assets/icons';
import { getLobby, selectFaction, leaveLobby, startGame } from './lobby-api';
import type { LobbyResponse } from './lobby-types';
import { WIN_CONDITION_LABELS, GAME_STATUS } from './lobby-types';

export function LobbyDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const userId = useAuthStore((s) => s.user?.id);

  const [lobby, setLobby] = useState<LobbyResponse | null>(null);
  const [connectionError, setConnectionError] = useState(false);
  const [factionError, setFactionError] = useState('');
  const [pendingFaction, setPendingFaction] = useState(false);
  const [isStarting, setIsStarting] = useState(false);
  const [isLeaving, setIsLeaving] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let active = true;
    let connection: signalR.HubConnection | null = null;

    const setup = async () => {
      // Initial load via REST (get current state)
      try {
        const data = await getLobby(id!);
        if (!active) return;
        setLobby(data);
        setConnectionError(false);
        if (data.status === GAME_STATUS.InProgress) {
          navigate(`/game/${data.id}`, { replace: true });
          return;
        }
      } catch {
        if (active) setConnectionError(true);
        return;
      }

      // Establish SignalR connection for real-time updates
      connection = createGameHubConnection(id!);

      connection.on('lobbyPlayerJoined', (lobby: LobbyResponse) => {
        if (active) {
          setLobby(lobby);
          setConnectionError(false);
        }
      });

      connection.on('lobbyPlayerLeft', (lobby: LobbyResponse) => {
        if (active) {
          setLobby(lobby);
          setConnectionError(false);
        }
      });

      connection.on('lobbyFactionSelected', (lobby: LobbyResponse) => {
        if (active) {
          setLobby(lobby);
          setConnectionError(false);
        }
      });

      connection.on('lobbyGameStarting', () => {
        if (active) {
          navigate(`/game/${id}`, { replace: true });
        }
      });

      connection.onclose(() => {
        if (active) setConnectionError(true);
      });

      connection.onreconnected(async () => {
        if (!active) return;
        setConnectionError(false);
        // Refresh lobby state after reconnect
        try {
          const data = await getLobby(id!);
          if (active) {
            setLobby(data);
            if (data.status === GAME_STATUS.InProgress) {
              navigate(`/game/${data.id}`, { replace: true });
            }
          }
        } catch {
          // ignore -- SignalR events will continue delivering updates
        }
      });

      connection.onreconnecting(() => {
        if (active) setConnectionError(true);
      });

      try {
        await connection.start();
        if (active) setConnectionError(false);
      } catch {
        if (active) setConnectionError(true);
      }
    };

    setup();

    return () => {
      active = false;
      connection?.stop();
    };
  }, [id, navigate]);

  const isHost = lobby?.hostUserId === userId;
  const currentPlayerFaction = lobby?.players.find(
    (p) => p.userId === userId,
  )?.factionTypeId;

  const allHaveFactions = lobby?.players.every(
    (p) => p.factionTypeId !== null,
  ) ?? false;
  const enoughPlayers = (lobby?.playerCount ?? 0) >= 2;
  const canStart = allHaveFactions && enoughPlayers;
  const startReason = !enoughPlayers
    ? 'Need at least 2 players'
    : !allHaveFactions
      ? 'Waiting for all players to select factions'
      : '';

  const handleCopy = async () => {
    if (!lobby) return;
    try {
      await navigator.clipboard.writeText(lobby.lobbyCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Clipboard API not available — no-op
    }
  };

  const handleFactionChange = async (factionTypeId: string) => {
    if (!factionTypeId || !lobby) return;
    setPendingFaction(true);
    setFactionError('');
    try {
      await selectFaction(lobby.id, factionTypeId);
    } catch (err) {
      setFactionError(
        err instanceof Error ? err.message : 'Faction already taken',
      );
    } finally {
      setPendingFaction(false);
    }
  };

  const handleStart = async () => {
    if (!lobby) return;
    setIsStarting(true);
    try {
      await startGame(lobby.id);
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to start game');
    } finally {
      setIsStarting(false);
    }
  };

  const handleLeave = async () => {
    if (!lobby) return;
    if (!window.confirm('Leave this lobby?')) return;
    setIsLeaving(true);
    try {
      await leaveLobby(lobby.id);
      navigate('/');
    } catch {
      setIsLeaving(false);
    }
  };

  return (
    <div className="flex-1 flex flex-col items-center justify-center px-4 py-8">
      <div className="max-w-xl w-full">
        {!lobby ? (
          <p className="text-parchment-400 text-center">Loading...</p>
        ) : (
          <>
            {connectionError && (
              <div className="bg-blood-600/20 border border-blood-600/30 text-blood-500 px-4 py-2 text-sm mb-4">
                Connection lost
              </div>
            )}

            <Panel className="p-6">
              <div className="text-center mb-6">
                <p className="text-parchment-400 text-xs uppercase tracking-widest mb-1 font-heading">
                  Invite Code
                </p>
                <div className="flex items-center justify-center gap-2">
                  <span className="text-3xl font-mono font-bold text-gold-500 tracking-[0.3em]">
                    {lobby.lobbyCode}
                  </span>
                  <button
                    onClick={handleCopy}
                    className="text-parchment-400 hover:text-gold-500 text-sm transition-colors"
                  >
                    {copied ? 'Copied!' : 'Copy'}
                  </button>
                </div>
              </div>

              <div className="flex justify-between text-parchment-400 text-sm mb-4 border-b border-bronze-700 pb-3">
                <span>
                  Win: {WIN_CONDITION_LABELS[lobby.winCondition] ?? 'Unknown'}
                </span>
                <span>
                  Players: {lobby.playerCount}/{lobby.maxPlayers}
                </span>
              </div>

              <div className="space-y-2 mb-4">
                <h2 className="text-parchment-200 text-sm font-heading font-medium mb-2 tracking-wide">
                  Players
                </h2>
                {lobby.players.map((player) => (
                  <div
                    key={player.kingdomId}
                    className="flex items-center justify-between bg-ash-700/50 border border-bronze-700/50 px-3 py-2"
                  >
                    <div className="flex items-center gap-2">
                      {player.isHost && (
                        <CrownIcon size={16} className="text-gold-500" />
                      )}
                      <span className="text-parchment-200">{player.userEmail}</span>
                    </div>
                    {player.factionName ? (
                      <Badge variant="success">{player.factionName}</Badge>
                    ) : (
                      <span className="text-parchment-400 text-sm italic">No faction</span>
                    )}
                  </div>
                ))}
              </div>

              {lobby.status === GAME_STATUS.Lobby && (
                <div className="mb-4">
                  <Select
                    id="factionSelect"
                    label="Select Faction"
                    value={currentPlayerFaction ?? ''}
                    onChange={(e) => handleFactionChange(e.target.value)}
                    disabled={pendingFaction}
                  >
                    <option value="">-- Select --</option>
                    {lobby.factions.map((f) => (
                      <option
                        key={f.factionTypeId}
                        value={f.factionTypeId}
                        disabled={!f.isAvailable}
                      >
                        {f.name}
                        {!f.isAvailable ? ' (Taken)' : ''}
                      </option>
                    ))}
                  </Select>
                  {factionError && (
                    <p className="text-blood-500 text-xs mt-1">{factionError}</p>
                  )}
                </div>
              )}

              <div className="flex gap-3 mt-4">
                <Button
                  onClick={handleLeave}
                  disabled={isLeaving}
                  variant="secondary"
                  className="flex-1"
                >
                  {isLeaving ? 'Leaving...' : 'Leave Lobby'}
                </Button>
                {isHost && (
                  <Button
                    onClick={handleStart}
                    disabled={!canStart || isStarting}
                    variant="primary"
                    className="flex-1"
                  >
                    {isStarting ? 'Starting...' : 'Start Game'}
                  </Button>
                )}
              </div>
              {isHost && !canStart && startReason && (
                <p className="text-parchment-400 text-xs mt-1 text-center">
                  {startReason}
                </p>
              )}
            </Panel>
          </>
        )}
      </div>
    </div>
  );
}
