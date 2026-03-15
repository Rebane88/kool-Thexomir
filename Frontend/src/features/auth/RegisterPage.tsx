import { useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router';
import { useAuthStore } from '@/features/auth/auth-store';
import { API_BASE_URL } from '@/lib/constants';
import type { LoginResponse, ProblemDetails } from '@/shared/types/api';

function validateRegisterForm(
  email: string,
  password: string,
  confirmPassword: string,
): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!email.trim()) errors.email = 'Email is required';
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))
    errors.email = 'Invalid email format';
  if (!password) errors.password = 'Password is required';
  else if (password.length < 6)
    errors.password = 'Password must be at least 6 characters';
  if (!confirmPassword)
    errors.confirmPassword = 'Please confirm your password';
  else if (password !== confirmPassword)
    errors.confirmPassword = 'Passwords do not match';
  return errors;
}

export function RegisterPage() {
  const navigate = useNavigate();

  const user = useAuthStore((s) => s.user);
  const isInitializing = useAuthStore((s) => s.isInitializing);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
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

  const handleFieldChange = (field: string, value: string) => {
    if (field === 'email') setEmail(value);
    else if (field === 'password') setPassword(value);
    else if (field === 'confirmPassword') setConfirmPassword(value);

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

    const validationErrors = validateRegisterForm(
      email,
      password,
      confirmPassword,
    );
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsSubmitting(true);
    setGeneralError('');

    try {
      // Step 1: Register
      const registerRes = await fetch(`${API_BASE_URL}/auth/register`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });

      if (!registerRes.ok) {
        const problem: ProblemDetails = await registerRes.json();
        setGeneralError(problem.detail || 'Registration failed');
        return;
      }

      // Step 2: Auto-login (register does NOT return tokens)
      const loginRes = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });

      if (!loginRes.ok) {
        // Registration succeeded but auto-login failed -- rare edge case
        navigate('/login');
        return;
      }

      const data: LoginResponse = await loginRes.json();
      useAuthStore.getState().setAuth(
        { id: data.userId, email: data.email, roles: data.roles },
        data.accessToken,
      );

      navigate('/', { replace: true });
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
                autoComplete="new-password"
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

          <div>
            <label
              htmlFor="confirmPassword"
              className="block text-gray-300 text-sm font-medium mb-1"
            >
              Confirm Password
            </label>
            <div className="relative">
              <input
                id="confirmPassword"
                type={showConfirmPassword ? 'text' : 'password'}
                autoComplete="new-password"
                value={confirmPassword}
                onChange={(e) =>
                  handleFieldChange('confirmPassword', e.target.value)
                }
                className={`w-full bg-gray-700 border rounded px-3 py-2 pr-10 text-gray-100 focus:outline-none focus:border-amber-500 ${
                  errors.confirmPassword ? 'border-red-500' : 'border-gray-600'
                }`}
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-200 text-sm"
              >
                {showConfirmPassword ? 'Hide' : 'Show'}
              </button>
            </div>
            {errors.confirmPassword && (
              <p className="text-red-400 text-xs mt-1">
                {errors.confirmPassword}
              </p>
            )}
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-amber-600 hover:bg-amber-500 disabled:bg-amber-600/50 disabled:cursor-not-allowed text-white font-medium py-2 rounded transition-colors"
          >
            {isSubmitting ? 'Creating account...' : 'Create Account'}
          </button>
        </form>

        <p className="text-center text-sm text-gray-400 mt-4">
          Already have an account?{' '}
          <Link to="/login" className="text-amber-500 hover:text-amber-400">
            Log in
          </Link>
        </p>
      </div>
    </div>
  );
}
