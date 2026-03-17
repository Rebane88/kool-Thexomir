import { useNavigate } from 'react-router';
import { Button } from '@/shared/ui';
import { Panel } from '@/shared/ui';

interface ErrorScreenProps {
  onRetry: () => void;
}

export function ErrorScreen({ onRetry }: ErrorScreenProps) {
  const navigate = useNavigate();

  return (
    <div className="flex flex-1 min-h-0 flex-col items-center justify-center bg-ash-950">
      <Panel className="max-w-md p-8 text-center">
        <h2 className="font-heading text-xl font-bold text-gold-500">
          Connection Failed
        </h2>
        <p className="mt-4 text-base text-parchment-300">
          Unable to reach the game server. Check your connection and try again.
        </p>
        <div className="mt-6 flex flex-col gap-2">
          <Button variant="primary" onClick={onRetry} autoFocus>
            Retry Connection
          </Button>
          <Button variant="ghost" onClick={() => navigate('/')}>
            Return to Lobby
          </Button>
        </div>
      </Panel>
    </div>
  );
}
