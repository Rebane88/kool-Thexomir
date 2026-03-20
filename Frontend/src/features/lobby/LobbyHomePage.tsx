import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Panel, Button, Input, FogBackground } from '@/shared/ui';
import { createLobby, joinLobby } from './lobby-api';
import bgLobbyPng from '@/assets/images/bg-lobby.png';

export function LobbyHomePage() {
  const navigate = useNavigate();

  const [activeTab, setActiveTab] = useState<'create' | 'join'>('create');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Create tab state
  const [maxPlayers, setMaxPlayers] = useState(2);

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
      const data = await createLobby(maxPlayers, 0);
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
    <div className="flex-1 flex flex-col items-center justify-center px-4 py-8 bg-ash-950">
      <FogBackground backgroundImage={bgLobbyPng} />

      <div className="relative z-10 max-w-md w-full">
        <div className="text-center mb-6">
          <h1
            className="font-heading text-gold-500 text-3xl tracking-wide"
            style={{ textShadow: '0 0 20px rgba(201, 168, 76, 0.4), 0 0 40px rgba(217, 119, 6, 0.15)' }}
          >
            War Council
          </h1>
        </div>

        <Panel variant="auth-frame" className="p-6">
          <div className="flex mb-5 gap-1">
            <button
              type="button"
              onClick={() => handleTabChange('create')}
              className={`flex-1 py-2.5 text-sm font-heading font-bold tracking-wide border-2 transition-all ${
                activeTab === 'create'
                  ? 'bg-gold-500/10 border-gold-500 text-gold-500 shadow-ember'
                  : 'bg-ash-700 border-bronze-700 text-parchment-400 hover:border-bronze-500 hover:text-parchment-200'
              }`}
            >
              Create
            </button>
            <button
              type="button"
              onClick={() => handleTabChange('join')}
              className={`flex-1 py-2.5 text-sm font-heading font-bold tracking-wide border-2 transition-all ${
                activeTab === 'join'
                  ? 'bg-gold-500/10 border-gold-500 text-gold-500 shadow-ember'
                  : 'bg-ash-700 border-bronze-700 text-parchment-400 hover:border-bronze-500 hover:text-parchment-200'
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
              <div>
                <label className="text-parchment-300 text-sm font-medium mb-2 block">Max Players</label>
                <div role="group" aria-label="Max Players" className="flex gap-2">
                  {([2, 3, 4] as const).map((n) => (
                    <button
                      key={n}
                      type="button"
                      onClick={() => setMaxPlayers(n)}
                      aria-pressed={maxPlayers === n}
                      className={`w-12 h-12 text-lg font-heading font-bold border-2 transition-all ${
                        maxPlayers === n
                          ? 'bg-gold-500 text-ash-950 border-gold-500 shadow-ember'
                          : 'bg-ash-700 text-parchment-300 border-bronze-700 hover:border-bronze-500 hover:text-parchment-200'
                      }`}
                    >
                      {n}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="text-parchment-300 text-sm font-medium mb-1 block">Win Condition</label>
                <p className="text-parchment-200 text-sm bg-ash-700 border border-bronze-700 px-3 py-2">Elimination</p>
              </div>

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
