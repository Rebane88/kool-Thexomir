import { Link } from 'react-router';
import { useAuthStore } from '@/features/auth';
import { API_BASE_URL } from '@/lib/constants';

export function NavBar() {
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);

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
    <nav className="bg-gray-800 border-b border-gray-700 px-6 py-3 flex items-center justify-between">
      <Link to="/" className="text-xl font-bold text-amber-500 hover:text-amber-400">
        Realms of Ash
      </Link>
      <div className="flex items-center gap-4">
        {user ? (
          <>
            <span className="text-gray-300">{user.email}</span>
            <button
              onClick={handleLogout}
              className="text-gray-400 hover:text-gray-200"
            >
              Logout
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="text-gray-300 hover:text-gray-100">
              Login
            </Link>
            <Link to="/register" className="text-gray-300 hover:text-gray-100">
              Register
            </Link>
          </>
        )}
      </div>
    </nav>
  );
}
