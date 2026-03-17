import { useState, useRef, useCallback, useEffect } from 'react';
import { useParams } from 'react-router';
import { useGameStore } from './game-store';
import { connectToGame, disconnectFromGame } from './game-hub';
import { useGameCanvas } from './canvas/useGameCanvas';
import { drawGameMap } from './canvas/hex-renderer';
import { pixelToAxial } from './canvas/hex-math';
import { LoadingScreen } from './components/LoadingScreen';
import { ErrorScreen } from './components/ErrorScreen';
import { ReconnectBanner } from './components/ReconnectBanner';
import { HexTooltip } from './components/HexTooltip';
import type { HexLayoutConfig, MapRenderState } from './canvas/types';

export function GamePage() {
  const { id: gameId } = useParams();
  const connectionStatus = useGameStore((s) => s.connectionStatus);

  const [hoveredTileKey, setHoveredTileKey] = useState<string | null>(null);
  const [selectedTileKey, setSelectedTileKey] = useState<string | null>(null);
  const [tooltipPos, setTooltipPos] = useState<{ x: number; y: number } | null>(null);

  const hoveredRef = useRef(hoveredTileKey);
  const selectedRef = useRef(selectedTileKey);
  hoveredRef.current = hoveredTileKey;
  selectedRef.current = selectedTileKey;

  useEffect(() => {
    if (!gameId) return;
    connectToGame(gameId);
    return () => disconnectFromGame();
  }, [gameId]);

  const draw = useCallback(
    (ctx: CanvasRenderingContext2D, width: number, height: number) => {
      const state = useGameStore.getState();
      const renderState: MapRenderState = {
        hoveredTileKey: hoveredRef.current,
        selectedTileKey: selectedRef.current,
      };
      drawGameMap(
        ctx,
        width,
        height,
        {
          tiles: state.tiles,
          kingdoms: state.kingdoms,
          armies: state.armies,
          tileIdToCoord: state.tileIdToCoord,
          myKingdomId: state.myKingdomId,
          mapRadius: state.mapRadius,
        },
        renderState,
      );
    },
    [],
  );

  const { canvasRef, markDirty } = useGameCanvas({ draw });

  useEffect(() => {
    const unsub = useGameStore.subscribe(() => markDirty());
    return unsub;
  }, [markDirty]);

  const handleMouseMove = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      const canvas = canvasRef.current;
      if (!canvas) return;
      const rect = canvas.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      const layout: HexLayoutConfig = {
        size: 30,
        origin: { x: rect.width / 2, y: rect.height / 2 },
      };
      const axial = pixelToAxial({ x, y }, layout);
      const key = `${axial.q},${axial.r}`;
      const tile = useGameStore.getState().tiles.get(key);
      const newKey = tile ? key : null;
      setHoveredTileKey(newKey);
      setTooltipPos(
        tile ? { x: Math.min(x + 16, rect.width - 200), y: Math.max(y - 8, 8) } : null,
      );
      markDirty();
    },
    [canvasRef, markDirty],
  );

  const handleMouseLeave = useCallback(() => {
    setHoveredTileKey(null);
    setTooltipPos(null);
    markDirty();
  }, [markDirty]);

  const handleClick = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      const canvas = canvasRef.current;
      if (!canvas) return;
      const rect = canvas.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      const layout: HexLayoutConfig = {
        size: 30,
        origin: { x: rect.width / 2, y: rect.height / 2 },
      };
      const axial = pixelToAxial({ x, y }, layout);
      const key = `${axial.q},${axial.r}`;
      const tile = useGameStore.getState().tiles.get(key);
      if (!tile) {
        setSelectedTileKey(null);
      } else {
        setSelectedTileKey((prev) => (prev === key ? null : key));
      }
      markDirty();
    },
    [canvasRef, markDirty],
  );

  const isLoading = connectionStatus === 'connecting' || connectionStatus === 'disconnected';
  const isFailed = connectionStatus === 'failed';

  if (isFailed) {
    return <ErrorScreen onRetry={() => gameId && connectToGame(gameId)} />;
  }

  return (
    <div className="relative flex flex-col w-full flex-1 min-h-0 overflow-hidden">
      {isLoading && <LoadingScreen />}
      {connectionStatus === 'reconnecting' && <ReconnectBanner />}
      <canvas
        ref={canvasRef}
        className={`block w-full h-full ${isLoading ? 'hidden' : ''}`}
        onMouseMove={handleMouseMove}
        onMouseLeave={handleMouseLeave}
        onClick={handleClick}
      />
      {hoveredTileKey &&
        tooltipPos &&
        (() => {
          const state = useGameStore.getState();
          const tile = state.tiles.get(hoveredTileKey);
          if (!tile) return null;
          const kingdom = tile.kingdomId
            ? (state.kingdoms.get(tile.kingdomId) ?? null)
            : null;
          const armies = [...state.armies.values()].filter((a) => {
            const coord = state.tileIdToCoord.get(a.tileId);
            return coord === hoveredTileKey;
          });
          return (
            <HexTooltip tile={tile} kingdom={kingdom} armies={armies} position={tooltipPos} />
          );
        })()}
    </div>
  );
}
