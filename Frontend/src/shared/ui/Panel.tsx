import type { HTMLAttributes } from 'react';

type PanelVariant = 'default' | 'elevated';

const variantClasses: Record<PanelVariant, string> = {
  default: 'bg-ash-800 shadow-panel',
  elevated: 'bg-ash-700 shadow-ember',
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
      {children}
    </div>
  );
}
