import { useState } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router';
import { useAuthStore } from '@/features/auth/auth-store';
import { apiFetch } from '@/lib/api-client';
import type { LoginResponse, ProblemDetails } from '@/shared/types/api';

function validateLoginForm(
  email: string,
  password: string,
): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!email.trim()) errors.email = 'Email is required';
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))
    errors.email = 'Invalid email format';
  if (!password) errors.password = 'Password is required';
  return errors;
}

export function LoginPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const user = useAuthStore((s) => s.user);
  const isInitializing = useAuthStore((s) => s.isInitializing);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [generalError, setGeneralError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (isInitializing) {
    return (
      <div className="min-h-screen bg-gray-900 flex items-center justify-center">
        <p className="text-gray-400">Loading...</p>
      </div>
    );
  }

  if (user) {
    return <Navigate to="/" replace />;
  }

  const sessionExpired = searchParams.get('expired') === 'true';

  const handleFieldChange = (field: string, value: string) => {
    if (field === 'email') setEmail(value);
    else if (field === 'password') setPassword(value);

    if (errors[field]) {
      setErrors((prev) => {
        const next = { ...prev };
        delete next[field];
        return next;
      });
    }
    if (generalError) setGeneralError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const validationErrors = validateLoginForm(email, password);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsSubmitting(true);
    setGeneralError('');

    try {
      const res = await apiFetch('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      });

      if (!res.ok) {
        const problem: ProblemDetails = await res.json();
        setGeneralError(problem.detail || 'Login failed');
        return;
      }

      const data: LoginResponse = await res.json();
      useAuthStore.getState().setAuth(
        { id: data.userId, email: data.email, roles: data.roles },
        data.accessToken,
      );

      const returnTo = searchParams.get('returnTo') || '/';
      navigate(returnTo, { replace: true });
    } catch {
      setGeneralError('Network error. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-900 flex items-center justify-center px-4">
      <div className="max-w-md w-full bg-gray-800 rounded-lg p-6 shadow-lg">
        <h1 className="text-amber-500 text-2xl font-bold text-center mb-6">
          Realms of Ash
        </h1>

        {sessionExpired && (
          <div className="bg-amber-900/50 border border-amber-600 text-amber-200 px-4 py-2 rounded text-sm mb-4">
            Session expired, please log in again.
          </div>
        )}

        {generalError && (
          <div className="bg-red-900/50 border border-red-600 text-red-200 px-4 py-2 rounded text-sm mb-4">
            {generalError}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label
              htmlFor="email"
              className="block text-gray-300 text-sm font-medium mb-1"
            >
              Email
            </label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => handleFieldChange('email', e.target.value)}
              className={`w-full bg-gray-700 border rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-amber-500 ${
                errors.email ? 'border-red-500' : 'border-gray-600'
              }`}
            />
            {errors.email && (
              <p className="text-red-400 text-xs mt-1">{errors.email}</p>
            )}
          </div>

          <div>
            <label
              htmlFor="password"
              className="block text-gray-300 text-sm font-medium mb-1"
            >
              Password
            </label>
            <div className="relative">
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                autoComplete="current-password"
                value={password}
                onChange={(e) => handleFieldChange('password', e.target.value)}
                className={`w-full bg-gray-700 border rounded px-3 py-2 pr-10 text-gray-100 focus:outline-none focus:border-amber-500 ${
                  errors.password ? 'border-red-500' : 'border-gray-600'
                }`}
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-200 text-sm"
              >
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
            {errors.password && (
              <p className="text-red-400 text-xs mt-1">{errors.password}</p>
            )}
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-amber-600 hover:bg-amber-500 disabled:bg-amber-600/50 disabled:cursor-not-allowed text-white font-medium py-2 rounded transition-colors"
          >
            {isSubmitting ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        <p className="text-center text-sm text-gray-400 mt-4">
          Don&apos;t have an account?{' '}
          <Link
            to="/register"
            className="text-amber-500 hover:text-amber-400"
          >
            Register
          </Link>
        </p>
      </div>
    </div>
  );
}
