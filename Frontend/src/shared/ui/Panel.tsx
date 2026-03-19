import type { HTMLAttributes } from 'react';

type PanelVariant = 'default' | 'elevated' | 'auth-frame';

const variantClasses: Record<PanelVariant, string> = {
  default: 'bg-ash-800 shadow-panel',
  elevated: 'bg-ash-700 shadow-ember',
  'auth-frame': 'relative bg-ash-800 shadow-ember border-gold-500/60',
};

interface PanelProps extends HTMLAttributes<HTMLDivElement> {
  variant?: PanelVariant;
}

export function Panel({ variant = 'default', className = '', children, ...props }: PanelProps) {
  return (
    <div
      className={`border border-bronze-700 ${variantClasses[variant]} ${className}`}
      {...props}
    >
      {variant === 'auth-frame' && (
        <>
          <span className="absolute top-0 left-0 w-4 h-4 border-t-2 border-l-2 border-gold-500" />
          <span className="absolute top-0 right-0 w-4 h-4 border-t-2 border-r-2 border-gold-500" />
          <span className="absolute bottom-0 left-0 w-4 h-4 border-b-2 border-l-2 border-gold-500" />
          <span className="absolute bottom-0 right-0 w-4 h-4 border-b-2 border-r-2 border-gold-500" />
        </>
      )}
      {children}
    </div>
  );
}
