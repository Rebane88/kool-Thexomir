import { useState, useRef, useCallback, useEffect } from 'react';
import type { Kingdom } from './types/kingdom-types';
import { useParams } from 'react-router';
import { useGameStore } from './game-store';
import { connectToGame, disconnectFromGame } from './game-hub';
import { useGameCanvas } from './canvas/useGameCanvas';
import { drawGameMap } from './canvas/hex-renderer';
import { pixelToAxial, getHexNeighbors } from './canvas/hex-math';
import { screenToWorld, computeZoom, DEFAULT_CAMERA } from './canvas/camera';
import type { CameraState } from './canvas/camera';
import { placeBuilding, fetchBuildingTypes, fetchUnitTypes, moveArmy, attackTile } from './game-api';
import { BuildingPanel } from './components/BuildingPanel';
import { LoadingScreen } from './components/LoadingScreen';
import { ErrorScreen } from './components/ErrorScreen';
import { ReconnectBanner } from './components/ReconnectBanner';
import { HexTooltip } from './components/HexTooltip';
import { AttackConfirmModal } from './components/AttackConfirmModal';
import { ResetCameraButton } from './components/ResetCameraButton';
import { GameHud } from './components/GameHud';
import { CombatResultModal } from './components/CombatResultModal';
import { EliminationBanner } from './components/EliminationBanner';
import type { HexLayoutConfig, MapRenderState } from './canvas/types';
import type { Army } from './types/military-types';

