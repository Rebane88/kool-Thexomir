import { useRef, useEffect, useCallback } from 'react';

interface UseGameCanvasOptions {
  draw: (ctx: CanvasRenderingContext2D, width: number, height: number) => void;
}

interface UseGameCanvasResult {
  canvasRef: React.RefObject<HTMLCanvasElement | null>;
  markDirty: () => void;
}

export function useGameCanvas({ draw }: UseGameCanvasOptions): UseGameCanvasResult {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const dirtyRef = useRef(true);
  const rafIdRef = useRef(0);

  const markDirty = useCallback(() => {
    dirtyRef.current = true;
  }, []);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d', { alpha: false });
    if (!ctx) return;

    function applyDpiScaling() {
      const dpr = window.devicePixelRatio || 1;
      const rect = canvas!.getBoundingClientRect();
      canvas!.width = rect.width * dpr;
      canvas!.height = rect.height * dpr;
      ctx!.scale(dpr, dpr);
      dirtyRef.current = true;
    }

    applyDpiScaling();

    const observer = new ResizeObserver(() => {
      applyDpiScaling();
    });
    observer.observe(canvas);

    function loop() {
      if (dirtyRef.current) {
        dirtyRef.current = false;
        const rect = canvas!.getBoundingClientRect();
        draw(ctx!, rect.width, rect.height);
      }
      rafIdRef.current = requestAnimationFrame(loop);
    }
    rafIdRef.current = requestAnimationFrame(loop);

    return () => {
      cancelAnimationFrame(rafIdRef.current);
      observer.disconnect();
    };
  }, [draw]);

  return { canvasRef, markDirty };
}
