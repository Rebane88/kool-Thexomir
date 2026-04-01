import { useTranslation } from 'react-i18next';
import { ApCostBadge } from './ApCostBadge';
import { Button } from '@/shared/ui/Button';

interface DeclareAttackPromptProps {
  defenderKingdomName: string;
  onCancel: () => void;
  position: { x: number; y: number };
}

export function DeclareAttackPrompt({ defenderKingdomName, onCancel, position }: DeclareAttackPromptProps) {
  const { t } = useTranslation();

  return (
    <div
      className="fixed z-40 pointer-events-auto"
      style={{
        left: position.x,
        top: position.y,
        transform: 'translate(-50%, -100%) translateY(-12px)',
      }}
    >
      <div className="bg-ash-900/95 border border-bronze-700 rounded-lg p-3 shadow-lg max-w-[200px]">
        <div className="flex items-center justify-between gap-2 mb-2">
          <span className="font-heading text-sm text-parchment-200">
            Attack {defenderKingdomName}?
          </span>
          <ApCostBadge cost={1} />
        </div>
        <p className="text-xs text-parchment-400 mb-3">
          {t('game.selectRiskedTile')}
        </p>
        <Button variant="ghost" size="sm" onClick={onCancel} className="w-full">
          {t('common.cancel')}
        </Button>
      </div>
      {/* Caret pointing down */}
      <div
        className="absolute left-1/2 -translate-x-1/2 -bottom-2 w-0 h-0"
        style={{
          borderLeft: '6px solid transparent',
          borderRight: '6px solid transparent',
          borderTop: '8px solid rgb(120 90 40)',
        }}
      />
    </div>
  );
}
