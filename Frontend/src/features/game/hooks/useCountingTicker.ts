import { useState, useEffect, useRef } from 'react';

export function useCountingTicker(target: number, duration: number = 1000): number {
  const [display, setDisplay] = useState(target);
  const startRef = useRef(target);
  const startTimeRef = useRef(0);

  useEffect(() => {
    if (startRef.current === target) return;
    const from = startRef.current;
    const to = target;
    startTimeRef.current = performance.now();

    let rafId: number;
    function tick(now: number) {
      const elapsed = now - startTimeRef.current;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3); // ease-out cubic
      setDisplay(Math.round(from + (to - from) * eased));
      if (progress < 1) {
        rafId = requestAnimationFrame(tick);
      } else {
        startRef.current = to;
      }
    }
    rafId = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(rafId);
  }, [target, duration]);

  return display;
}
