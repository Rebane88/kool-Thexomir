import { useRef, useLayoutEffect, useState, type ReactNode } from 'react';

const TOOLTIP_MARGIN = 12;
const ARROW_SIZE = 8;

interface TooltipProps {
  children: ReactNode;
  position: { x: number; y: number };
  className?: string;
  style?: React.CSSProperties;
}

export function Tooltip({ children, position, className = '', style: extraStyle }: TooltipProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [style, setStyle] = useState<React.CSSProperties>({
    position: 'fixed',
    zIndex: 40,
    left: position.x,
    top: position.y + TOOLTIP_MARGIN + ARROW_SIZE,
    transform: 'translateX(-50%)',
    visibility: 'hidden',
  });
  const [arrowFlipped, setArrowFlipped] = useState(false);

  useLayoutEffect(() => {
    if (!ref.current) return;
    const rect = ref.current.getBoundingClientRect();
    const width = rect.width;
    const height = rect.height;

    let left = position.x;
    let top = position.y + TOOLTIP_MARGIN + ARROW_SIZE;
    let flipAbove = false;

    // Horizontal viewport clamp
    if (left + width / 2 > window.innerWidth - TOOLTIP_MARGIN) {
      left = window.innerWidth - TOOLTIP_MARGIN - width / 2;
    }
    if (left - width / 2 < TOOLTIP_MARGIN) {
      left = TOOLTIP_MARGIN + width / 2;
    }

    // Vertical flip above cursor if overflows bottom
    if (top + height + TOOLTIP_MARGIN > window.innerHeight) {
      top = position.y - TOOLTIP_MARGIN - ARROW_SIZE - height;
      flipAbove = true;
    }

    setArrowFlipped(flipAbove);
    setStyle({
      position: 'fixed',
      zIndex: 40,
      left,
      top,
      transform: 'translateX(-50%)',
      visibility: 'visible',
    });
  }, [position.x, position.y]);

  const arrowStyle: React.CSSProperties = arrowFlipped
    ? {
        position: 'absolute',
        bottom: -ARROW_SIZE,
        left: '50%',
        transform: 'translateX(-50%)',
        width: 0,
        height: 0,
        borderLeft: '8px solid transparent',
        borderRight: '8px solid transparent',
        borderTop: `8px solid var(--color-ash-700)`,
      }
    : {
        position: 'absolute',
        top: -ARROW_SIZE,
        left: '50%',
        transform: 'translateX(-50%)',
        width: 0,
        height: 0,
        borderLeft: '8px solid transparent',
        borderRight: '8px solid transparent',
        borderBottom: `8px solid var(--color-ash-700)`,
      };

  return (
    <div
      ref={ref}
      style={{ ...style, ...extraStyle }}
      className={`bg-ash-700 border border-bronze-700 border-t-2 border-t-gold-500 shadow-ember ${className}`}
    >
      <span style={arrowStyle} />
      {children}
    </div>
  );
}