export function GamePage() {
  const { id: gameId } = useParams();
  const connectionStatus = useGameStore((s) => s.connectionStatus);

  const [hoveredTileKey, setHoveredTileKey] = useState<string | null>(null);
  const [selectedTileKey, setSelectedTileKey] = useState<string | null>(null);
  const [tooltipPos, setTooltipPos] = useState<{ x: number; y: number } | null>(null);

  const [armyHighlightTileKey, setArmyHighlightTileKey] = useState<string | null>(null);
  const [attackTarget, setAttackTarget] = useState<{ tileId: string; tileKey: string } | null>(null);

  const hoveredRef = useRef(hoveredTileKey);
  const selectedRef = useRef(selectedTileKey);
  const armyHighlightRef = useRef(armyHighlightTileKey);
  hoveredRef.current = hoveredTileKey;
  selectedRef.current = selectedTileKey;
  armyHighlightRef.current = armyHighlightTileKey;

  const cameraRef = useRef<CameraState>({ ...DEFAULT_CAMERA });
  const dragStartRef = useRef<{ x: number; y: number } | null>(null);
  const isDraggingRef = useRef(false);
  const lastDragPosRef = useRef<{ x: number; y: number } | null>(null);

  const myKingdomId = useGameStore((s) => s.myKingdomId);
  const isMyTurn = useGameStore(
    (s) => s.myKingdomId !== null && s.currentTurnKingdomId === s.myKingdomId,
  );
  const selectedTile = useGameStore((s) => {
    if (!selectedTileKey) return null;
    return s.tiles.get(selectedTileKey) ?? null;
  });
  const showBuildingPanel = selectedTile !== null && selectedTile.kingdomId === myKingdomId;

  const lastCombatResult = useGameStore((s) => s.lastCombatResult);
  const dismissCombatResult = useGameStore((s) => s.dismissCombatResult);
  const myKingdom = useGameStore((s) => s.myKingdomId ? s.kingdoms.get(s.myKingdomId) : undefined);
  const isEliminated = myKingdom?.isEliminated ?? false;

  const [eliminationBanners, setEliminationBanners] = useState<string[]>([]);

  const findMyArmyOnTile = useCallback((tileKey: string) => {
    const state = useGameStore.getState();
    if (!state.myKingdomId) return null;
    for (const army of state.armies.values()) {
      const coord = state.tileIdToCoord.get(army.tileId);
      if (coord === tileKey && army.kingdomId === state.myKingdomId) return army;
    }
    return null;
  }, []);

  const isNeighborOfHighlight = useCallback((targetKey: string, highlightKey: string) => {
    const state = useGameStore.getState();
    const highlightTile = state.tiles.get(highlightKey);
    if (!highlightTile) return false;
    const neighbors = getHexNeighbors(highlightTile.coordQ, highlightTile.coordR);
    return neighbors.some(n => `${n.q},${n.r}` === targetKey);
  }, []);

  useEffect(() => {
    if (!gameId) return;
    connectToGame(gameId);
    return () => disconnectFromGame();
  }, [gameId]);

  // Fetch building types once on connection
  useEffect(() => {
    if (!gameId || connectionStatus !== 'connected') return;
    const { buildingTypes } = useGameStore.getState();
    if (buildingTypes.length > 0) return;
    fetchBuildingTypes(gameId)
      .then((types) => useGameStore.getState().setBuildingTypes(types))
      .catch((err) => console.error('Failed to fetch building types:', err));
  }, [gameId, connectionStatus]);

  // Fetch unit types once on connection
  useEffect(() => {
    if (!gameId || connectionStatus !== 'connected') return;
    const { unitTypes } = useGameStore.getState();
    if (unitTypes.length > 0) return;
    fetchUnitTypes(gameId)
      .then((types) => useGameStore.getState().setUnitTypes(types))
      .catch((err) => console.error('Failed to fetch unit types:', err));
  }, [gameId, connectionStatus]);

  // Clear build mode when panel hides
  useEffect(() => {
    if (!showBuildingPanel) {
      useGameStore.getState().setBuildMode(null);
    }
  }, [showBuildingPanel]);

  // Detect newly eliminated kingdoms to show banners
  const prevKingdomsRef = useRef<Map<string, Kingdom>>(new Map());
  useEffect(() => {
    const unsub = useGameStore.subscribe((state) => {
      const prev = prevKingdomsRef.current;
      for (const [id, kingdom] of state.kingdoms) {
        const prevKingdom = prev.get(id);
        if (kingdom.isEliminated && prevKingdom && !prevKingdom.isEliminated) {
          setEliminationBanners((b) => [...b, kingdom.name]);
        }
      }
      prevKingdomsRef.current = new Map(state.kingdoms);
    });
    return unsub;
  }, []);

  // Auto-highlight army on selected tile
  useEffect(() => {
    if (!selectedTileKey || !isMyTurn || isEliminated) {
      setArmyHighlightTileKey(null);
      return;
    }
    const army = findMyArmyOnTile(selectedTileKey);
    setArmyHighlightTileKey(army ? selectedTileKey : null);
  }, [selectedTileKey, isMyTurn, isEliminated, findMyArmyOnTile]);

  // Clear army highlights when build mode activates
  useEffect(() => {
    const unsub = useGameStore.subscribe((state) => {
      if (state.buildModeTypeId) {
        setArmyHighlightTileKey(null);
      }
    });
    return unsub;
  }, []);

  const draw = useCallback(
    (ctx: CanvasRenderingContext2D, width: number, height: number) => {
      const state = useGameStore.getState();
      const renderState: MapRenderState = {
        hoveredTileKey: hoveredRef.current,
        selectedTileKey: selectedRef.current,
        buildModeTypeId: state.buildModeTypeId,
        armyHighlightTileKey: armyHighlightRef.current,
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
      const state = useGameStore.getState();
      const tile = state.tiles.get(key);

      // ARMY MOVEMENT/ATTACK INTERCEPT
      if (armyHighlightRef.current && tile && isNeighborOfHighlight(key, armyHighlightRef.current)) {
        const army = findMyArmyOnTile(armyHighlightRef.current);
        if (army) {
          const isEnemy = tile.kingdomId !== null && tile.kingdomId !== state.myKingdomId;
          if (isEnemy) {
            setAttackTarget({ tileId: tile.id, tileKey: key });
            return;
          }
          // Friendly/empty tile: move army
          if (state.gameId) {
            moveArmy(state.gameId, { armyId: army.id, targetTileId: tile.id })
              .catch((err) => console.error('Failed to move army:', err));
          }
          setSelectedTileKey(null);
          setArmyHighlightTileKey(null);
          markDirty();
          return;
        }
      }

      // BUILD MODE INTERCEPT: handle placement instead of selection
      if (state.buildModeTypeId && tile) {
        if (tile.kingdomId === state.myKingdomId && tile.buildings.length === 0) {
          const currentGameId = state.gameId;
          if (currentGameId) {
            placeBuilding(currentGameId, {
              tileId: tile.id,
              buildingTypeId: state.buildModeTypeId,
            }).catch((err) => {
              console.error('Failed to place building:', err);
            });
          }
        }
        markDirty();
        return;
      }

      // Normal tile selection (not in build mode)
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

  // Keyboard shortcuts: Home (reset camera), Escape (exit build mode)
  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'Home') {
        e.preventDefault();
        handleResetCamera();
      }
      if (e.key === 'Escape') {
        const { lastCombatResult: combatResult } = useGameStore.getState();
        if (combatResult) {
          useGameStore.getState().dismissCombatResult();
          return;
        }
        const { buildModeTypeId } = useGameStore.getState();
        if (buildModeTypeId) {
          useGameStore.getState().setBuildMode(null);
          markDirty();
        }
        if (armyHighlightRef.current) {
          setArmyHighlightTileKey(null);
          setSelectedTileKey(null);
          markDirty();
        }
      }
    }
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [handleResetCamera, markDirty]);

  const handleContextMenu = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      e.preventDefault();
      const { buildModeTypeId } = useGameStore.getState();
      if (buildModeTypeId) {
        useGameStore.getState().setBuildMode(null);
        markDirty();
      }
      if (armyHighlightRef.current) {
        setArmyHighlightTileKey(null);
        setSelectedTileKey(null);
        markDirty();
      }
    },
    [markDirty],
  );

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
        onContextMenu={handleContextMenu}
        style={{ cursor: 'grab' }}
      />
      {!isLoading && <GameHud />}
      {!isLoading && showBuildingPanel && (
        <div className={isMyTurn && !isEliminated ? '' : 'opacity-50 pointer-events-none'}>
          <BuildingPanel selectedTileKey={selectedTileKey!} />
        </div>
      )}
      {!isLoading && <ResetCameraButton onReset={handleResetCamera} />}
      {eliminationBanners.map((name, index) => (
        <EliminationBanner
          key={`${name}-${index}`}
          kingdomName={name}
          onFaded={() => setEliminationBanners((b) => b.filter((_, i) => i !== index))}
        />
      ))}
      {lastCombatResult && (() => {
        const state = useGameStore.getState();
        const attackerKingdom = state.kingdoms.get(lastCombatResult.attackerKingdomId);
        const defenderKingdom = state.kingdoms.get(lastCombatResult.defenderKingdomId);
        return (
          <CombatResultModal
            open={true}
            onClose={dismissCombatResult}
            result={lastCombatResult}
            myKingdomId={state.myKingdomId ?? ''}
            attackerKingdomName={attackerKingdom?.name ?? 'Attacker'}
            defenderKingdomName={defenderKingdom?.name ?? 'Defender'}
          />
        );
      })()}
      {attackTarget && armyHighlightTileKey && (() => {
        const state = useGameStore.getState();
        const myArmy = findMyArmyOnTile(armyHighlightTileKey);
        const targetTile = state.tiles.get(attackTarget.tileKey);
        if (!myArmy || !targetTile) return null;

        let defenderArmy: Army | null = null;
        for (const army of state.armies.values()) {
          const coord = state.tileIdToCoord.get(army.tileId);
          if (coord === attackTarget.tileKey) { defenderArmy = army; break; }
        }

        const myKingdomObj = state.myKingdomId ? state.kingdoms.get(state.myKingdomId) : null;
        const defenderKingdom = targetTile.kingdomId ? state.kingdoms.get(targetTile.kingdomId) : null;

        return (
          <AttackConfirmModal
            open={true}
            onClose={() => setAttackTarget(null)}
            onConfirm={() => {
              if (state.gameId && myArmy) {
                attackTile(state.gameId, {
                  attackerArmyId: myArmy.id,
                  defenderTileId: targetTile.id,
                }).catch((err) => console.error('Failed to attack:', err));
              }
              setAttackTarget(null);
              setSelectedTileKey(null);
              setArmyHighlightTileKey(null);
              markDirty();
            }}
            attackerArmy={myArmy}
            defenderArmy={defenderArmy}
            attackerKingdomName={myKingdomObj?.name ?? 'Your Army'}
            defenderKingdomName={defenderKingdom?.name ?? 'Enemy'}
          />
        );
      })()}
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
