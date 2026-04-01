import { useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/features/auth/auth-store';
import { API_BASE_URL } from '@/lib/constants';
import type { LoginResponse, ProblemDetails } from '@/shared/types/api';
import { Button, Panel, Input, FogBackground } from '@/shared/ui';
import { EyeIcon, EyeOffIcon } from '@/assets/icons';
import bgAuthPng from '@/assets/images/bg-auth.png';

function validateRegisterForm(
  email: string,
  password: string,
  confirmPassword: string,
  t: (key: string) => string,
): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!email.trim()) errors.email = t('auth.emailRequired');
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))
    errors.email = t('auth.emailInvalid');
  if (!password) errors.password = t('auth.passwordRequired');
  else if (password.length < 6)
    errors.password = t('auth.passwordMinLength');
  if (!confirmPassword)
    errors.confirmPassword = t('auth.passwordConfirmRequired');
  else if (password !== confirmPassword)
    errors.confirmPassword = t('auth.passwordMismatch');
  return errors;
}

export function RegisterPage() {
  const { t } = useTranslation();
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
      <div className="min-h-dvh flex items-center justify-center">
        <p className="text-parchment-400">{t('common.loading')}</p>
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
      t,
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
      setGeneralError(t('auth.networkError'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-dvh flex flex-col items-center justify-center px-4 bg-ash-950">
      <FogBackground backgroundImage={bgAuthPng} />

      <div className="relative z-10 max-w-md w-full">
        <div className="text-center mb-8">
          <h1 className="font-heading text-gold-500 text-5xl tracking-wide" style={{ textShadow: '0 0 20px rgba(201, 168, 76, 0.4), 0 0 40px rgba(217, 119, 6, 0.15)' }}>
            {t('auth.title')}
          </h1>
          <p className="text-parchment-400 italic mt-2 text-sm">{t('auth.subtitle')}</p>
        </div>

        <Panel variant="auth-frame" className="p-8">
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
              label={t('auth.email')}
              value={email}
              onChange={(e) => handleFieldChange('email', e.target.value)}
              error={errors.email}
            />

            <div>
              <label
                htmlFor="password"
                className="text-parchment-300 text-sm font-medium mb-1 block"
              >
                {t('auth.password')}
              </label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
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

            <div>
              <label
                htmlFor="confirmPassword"
                className="text-parchment-300 text-sm font-medium mb-1 block"
              >
                {t('auth.confirmPassword')}
              </label>
              <div className="relative">
                <Input
                  id="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  value={confirmPassword}
                  onChange={(e) =>
                    handleFieldChange('confirmPassword', e.target.value)
                  }
                  error={errors.confirmPassword}
                  className="pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-parchment-400 hover:text-gold-500 transition-colors"
                >
                  {showConfirmPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                </button>
              </div>
            </div>

            <Button type="submit" disabled={isSubmitting} className="w-full">
              {isSubmitting ? t('auth.registering') : t('auth.createAccount')}
            </Button>
          </form>

          <p className="text-center text-sm text-parchment-400 mt-4">
            {t('auth.haveAccount')}{' '}
            <Link
              to="/login"
              className="text-gold-500 hover:text-gold-300 transition-colors"
            >
              {t('auth.login')}
            </Link>
          </p>
        </Panel>
      </div>
    </div>
  );
}
