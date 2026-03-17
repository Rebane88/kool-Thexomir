import { useState, useRef, useCallback, useEffect } from 'react';
import { useParams } from 'react-router';
import { useGameStore } from './game-store';
import { connectToGame, disconnectFromGame } from './game-hub';
import { useGameCanvas } from './canvas/useGameCanvas';
import { drawGameMap } from './canvas/hex-renderer';
import { pixelToAxial } from './canvas/hex-math';
import { screenToWorld, computeZoom, DEFAULT_CAMERA } from './canvas/camera';
import type { CameraState } from './canvas/camera';
import { LoadingScreen } from './components/LoadingScreen';
import { ErrorScreen } from './components/ErrorScreen';
import { ReconnectBanner } from './components/ReconnectBanner';
import { HexTooltip } from './components/HexTooltip';
import { ResetCameraButton } from './components/ResetCameraButton';
import { GameHud } from './components/GameHud';
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

  const cameraRef = useRef<CameraState>({ ...DEFAULT_CAMERA });
  const dragStartRef = useRef<{ x: number; y: number } | null>(null);
  const isDraggingRef = useRef(false);
  const lastDragPosRef = useRef<{ x: number; y: number } | null>(null);

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
        buildModeTypeId: state.buildModeTypeId,
      };
      const cam = cameraRef.current;

      // Clear in untransformed coordinates (only DPI scale active)
      ctx.fillStyle = '#0a0a0f';
      ctx.fillRect(0, 0, width, height);

      // Apply camera transform
      ctx.save();
      ctx.translate(cam.offsetX, cam.offsetY);
      ctx.scale(cam.zoom, cam.zoom);

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
        { skipClear: true },
      );

      ctx.restore();
    },
    [],
  );

  const { canvasRef, markDirty } = useGameCanvas({ draw });

  useEffect(() => {
    const unsub = useGameStore.subscribe(() => markDirty());
    return unsub;
  }, [markDirty]);

  const screenToAxial = useCallback(
    (screenX: number, screenY: number, rectWidth: number, rectHeight: number) => {
      const world = screenToWorld(screenX, screenY, cameraRef.current);
      const layout: HexLayoutConfig = {
        size: 30,
        origin: { x: rectWidth / 2, y: rectHeight / 2 },
      };
      return { axial: pixelToAxial(world, layout), layout };
    },
    [],
  );

  const handleMouseMove = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      const canvas = canvasRef.current;
      if (!canvas) return;
      const rect = canvas.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;

      // Drag handling
      if (dragStartRef.current) {
        if (!isDraggingRef.current) {
          const dx = x - dragStartRef.current.x;
          const dy = y - dragStartRef.current.y;
          if (Math.sqrt(dx * dx + dy * dy) >= 5) {
            isDraggingRef.current = true;
            lastDragPosRef.current = { x, y };
            setHoveredTileKey(null);
            setTooltipPos(null);
          }
        }
        if (isDraggingRef.current && lastDragPosRef.current) {
          const dx = x - lastDragPosRef.current.x;
          const dy = y - lastDragPosRef.current.y;
          cameraRef.current = {
            ...cameraRef.current,
            offsetX: cameraRef.current.offsetX + dx,
            offsetY: cameraRef.current.offsetY + dy,
          };
          lastDragPosRef.current = { x, y };
          canvas.style.cursor = 'grabbing';
          markDirty();
          return; // Skip hover update during drag
        }
      }

      // Hover handling (only when not dragging)
      const { axial } = screenToAxial(x, y, rect.width, rect.height);
      const key = `${axial.q},${axial.r}`;
      const tile = useGameStore.getState().tiles.get(key);
      const newKey = tile ? key : null;
      setHoveredTileKey(newKey);
      setTooltipPos(
        tile ? { x: Math.min(x + 16, rect.width - 200), y: Math.max(y - 8, 8) } : null,
      );
      markDirty();
    },
    [canvasRef, markDirty, screenToAxial],
  );

  const handleMouseDown = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      if (e.button !== 0) return; // Left click only
      const canvas = canvasRef.current;
      if (!canvas) return;
      const rect = canvas.getBoundingClientRect();
      dragStartRef.current = { x: e.clientX - rect.left, y: e.clientY - rect.top };
      isDraggingRef.current = false;
      lastDragPosRef.current = null;
    },
    [canvasRef],
  );

  const handleMouseUp = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      const wasDragging = isDraggingRef.current;
      dragStartRef.current = null;
      isDraggingRef.current = false;
      lastDragPosRef.current = null;

      const canvas = canvasRef.current;
      if (canvas) canvas.style.cursor = 'grab';

      if (wasDragging) return; // Swallow click after drag

      // Click handling (was < 5px movement)
      if (!canvas) return;
      const rect = canvas.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      const { axial } = screenToAxial(x, y, rect.width, rect.height);
      const key = `${axial.q},${axial.r}`;
      const tile = useGameStore.getState().tiles.get(key);
      if (!tile) {
        setSelectedTileKey(null);
      } else {
        setSelectedTileKey((prev) => (prev === key ? null : key));
      }
      markDirty();
    },
    [canvasRef, markDirty, screenToAxial],
  );

  const handleMouseLeave = useCallback(() => {
    dragStartRef.current = null;
    isDraggingRef.current = false;
    lastDragPosRef.current = null;
    setHoveredTileKey(null);
    setTooltipPos(null);
    const canvas = canvasRef.current;
    if (canvas) canvas.style.cursor = 'grab';
    markDirty();
  }, [canvasRef, markDirty]);

  // Wheel zoom (must use addEventListener for passive:false)
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    function onWheel(e: WheelEvent) {
      e.preventDefault();
      const rect = canvas!.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const mouseY = e.clientY - rect.top;
      cameraRef.current = computeZoom(cameraRef.current, mouseX, mouseY, e.deltaY);
      markDirty();
    }

    canvas.addEventListener('wheel', onWheel, { passive: false });
    return () => canvas.removeEventListener('wheel', onWheel);
  }, [canvasRef, markDirty]);

  // Reset camera handler
  const handleResetCamera = useCallback(() => {
    cameraRef.current = { ...DEFAULT_CAMERA };
    markDirty();
  }, [markDirty]);

  // Home key shortcut
  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'Home') {
        e.preventDefault();
        handleResetCamera();
      }
    }
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [handleResetCamera]);

  const isLoading = connectionStatus === 'connecting' || connectionStatus === 'disconnected';
  const isFailed = connectionStatus === 'failed';

  // Force redraw when canvas transitions from hidden to visible
  useEffect(() => {
    if (!isLoading) markDirty();
  }, [isLoading, markDirty]);

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
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseLeave}
        style={{ cursor: 'grab' }}
      />
      {!isLoading && <GameHud />}
      {!isLoading && <ResetCameraButton onReset={handleResetCamera} />}
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
