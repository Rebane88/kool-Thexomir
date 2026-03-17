import { Navigate, Outlet, useLocation } from 'react-router';
import { useAuthStore } from '@/features/auth';

export function ProtectedRoute() {
  const user = useAuthStore((s) => s.user);
  const isInitializing = useAuthStore((s) => s.isInitializing);
  const location = useLocation();

  if (isInitializing) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <p className="text-lg text-parchment-400">Loading...</p>
      </div>
    );
  }

  if (!user) {
    return (
      <Navigate
        to={`/login?returnTo=${encodeURIComponent(location.pathname)}`}
        replace
      />
    );
  }

  return <Outlet />;
}
