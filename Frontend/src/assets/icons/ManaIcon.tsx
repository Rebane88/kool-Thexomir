import type { SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

export function ManaIcon({ size = 24, ...props }: IconProps) {
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
      {/* Crystal diamond shape */}
      <polygon points="12,2 20,10 12,22 4,10" />
      {/* Facet lines from center */}
      <line x1="12" y1="2" x2="12" y2="22" />
      <line x1="4" y1="10" x2="20" y2="10" />
      {/* Inner facet highlights */}
      <line x1="8" y1="6" x2="12" y2="10" />
      <line x1="16" y1="6" x2="12" y2="10" />
    </svg>
  );
}
