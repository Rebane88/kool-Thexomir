import { useCallback, useEffect } from 'react';
import { useParams } from 'react-router';
import { useGameStore } from './game-store';
import { connectToGame, disconnectFromGame } from './game-hub';
import { useGameCanvas } from './canvas/useGameCanvas';
import { drawHexGrid } from './canvas/hex-renderer';
import { generateAxialCoords } from './canvas/hex-math';
import { LoadingScreen } from './components/LoadingScreen';
import { ErrorScreen } from './components/ErrorScreen';
import { ReconnectBanner } from './components/ReconnectBanner';
import type { HexLayoutConfig } from './canvas/types';

export function GamePage() {
  const { id: gameId } = useParams();
  const connectionStatus = useGameStore((s) => s.connectionStatus);
  const mapRadius = useGameStore((s) => s.mapRadius);

  useEffect(() => {
    if (!gameId) return;
    connectToGame(gameId);
    return () => disconnectFromGame();
  }, [gameId]);

  const draw = useCallback(
    (ctx: CanvasRenderingContext2D, width: number, height: number) => {
      ctx.fillStyle = '#0a0a0f';
      ctx.fillRect(0, 0, width, height);

      const layout: HexLayoutConfig = {
        size: 30,
        origin: { x: width / 2, y: height / 2 },
      };
      // Use mapRadius from store if available, otherwise default
      const radius = mapRadius > 0 ? mapRadius : 7;
      const coords = generateAxialCoords(radius);
      drawHexGrid(ctx, coords, layout);
    },
    [mapRadius],
  );

  const { canvasRef } = useGameCanvas({ draw });

  if (connectionStatus === 'connecting' || connectionStatus === 'disconnected') {
    return <LoadingScreen />;
  }

  if (connectionStatus === 'failed') {
    return <ErrorScreen onRetry={() => gameId && connectToGame(gameId)} />;
  }

  return (
    <div className="relative w-full flex-1 min-h-0 overflow-hidden">
      {connectionStatus === 'reconnecting' && <ReconnectBanner />}
      <canvas ref={canvasRef} className="block w-full h-full" />
    </div>
  );
}
