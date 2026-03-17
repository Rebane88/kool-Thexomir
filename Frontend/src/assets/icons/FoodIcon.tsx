import type { SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

export function FoodIcon({ size = 24, ...props }: IconProps) {
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
      {/* Wheat stalk */}
      <line x1="12" y1="21" x2="12" y2="5" />
      {/* Left grain kernels */}
      <line x1="12" y1="9" x2="8" y2="6" />
      <line x1="12" y1="12" x2="8" y2="9" />
      <line x1="12" y1="15" x2="8" y2="12" />
      {/* Right grain kernels */}
      <line x1="12" y1="9" x2="16" y2="6" />
      <line x1="12" y1="12" x2="16" y2="9" />
      <line x1="12" y1="15" x2="16" y2="12" />
      {/* Top kernel */}
      <line x1="12" y1="5" x2="12" y2="3" />
    </svg>
  );
}
