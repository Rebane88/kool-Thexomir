import { useCallback } from 'react';
import { useParams } from 'react-router';
import { useGameCanvas } from './canvas/useGameCanvas';
import { drawHexGrid } from './canvas/hex-renderer';
import { generateAxialCoords } from './canvas/hex-math';
import type { HexLayoutConfig } from './canvas/types';

export function GamePage() {
  const { id: _id } = useParams();

  const draw = useCallback(
    (ctx: CanvasRenderingContext2D, width: number, height: number) => {
      ctx.fillStyle = '#0a0a0f';
      ctx.fillRect(0, 0, width, height);

      const layout: HexLayoutConfig = {
        size: 30,
        origin: { x: width / 2, y: height / 2 },
      };
      const coords = generateAxialCoords(7);
      drawHexGrid(ctx, coords, layout);
    },
    [],
  );

  const { canvasRef } = useGameCanvas({ draw });

  return (
    <div className="relative w-full flex-1 min-h-0 overflow-hidden">
      <canvas ref={canvasRef} className="block w-full h-full" />
    </div>
  );
}
