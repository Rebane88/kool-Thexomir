import { useEffect } from 'react';
import { ErrorBoundary } from 'react-error-boundary';
import { useAuthStore } from '@/features/auth';
import { API_BASE_URL } from '@/lib/constants';
import { AppRouter } from '@/routes/router';

function ErrorFallback({ error, resetErrorBoundary }: { error: unknown; resetErrorBoundary: () => void }) {
  const message = error instanceof Error ? error.message : 'An unexpected error occurred';
  return (
    <div className="min-h-screen bg-gray-900 text-gray-100 flex flex-col items-center justify-center gap-4">
      <h1 className="text-2xl font-bold text-red-500">Something went wrong</h1>
      <p className="text-gray-400">{message}</p>
      <button
        onClick={resetErrorBoundary}
        className="px-4 py-2 bg-amber-600 text-white rounded hover:bg-amber-500"
      >
        Try again
      </button>
    </div>
  );
}

function AuthInitializer({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    const init = async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/auth/refresh`, {
          method: 'POST',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ accessToken: '' }),
        });
        if (res.ok) {
          const data = await res.json();
          useAuthStore.getState().setAuth(
            { id: data.userId, email: data.email, roles: data.roles },
            data.accessToken,
          );
        }
      } catch {
        // No session to restore — stay logged out
      } finally {
        useAuthStore.getState().setInitialized();
      }
    };
    init();
  }, []);

  return <>{children}</>;
}

function App() {
  return (
    <ErrorBoundary FallbackComponent={ErrorFallback}>
      <AuthInitializer>
        <AppRouter />
      </AuthInitializer>
    </ErrorBoundary>
  );
}

export default App;
