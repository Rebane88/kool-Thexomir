import type { SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

export function ShieldIcon({ size = 24, ...props }: IconProps) {
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
      {/* Outer shield shape */}
      <path d="M12 2L4 6v5c0 5.25 3.4 10.15 8 12 4.6-1.85 8-6.75 8-12V6l-8-4z" />
      {/* Inner border for depth */}
      <path d="M12 5L6.5 7.5v3.5c0 3.94 2.55 7.62 5.5 9 2.95-1.38 5.5-5.06 5.5-9V7.5L12 5z" />
    </svg>
  );
}
