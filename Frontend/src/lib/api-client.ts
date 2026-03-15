import { useAuthStore } from '@/features/auth';
import { API_BASE_URL } from '@/lib/constants';

let refreshPromise: Promise<boolean> | null = null;

async function refreshAccessToken(): Promise<boolean> {
  try {
    const currentToken = useAuthStore.getState().accessToken ?? '';
    const res = await fetch(`${API_BASE_URL}/auth/refresh`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ accessToken: currentToken }),
    });
    if (!res.ok) return false;
    const data = await res.json();
    useAuthStore.getState().setAuth(
      { id: data.userId, email: data.email, roles: data.roles },
      data.accessToken,
    );
    return true;
  } catch {
    return false;
  }
}

export async function apiFetch(
  path: string,
  options: RequestInit = {},
): Promise<Response> {
  const makeRequest = () => {
    const token = useAuthStore.getState().accessToken;
    const headers = new Headers(options.headers);
    if (token) headers.set('Authorization', `Bearer ${token}`);
    if (!headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json');
    }
    return fetch(`${API_BASE_URL}${path}`, {
      ...options,
      headers,
      credentials: 'include',
    });
  };

  let response = await makeRequest();

  if (response.status === 401) {
    if (!refreshPromise) {
      refreshPromise = refreshAccessToken().finally(() => {
        refreshPromise = null;
      });
    }
    const success = await refreshPromise;
    if (success) {
      response = await makeRequest();
    } else {
      useAuthStore.getState().clearAuth();
      window.location.href = '/login?expired=true';
    }
  }

  return response;
}
