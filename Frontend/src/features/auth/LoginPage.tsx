import { useState } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router';
import { useAuthStore } from '@/features/auth/auth-store';
import { API_BASE_URL } from '@/lib/constants';
import type { LoginResponse, ProblemDetails } from '@/shared/types/api';
import { Button, Panel, Input } from '@/shared/ui';
import { EyeIcon, EyeOffIcon } from '@/assets/icons';

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
      <div className="min-h-dvh flex items-center justify-center">
        <p className="text-parchment-400">Loading...</p>
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
      const res = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
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
    <div className="min-h-dvh flex flex-col items-center justify-center px-4 relative">
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,_var(--color-ash-800)_0%,_transparent_70%)] pointer-events-none" />

      <div className="relative z-10 max-w-md w-full">
        <h1 className="font-heading text-gold-500 text-4xl text-center mb-8 tracking-wide">
          Realms of Ash
        </h1>

        <Panel className="p-6">
          {sessionExpired && (
            <div className="bg-ember-500/20 border border-ember-500/30 text-ember-400 px-4 py-2 text-sm mb-4">
              Session expired, please log in again.
            </div>
          )}

          {generalError && (
            <div className="bg-blood-600/20 border border-blood-600/30 text-blood-500 px-4 py-2 text-sm mb-4">
              {generalError}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <Input
              id="email"
              type="email"
              autoComplete="email"
              label="Email"
              value={email}
              onChange={(e) => handleFieldChange('email', e.target.value)}
              error={errors.email}
            />

            <div>
              <label
                htmlFor="password"
                className="text-parchment-300 text-sm font-medium mb-1 block"
              >
                Password
              </label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => handleFieldChange('password', e.target.value)}
                  error={errors.password}
                  className="pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-parchment-400 hover:text-gold-500 transition-colors"
                >
                  {showPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                </button>
              </div>
            </div>

            <Button type="submit" disabled={isSubmitting} className="w-full">
              {isSubmitting ? 'Signing in...' : 'Sign In'}
            </Button>
          </form>

          <p className="text-center text-sm text-parchment-400 mt-4">
            Don&apos;t have an account?{' '}
            <Link
              to="/register"
              className="text-gold-500 hover:text-gold-300 transition-colors"
            >
              Register
            </Link>
          </p>
        </Panel>
      </div>
    </div>
  );
}
