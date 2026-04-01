import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import eliminationSkullPng from '@/assets/images/elimination-skull.png';

interface EliminationBannerProps {
  kingdomName: string;
  onFaded: () => void;
}

export function EliminationBanner({ kingdomName, onFaded }: EliminationBannerProps) {
  const { t } = useTranslation();
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    const fadeTimer = setTimeout(() => setVisible(false), 4000);
    const removeTimer = setTimeout(() => onFaded(), 5000);
    return () => {
      clearTimeout(fadeTimer);
      clearTimeout(removeTimer);
    };
  }, [onFaded]);

  return (
    <div className="fixed top-16 left-0 right-0 z-40 flex justify-center pointer-events-none">
      <div
        className={`bg-blood-900/90 border border-ember-500 px-6 py-3 rounded-lg shadow-lg transition-opacity duration-1000 ${
          visible ? 'opacity-100' : 'opacity-0'
        }`}
      >
        <div className="flex items-center gap-3">
          <img src={eliminationSkullPng} alt="" className="w-8 h-8 object-contain" />
          <span className="text-parchment-100 font-heading text-lg font-semibold">
            {t('game.hasBeenEliminated', { name: kingdomName })}
          </span>
          <img src={eliminationSkullPng} alt="" className="w-8 h-8 object-contain" />
        </div>
      </div>
    </div>
  );
}
