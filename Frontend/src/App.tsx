import { useEffect } from 'react';
import { ErrorBoundary } from 'react-error-boundary';
import { useAuthStore } from '@/features/auth';
import { API_BASE_URL } from '@/lib/constants';
import { AppRouter } from '@/routes/router';
import { Button } from '@/shared/ui';

function ErrorFallback({ error, resetErrorBoundary }: { error: unknown; resetErrorBoundary: () => void }) {
  const message = error instanceof Error ? error.message : 'An unexpected error occurred';
  return (
    <div className="min-h-dvh flex flex-col items-center justify-center gap-4 px-4">
      <h1 className="text-2xl font-heading font-bold text-blood-500">Something went wrong</h1>
      <p className="text-parchment-400">{message}</p>
      <Button onClick={resetErrorBoundary} variant="primary">
        Try again
      </Button>
    </div>
  );
}

let initPromise: Promise<void> | null = null;

function AuthInitializer({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    // Deduplicate: StrictMode double-mounts in dev would fire two concurrent
    // refresh calls, racing the token rotation and sometimes invalidating both.
    if (!initPromise) {
      initPromise = (async () => {
        try {
          const res = await fetch(`${API_BASE_URL}/auth/refresh`, {
            method: 'POST',
            credentials: 'include',
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
      })();
    }
    return () => {
      // Allow re-init on full remount (e.g. hot reload), not StrictMode cleanup
    };
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
