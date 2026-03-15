import { useState } from 'react';
import { useNavigate } from 'react-router';
import { createLobby, joinLobby } from './lobby-api';
import { WIN_CONDITION_LABELS } from './lobby-types';

export function LobbyHomePage() {
  const navigate = useNavigate();

  const [activeTab, setActiveTab] = useState<'create' | 'join'>('create');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Create tab state
  const [maxPlayers, setMaxPlayers] = useState(8);
  const [winCondition, setWinCondition] = useState(0);

  // Join tab state
  const [inviteCode, setInviteCode] = useState('');

  const handleTabChange = (tab: 'create' | 'join') => {
    setActiveTab(tab);
    setError('');
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError('');

    try {
      const data = await createLobby(maxPlayers, winCondition);
      navigate(`/lobby/${data.lobbyId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create lobby');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleJoin = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError('');

    try {
      const data = await joinLobby(inviteCode);
      navigate(`/lobby/${data.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to join lobby');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-900 flex items-center justify-center px-4">
      <div className="max-w-md w-full bg-gray-800 rounded-lg p-6 shadow-lg">
        <h1 className="text-amber-500 text-2xl font-bold text-center mb-6">
          Lobby
        </h1>

        <div className="flex border-b border-gray-700 mb-4">
          <button
            type="button"
            onClick={() => handleTabChange('create')}
            className={`flex-1 py-2 text-sm font-medium ${
              activeTab === 'create'
                ? 'text-amber-500 border-b-2 border-amber-500'
                : 'text-gray-400 hover:text-gray-200'
            }`}
          >
            Create
          </button>
          <button
            type="button"
            onClick={() => handleTabChange('join')}
            className={`flex-1 py-2 text-sm font-medium ${
              activeTab === 'join'
                ? 'text-amber-500 border-b-2 border-amber-500'
                : 'text-gray-400 hover:text-gray-200'
            }`}
          >
            Join
          </button>
        </div>

        {error && (
          <div className="bg-red-900/50 border border-red-600 text-red-200 px-4 py-2 rounded text-sm mb-4">
            {error}
          </div>
        )}

        {activeTab === 'create' ? (
          <form onSubmit={handleCreate} className="space-y-4">
            <div>
              <label
                htmlFor="maxPlayers"
                className="block text-gray-300 text-sm font-medium mb-1"
              >
                Max Players
              </label>
              <select
                id="maxPlayers"
                value={maxPlayers}
                onChange={(e) => setMaxPlayers(Number(e.target.value))}
                className="w-full bg-gray-700 border border-gray-600 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-amber-500"
              >
                {[2, 3, 4, 5, 6, 7, 8].map((n) => (
                  <option key={n} value={n}>
                    {n}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label
                htmlFor="winCondition"
                className="block text-gray-300 text-sm font-medium mb-1"
              >
                Win Condition
              </label>
              <select
                id="winCondition"
                value={winCondition}
                onChange={(e) => setWinCondition(Number(e.target.value))}
                className="w-full bg-gray-700 border border-gray-600 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-amber-500"
              >
                {Object.entries(WIN_CONDITION_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full bg-amber-600 hover:bg-amber-500 disabled:bg-amber-600/50 disabled:cursor-not-allowed text-white font-medium py-2 rounded transition-colors"
            >
              {isSubmitting ? 'Creating...' : 'Create Lobby'}
            </button>
          </form>
        ) : (
          <form onSubmit={handleJoin} className="space-y-4">
            <div>
              <label
                htmlFor="inviteCode"
                className="block text-gray-300 text-sm font-medium mb-1"
              >
                Invite Code
              </label>
              <input
                id="inviteCode"
                type="text"
                placeholder="Enter invite code"
                maxLength={6}
                value={inviteCode}
                onChange={(e) => setInviteCode(e.target.value.toUpperCase())}
                className="w-full bg-gray-700 border border-gray-600 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-amber-500"
              />
            </div>

            <button
              type="submit"
              disabled={inviteCode.length !== 6 || isSubmitting}
              className="w-full bg-amber-600 hover:bg-amber-500 disabled:bg-amber-600/50 disabled:cursor-not-allowed text-white font-medium py-2 rounded transition-colors"
            >
              {isSubmitting ? 'Joining...' : 'Join Lobby'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
