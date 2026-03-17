import type { HTMLAttributes } from 'react';

type BadgeVariant = 'default' | 'success' | 'warning' | 'danger';

const variantClasses: Record<BadgeVariant, string> = {
  default: 'bg-ash-700 text-parchment-300 border-bronze-700',
  success: 'bg-gold-500/20 text-gold-400 border-gold-500/30',
  warning: 'bg-ember-500/20 text-ember-400 border-ember-500/30',
  danger: 'bg-blood-600/20 text-blood-500 border-blood-600/30',
};

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
}

export function Badge({ variant = 'default', className = '', children, ...props }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 text-xs font-medium border rounded-sm ${variantClasses[variant]} ${className}`}
      {...props}
    >
      {children}
    </span>
  );
}
