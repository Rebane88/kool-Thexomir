import type { SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

export function ScrollIcon({ size = 24, ...props }: IconProps) {
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
      {/* Top curl */}
      <path d="M6 3c-1.1 0-2 .9-2 2v1h16V5c0-1.1-.9-2-2-2H6z" />
      {/* Body */}
      <path d="M4 6v12c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V6" />
      {/* Bottom curl */}
      <path d="M4 18c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2" />
      {/* Text lines */}
      <line x1="8" y1="10" x2="16" y2="10" />
      <line x1="8" y1="14" x2="14" y2="14" />
    </svg>
  );
}
