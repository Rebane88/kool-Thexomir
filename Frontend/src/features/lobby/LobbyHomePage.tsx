import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Panel, Button, Input, Select } from '@/shared/ui';
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
    <div className="flex-1 flex flex-col items-center justify-center px-4 py-8">
      <div className="max-w-md w-full">
        <h1 className="font-heading text-gold-500 text-3xl text-center mb-6 tracking-wide">
          War Council
        </h1>

        <Panel className="p-6">
          <div className="flex border-b border-bronze-700 mb-5">
            <button
              type="button"
              onClick={() => handleTabChange('create')}
              className={`flex-1 py-2 text-sm font-heading font-medium tracking-wide ${
                activeTab === 'create'
                  ? 'text-gold-500 border-b-2 border-gold-500'
                  : 'text-parchment-400 hover:text-parchment-200'
              }`}
            >
              Create
            </button>
            <button
              type="button"
              onClick={() => handleTabChange('join')}
              className={`flex-1 py-2 text-sm font-heading font-medium tracking-wide ${
                activeTab === 'join'
                  ? 'text-gold-500 border-b-2 border-gold-500'
                  : 'text-parchment-400 hover:text-parchment-200'
              }`}
            >
              Join
            </button>
          </div>

          {error && (
            <div className="bg-blood-600/20 border border-blood-600/30 text-blood-500 px-4 py-2 text-sm mb-4">
              {error}
            </div>
          )}

          {activeTab === 'create' ? (
            <form onSubmit={handleCreate} className="space-y-4">
              <Select
                id="maxPlayers"
                label="Max Players"
                value={maxPlayers}
                onChange={(e) => setMaxPlayers(Number(e.target.value))}
              >
                {[2, 3, 4, 5, 6, 7, 8].map((n) => (
                  <option key={n} value={n}>
                    {n}
                  </option>
                ))}
              </Select>

              <Select
                id="winCondition"
                label="Win Condition"
                value={winCondition}
                onChange={(e) => setWinCondition(Number(e.target.value))}
              >
                {Object.entries(WIN_CONDITION_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </Select>

              <Button
                type="submit"
                disabled={isSubmitting}
                variant="primary"
                className="w-full"
              >
                {isSubmitting ? 'Creating...' : 'Create Lobby'}
              </Button>
            </form>
          ) : (
            <form onSubmit={handleJoin} className="space-y-4">
              <Input
                id="inviteCode"
                label="Invite Code"
                type="text"
                placeholder="Enter invite code"
                maxLength={6}
                value={inviteCode}
                onChange={(e) => setInviteCode(e.target.value.toUpperCase())}
              />

              <Button
                type="submit"
                disabled={inviteCode.length !== 6 || isSubmitting}
                variant="primary"
                className="w-full"
              >
                {isSubmitting ? 'Joining...' : 'Join Lobby'}
              </Button>
            </form>
          )}
        </Panel>
      </div>
    </div>
  );
}
