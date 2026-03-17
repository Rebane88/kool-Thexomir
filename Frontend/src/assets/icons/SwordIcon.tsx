import type { SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

export function SwordIcon({ size = 24, ...props }: IconProps) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    >
      {/* Blade */}
      <line x1="5" y1="19" x2="18" y2="4" />
      {/* Crossguard */}
      <line x1="7.5" y1="14" x2="12.5" y2="16" />
      {/* Pommel */}
      <line x1="3.5" y1="20.5" x2="5" y2="19" />
    </svg>
  );
}
