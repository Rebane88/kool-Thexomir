import type { SVGProps } from 'react';

interface IconProps extends SVGProps<SVGSVGElement> {
  size?: number;
}

export function WoodIcon({ size = 24, ...props }: IconProps) {
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
      {/* Bottom log */}
      <ellipse cx="5" cy="16" rx="2" ry="3" />
      <line x1="5" y1="13" x2="21" y2="13" />
      <line x1="5" y1="19" x2="21" y2="19" />
      <ellipse cx="21" cy="16" rx="2" ry="3" />
      {/* Top log */}
      <ellipse cx="3" cy="9" rx="2" ry="3" />
      <line x1="3" y1="6" x2="19" y2="6" />
      <line x1="3" y1="12" x2="19" y2="12" />
      <ellipse cx="19" cy="9" rx="2" ry="3" />
      {/* Wood grain line on top log */}
      <line x1="7" y1="8" x2="15" y2="8" />
    </svg>
  );
}
