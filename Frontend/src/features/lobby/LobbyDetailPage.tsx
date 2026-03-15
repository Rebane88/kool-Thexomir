import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router';
import { useAuthStore } from '@/features/auth/auth-store';
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
    const poll = async () => {
      try {
        const data = await getLobby(id!);
        if (!active) return;
        setLobby(data);
        setConnectionError(false);
        if (data.status === GAME_STATUS.InProgress) {
          navigate(`/game/${data.id}`, { replace: true });
        }
      } catch {
        if (active) setConnectionError(true);
      }
    };
    poll();
    const intervalId = setInterval(poll, 3000);
    return () => { active = false; clearInterval(intervalId); };
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
    <div className="min-h-screen bg-gray-900 flex items-center justify-center px-4">
      <div className="max-w-xl w-full bg-gray-800 rounded-lg p-6 shadow-lg">
        {!lobby ? (
          <p className="text-gray-400 text-center">Loading...</p>
        ) : (
          <>
            {connectionError && (
              <div className="bg-red-900/50 border border-red-600 text-red-200 px-4 py-2 rounded text-sm mb-4">
                Connection lost
              </div>
            )}

            <div className="text-center mb-6">
              <p className="text-gray-400 text-sm mb-1">Invite Code</p>
              <div className="flex items-center justify-center gap-2">
                <span className="text-3xl font-mono font-bold text-amber-500 tracking-widest">
                  {lobby.lobbyCode}
                </span>
                <button
                  onClick={handleCopy}
                  className="text-gray-400 hover:text-amber-500 text-sm"
                >
                  {copied ? 'Copied!' : 'Copy'}
                </button>
              </div>
            </div>

            <div className="flex justify-between text-gray-400 text-sm mb-4 border-b border-gray-700 pb-3">
              <span>
                Win: {WIN_CONDITION_LABELS[lobby.winCondition] ?? 'Unknown'}
              </span>
              <span>
                Players: {lobby.playerCount}/{lobby.maxPlayers}
              </span>
            </div>

            <div className="space-y-2 mb-4">
              <h2 className="text-gray-300 text-sm font-medium mb-2">
                Players
              </h2>
              {lobby.players.map((player) => (
                <div
                  key={player.kingdomId}
                  className="flex items-center justify-between bg-gray-700/50 rounded px-3 py-2"
                >
                  <div className="flex items-center gap-2">
                    {player.isHost && (
                      <span className="text-amber-500" title="Host">
                        &#9813;
                      </span>
                    )}
                    <span className="text-gray-100">{player.userEmail}</span>
                  </div>
                  <span
                    className={
                      player.factionName
                        ? 'text-amber-400 text-sm'
                        : 'text-gray-500 text-sm italic'
                    }
                  >
                    {player.factionName ?? 'No faction'}
                  </span>
                </div>
              ))}
            </div>

            {lobby.status === GAME_STATUS.Lobby && (
              <div className="mb-4">
                <label className="block text-gray-300 text-sm font-medium mb-1">
                  Select Faction
                </label>
                <select
                  value={currentPlayerFaction ?? ''}
                  onChange={(e) => handleFactionChange(e.target.value)}
                  disabled={pendingFaction}
                  className="w-full bg-gray-700 border border-gray-600 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-amber-500 disabled:opacity-50"
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
                </select>
                {factionError && (
                  <p className="text-red-400 text-xs mt-1">{factionError}</p>
                )}
              </div>
            )}

            <div className="flex gap-3 mt-4">
              <button
                onClick={handleLeave}
                disabled={isLeaving}
                className="flex-1 bg-gray-600 hover:bg-gray-500 disabled:opacity-50 text-gray-200 font-medium py-2 rounded transition-colors"
              >
                {isLeaving ? 'Leaving...' : 'Leave Lobby'}
              </button>
              {isHost && (
                <button
                  onClick={handleStart}
                  disabled={!canStart || isStarting}
                  className={`flex-1 font-medium py-2 rounded transition-colors ${
                    canStart
                      ? 'bg-amber-600 hover:bg-amber-500 text-white'
                      : 'bg-gray-600 text-gray-400 cursor-not-allowed'
                  }`}
                >
                  {isStarting ? 'Starting...' : 'Start Game'}
                </button>
              )}
            </div>
            {isHost && !canStart && startReason && (
              <p className="text-gray-400 text-xs mt-1 text-center">
                {startReason}
              </p>
            )}
          </>
        )}
      </div>
    </div>
  );
}
