import { useState, useEffect } from 'react';

interface EliminationBannerProps {
  kingdomName: string;
  onFaded: () => void;
}

export function EliminationBanner({ kingdomName, onFaded }: EliminationBannerProps) {
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
        <span className="text-parchment-100 font-heading text-lg font-semibold">
          {kingdomName} has been eliminated!
        </span>
      </div>
    </div>
  );
}
