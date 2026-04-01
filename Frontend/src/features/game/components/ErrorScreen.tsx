import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui';
import { Panel } from '@/shared/ui';

interface ErrorScreenProps {
  onRetry: () => void;
}

export function ErrorScreen({ onRetry }: ErrorScreenProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div className="flex flex-1 min-h-0 flex-col items-center justify-center bg-ash-950">
      <Panel className="max-w-md p-8 text-center">
        <h2 className="font-heading text-xl font-bold text-gold-500">
          {t('game.connectionFailed')}
        </h2>
        <p className="mt-4 text-base text-parchment-300">
          {t('game.connectionFailedMsg')}
        </p>
        <div className="mt-6 flex flex-col gap-2">
          <Button variant="primary" onClick={onRetry} autoFocus>
            {t('game.retryConnection')}
          </Button>
          <Button variant="ghost" onClick={() => navigate('/')}>
            {t('game.returnToLobby')}
          </Button>
        </div>
      </Panel>
    </div>
  );
}
