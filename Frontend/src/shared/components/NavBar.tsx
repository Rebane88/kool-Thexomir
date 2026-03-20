import { Link, useMatch } from 'react-router';
import { useAuthStore } from '@/features/auth';
import { useGameStore } from '@/features/game';
import { API_BASE_URL } from '@/lib/constants';
import { LogoutIcon, SwordIcon } from '@/assets/icons';

export function NavBar() {
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const activeGameId = useGameStore((s) => s.activeGameId);
  const onGamePage = useMatch('/game/:id');

  const handleLogout = async () => {
    try {
      await fetch(`${API_BASE_URL}/auth/logout`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          ...(useAuthStore.getState().accessToken
            ? { Authorization: `Bearer ${useAuthStore.getState().accessToken}` }
            : {}),
        },
      });
    } catch {
      // logout API failure is non-critical — clear local state regardless
    }
    clearAuth();
    window.location.href = '/login';
  };

  return (
    <nav className="relative z-20 bg-ash-900 border-b border-bronze-700 px-6 py-3 flex items-center justify-between">
      <Link to="/" className="font-heading text-xl font-bold text-gold-500 hover:text-gold-300 transition-colors tracking-wide">
        Realms of Ash
      </Link>
      {activeGameId && user && !onGamePage && (
        <>
          <div className="h-5 w-px bg-bronze-700" />
          <div className="flex items-center gap-2">
            <Link
              to={`/game/${activeGameId}`}
              className="flex items-center gap-1 font-heading text-sm tracking-wide text-gold-500 hover:text-gold-300 transition-colors"
            >
              <SwordIcon size={16} />
              Rejoin Game
            </Link>
            <button
              className="px-3 py-1 text-sm bg-blood-700 text-parchment-200 opacity-50 cursor-not-allowed"
              aria-disabled="true"
              title="Coming soon"
              disabled
            >
              Abandon Game
            </button>
          </div>
          <div className="h-5 w-px bg-bronze-700" />
        </>
      )}
      <div className="flex items-center gap-4">
        {user ? (
          <>
            <span className="text-parchment-400 text-sm">{user.email}</span>
            <button
              onClick={handleLogout}
              className="flex items-center gap-1.5 text-parchment-400 hover:text-gold-500 transition-colors"
            >
              <LogoutIcon size={18} />
              <span className="text-sm">Logout</span>
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="text-parchment-300 hover:text-gold-500 transition-colors text-sm">
              Login
            </Link>
            <Link to="/register" className="text-parchment-300 hover:text-gold-500 transition-colors text-sm">
              Register
            </Link>
          </>
        )}
      </div>
    </nav>
  );
}
