import type { SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

export function StoneIcon({ size = 24, ...props }: IconProps) {
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
      {/* Front face of stone block */}
      <polygon points="4,10 12,6 20,10 20,18 12,22 4,18" />
      {/* Top face */}
      <polygon points="4,10 12,6 20,10 12,14" />
      {/* Chisel mark on front */}
      <line x1="10" y1="15" x2="14" y2="17" />
      <line x1="14" y1="15" x2="10" y2="17" />
    </svg>
  );
}
