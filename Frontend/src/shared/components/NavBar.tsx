import { useState } from 'react';
import { Link, useMatch } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/features/auth';
import { useGameStore } from '@/features/game';
import { abandonGame } from '@/features/game/game-api';
import { API_BASE_URL } from '@/lib/constants';
import { LogoutIcon, SwordIcon } from '@/assets/icons';
import { LanguageSwitcher } from './LanguageSwitcher';
import { Modal } from '@/shared/ui/Modal';
import { Button } from '@/shared/ui/Button';

export function NavBar() {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const activeGameId = useGameStore((s) => s.activeGameId);
  const onGamePage = useMatch('/game/:id');

  const [showAbandonModal, setShowAbandonModal] = useState(false);
  const [isAbandoning, setIsAbandoning] = useState(false);

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
        {t('auth.title')}
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
              {t('game.rejoinGame')}
            </Link>
            <button
              className="px-3 py-1 text-sm bg-blood-700 text-parchment-200 hover:bg-blood-600 transition-colors rounded"
              onClick={() => setShowAbandonModal(true)}
            >
              {t('game.abandonGame')}
            </button>
          </div>
          <div className="h-5 w-px bg-bronze-700" />

          <Modal open={showAbandonModal} onClose={() => setShowAbandonModal(false)}>
            <h2 className="font-heading text-gold-400 text-lg mb-2">{t('game.abandonGame')}</h2>
            <p className="text-parchment-300 text-sm mb-4">{t('game.abandonConfirm')}</p>
            <div className="flex gap-2 justify-end">
              <Button variant="secondary" size="sm" onClick={() => setShowAbandonModal(false)}>
                {t('common.cancel')}
              </Button>
              <Button
                variant="danger"
                size="sm"
                disabled={isAbandoning}
                onClick={async () => {
                  if (!activeGameId) return;
                  setIsAbandoning(true);
                  try {
                    await abandonGame(activeGameId);
                    setShowAbandonModal(false);
                    useGameStore.getState().setActiveGameId(null);
                  } catch (err) {
                    console.error('Failed to abandon game:', err);
                  } finally {
                    setIsAbandoning(false);
                  }
                }}
              >
                {isAbandoning ? '...' : t('game.abandonGame')}
              </Button>
            </div>
          </Modal>
        </>
      )}
      <div className="flex items-center gap-4">
        <LanguageSwitcher />
        {user ? (
          <>
            <span className="text-parchment-400 text-sm">{user.email}</span>
            <button
              onClick={handleLogout}
              className="flex items-center gap-1.5 text-parchment-400 hover:text-gold-500 transition-colors"
            >
              <LogoutIcon size={18} />
              <span className="text-sm">{t('game.logout')}</span>
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="text-parchment-300 hover:text-gold-500 transition-colors text-sm">
              {t('game.login')}
            </Link>
            <Link to="/register" className="text-parchment-300 hover:text-gold-500 transition-colors text-sm">
              {t('game.registerLink')}
            </Link>
          </>
        )}
      </div>
    </nav>
  );
}
