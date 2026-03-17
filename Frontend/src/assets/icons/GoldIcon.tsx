import type { SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

export function GoldIcon({ size = 24, ...props }: IconProps) {
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
      {/* Outer coin circle */}
      <circle cx="12" cy="12" r="9" />
      {/* Inner coin rim */}
      <circle cx="12" cy="12" r="6" />
      {/* Embossed star/cross in center */}
      <line x1="12" y1="8.5" x2="12" y2="15.5" />
      <line x1="8.5" y1="12" x2="15.5" y2="12" />
    </svg>
  );
}
