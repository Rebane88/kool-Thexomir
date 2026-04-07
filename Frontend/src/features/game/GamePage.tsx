import { useState, useRef, useCallback, useEffect, useMemo } from 'react';
import type { Kingdom } from './types/kingdom-types';
import { useParams } from 'react-router';
import { useGameStore } from './game-store';
import { FACTION_VISUALS } from '../lobby/faction-constants';
import { connectToGame, disconnectFromGame } from './game-hub';
import { useGameCanvas } from './canvas/useGameCanvas';
import { textureCache } from './canvas/texture-cache';
import { drawGameMap } from './canvas/hex-renderer';
import { pixelToAxial, axialToPixel, getHexNeighbors } from './canvas/hex-math';
import { screenToWorld, computeZoom, DEFAULT_CAMERA } from './canvas/camera';
import type { CameraState } from './canvas/camera';
import { placeBuilding, fetchBuildingTypes, fetchArmyTypes, declareAttack } from './game-api';
import { BuildingPanel } from './components/BuildingPanel';
import { LoadingScreen } from './components/LoadingScreen';
import { ErrorScreen } from './components/ErrorScreen';
import { ReconnectBanner } from './components/ReconnectBanner';
import { HexTooltip } from './components/HexTooltip';
import { ResetCameraButton } from './components/ResetCameraButton';
import { GameHud } from './components/GameHud';
import { Standings } from './components/Standings';
import { PhaseBanner } from './components/PhaseBanner';
import { EliminationBanner } from './components/EliminationBanner';
import { GameOverOverlay } from './components/GameOverOverlay';
import { SlotMachineOverlay } from './components/SlotMachineOverlay';
import { ArmyRosterDrawer } from './components/ArmyRosterDrawer';
import { DeclareAttackPrompt } from './components/DeclareAttackPrompt';
import { BattleOverlay } from './components/BattleOverlay';
import type { HexLayoutConfig, MapRenderState, PlacementAnimation } from './canvas/types';

export function GamePage() {
  const { id: gameId } = useParams();
  const connectionStatus = useGameStore((s) => s.connectionStatus);

  const [hoveredTileKey, setHoveredTileKey] = useState<string | null>(null);
  const [selectedTileKey, setSelectedTileKey] = useState<string | null>(null);
  const [tooltipPos, setTooltipPos] = useState<{ x: number; y: number } | null>(null);

  const [assetsReady, setAssetsReady] = useState(false);
  const [loadProgress, setLoadProgress] = useState({ loaded: 0, total: 19 });
  const assetsReadyRef = useRef(false);
  assetsReadyRef.current = assetsReady;

  const hoveredRef = useRef(hoveredTileKey);
  const selectedRef = useRef(selectedTileKey);
  hoveredRef.current = hoveredTileKey;
  selectedRef.current = selectedTileKey;

  const declareAttackGlowRef = useRef<Set<string> | null>(null);
  const placementAnimationsRef = useRef<PlacementAnimation[]>([]);

  const cameraRef = useRef<CameraState>({ ...DEFAULT_CAMERA });
  const dragStartRef = useRef<{ x: number; y: number } | null>(null);
  const isDraggingRef = useRef(false);
  const lastDragPosRef = useRef<{ x: number; y: number } | null>(null);

  // Stable ref so draw and pre-declaration effects can call markDirty without
  // capturing it from useGameCanvas, which would be a TDZ forward reference.
  const markDirtyRef = useRef<() => void>(() => {});

  const isMyTurn = useGameStore(
    (s) => s.myKingdomId !== null && s.currentTurnKingdomId === s.myKingdomId,
  );
  const currentPhase = useGameStore((s) => s.currentPhase);

  // Building panel shows automatically during your Action phase
  const showBuildingPanel = isMyTurn && currentPhase === 'Action';

  const myKingdom = useGameStore((s) => s.myKingdomId ? s.kingdoms.get(s.myKingdomId) : undefined);
  const isEliminated = myKingdom?.status === 'Defeated';

  const factionColor = isMyTurn && myKingdom?.factionTypeId
    ? FACTION_VISUALS[myKingdom.factionTypeId.toLowerCase()]?.hexColor ?? null
    : null;

  const declareAttackMode = useGameStore((s) => s.declareAttackMode);
  const declareAttackTargetTileKey = useGameStore((s) => s.declareAttackTargetTileKey);
  const declareAttackDefenderKingdomId = useGameStore((s) => s.declareAttackDefenderKingdomId);

  const [standingsOpen, setStandingsOpen] = useState(false);
  const [eliminationBanners, setEliminationBanners] = useState<string[]>([]);

  // Compute glowing tile keys when in declare-attack risked tile selection mode
  const declareAttackGlowTileKeys = useMemo<Set<string> | null>(() => {
    if (declareAttackMode !== 'selectRiskedTile' || !declareAttackTargetTileKey) return null;
    const state = useGameStore.getState();
    const targetTile = state.tiles.get(declareAttackTargetTileKey);
    if (!targetTile) return null;
    const neighbors = getHexNeighbors(targetTile.coordQ, targetTile.coordR);
    const glowKeys = new Set<string>();
    for (const n of neighbors) {
      const key = `${n.q},${n.r}`;
      const tile = state.tiles.get(key);
      if (tile?.kingdomId === state.myKingdomId) {
        glowKeys.add(key);
      }
    }
    return glowKeys;
  }, [declareAttackMode, declareAttackTargetTileKey]);

  // Keep glow ref in sync with memo value (allows draw callback to access without recreation)
  declareAttackGlowRef.current = declareAttackGlowTileKeys;

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

  // Fetch army types once on connection
  useEffect(() => {
    if (!gameId || connectionStatus !== 'connected') return;
    const { armyTypes } = useGameStore.getState();
    if (armyTypes.length > 0) return;
    fetchArmyTypes(gameId)
      .then((types) => useGameStore.getState().setArmyTypes(types))
      .catch((err) => console.error('Failed to fetch army types:', err));
  }, [gameId, connectionStatus]);

  // Clear build mode when turn ends or phase changes
  useEffect(() => {
    if (!isMyTurn || currentPhase !== 'Action') {
      useGameStore.getState().setBuildMode(null);
    }
  }, [isMyTurn, currentPhase]);

  // Detect newly eliminated kingdoms to show banners
  const prevKingdomsRef = useRef<Map<string, Kingdom>>(new Map());
  useEffect(() => {
    const unsub = useGameStore.subscribe((state) => {
      const prev = prevKingdomsRef.current;
      for (const [id, kingdom] of state.kingdoms) {
        const prevKingdom = prev.get(id);
        if (kingdom.status === 'Defeated' && prevKingdom && prevKingdom.status !== 'Defeated') {
          setEliminationBanners((b) => [...b, kingdom.name]);
        }
      }
      prevKingdomsRef.current = new Map(state.kingdoms);
    });
    return unsub;
  }, []);

  // Detect new building placements to trigger placement animations
  useEffect(() => {
    let prevTiles = useGameStore.getState().tiles;
    const unsub = useGameStore.subscribe((state) => {
      const newAnims: PlacementAnimation[] = [];
      for (const [key, tile] of state.tiles) {
        const prev = prevTiles.get(key);
        if (!prev) continue;
        // Detect new building placed on this tile
        if (tile.buildings.length > prev.buildings.length) {
          // Find newly claimed tiles (tiles that changed ownership to this kingdom)
          const claimedTileKeys: string[] = [];
          if (tile.kingdomId !== null) {
            for (const [k, t] of state.tiles) {
              const pt = prevTiles.get(k);
              if (pt && t.kingdomId === tile.kingdomId && pt.kingdomId !== tile.kingdomId) {
                claimedTileKeys.push(k);
              }
            }
          }
          newAnims.push({
            tileKey: key,
            startTime: performance.now(),
            claimedTileKeys,
          });
        }
      }
      if (newAnims.length > 0) {
        placementAnimationsRef.current = [...placementAnimationsRef.current, ...newAnims];
        markDirtyRef.current();
      }
      prevTiles = state.tiles;
    });
    return unsub;
  }, []);

  const draw = useCallback(
    (ctx: CanvasRenderingContext2D, width: number, height: number) => {
      const state = useGameStore.getState();
      const selectedBuildingType = state.buildModeTypeId
        ? state.buildingTypes.find((bt) => bt.id === state.buildModeTypeId)
        : null;
      const renderState: MapRenderState = {
        hoveredTileKey: hoveredRef.current,
        selectedTileKey: selectedRef.current,
        buildModeTypeId: state.buildModeTypeId,
        buildModeUpgradesFrom: selectedBuildingType?.unlockedByBuildingTypeId ?? null,
        armyHighlightTileKey: null,
        assetsReady: assetsReadyRef.current,
        declareAttackGlowTileKeys: declareAttackGlowRef.current,
        placementAnimations: placementAnimationsRef.current,
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
          tileIdToCoord: state.tileIdToCoord,
          myKingdomId: state.myKingdomId,
          mapRadius: state.mapRadius,
        },
        renderState,
        { skipClear: true },
      );

      ctx.restore();

      // Prune expired placement animations and schedule continuation
      const drawNow = performance.now();
      placementAnimationsRef.current = placementAnimationsRef.current.filter(
        (a) => drawNow - a.startTime < 600,
      );
      if (placementAnimationsRef.current.length > 0) {
        // Schedule next frame for animation continuation
        requestAnimationFrame(() => markDirtyRef.current());
      }
    },
    [],
  );

  const { canvasRef, markDirty } = useGameCanvas({ draw });
  // Wire up the ref so draw and pre-declaration effects resolve to the real markDirty
  markDirtyRef.current = markDirty;

  // Initialize TextureCache when connected
  useEffect(() => {
    if (connectionStatus !== 'connected' || assetsReady) return;
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d', { alpha: false });
    if (!ctx) return;
    textureCache.init(ctx, (loaded, total) => {
      setLoadProgress({ loaded, total });
    }).then(() => {
      setAssetsReady(true);
      markDirty();
    }).catch((err) => {
      console.error('Failed to load game assets:', err);
    });
  }, [connectionStatus, assetsReady, canvasRef, markDirty]);

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
      setTooltipPos(tile ? { x: e.clientX, y: e.clientY + 16 } : null);
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

      // DECLARE ATTACK INTERCEPT: handle attack flow
      if (state.declareAttackMode === 'selectRiskedTile' && tile) {
        // Check tile is owned by me AND is adjacent to the target tile
        if (tile.kingdomId === state.myKingdomId && state.declareAttackTargetTileKey) {
          const targetTile = state.tiles.get(state.declareAttackTargetTileKey);
          if (targetTile) {
            const neighbors = getHexNeighbors(targetTile.coordQ, targetTile.coordR);
            const isAdjacent = neighbors.some((n) => `${n.q},${n.r}` === key);
            if (isAdjacent && state.gameId) {
              declareAttack(state.gameId, {
                targetTileId: state.declareAttackTargetTileId!,
                riskedTileId: tile.id,
              }).catch((err) => console.error('Failed to declare attack:', err));
              useGameStore.getState().completeDeclareAttack();
              markDirty();
              return;
            }
          }
        }
        // Clicking anything else while in select-risked-tile mode: ignore
        return;
      }

      // DECLARE ATTACK ENTRY: click enemy hex adjacent to owned territory during Action Phase
      if (
        state.declareAttackMode === 'idle' &&
        state.currentPhase === 'Action' &&
        (state.actionPoints ?? 0) >= 1 &&
        tile &&
        tile.kingdomId !== null &&
        tile.kingdomId !== state.myKingdomId
      ) {
        const neighbors = getHexNeighbors(tile.coordQ, tile.coordR);
        const hasAdjacentOwned = neighbors.some((n) => {
          const nTile = state.tiles.get(`${n.q},${n.r}`);
          return nTile?.kingdomId === state.myKingdomId;
        });
        if (hasAdjacentOwned) {
          useGameStore.getState().setDeclareAttackTarget(tile.id, key, tile.kingdomId!);
          markDirty();
          return;
        }
      }

      // BUILD MODE INTERCEPT: handle placement instead of selection
      if (state.buildModeTypeId && tile) {
        if (tile.kingdomId === state.myKingdomId) {
          const buildType = state.buildingTypes.find((bt) => bt.id === state.buildModeTypeId);
          const isUpgrade = buildType?.unlockedByBuildingTypeId != null;
          const isValidTarget = isUpgrade
            ? tile.buildings.some((b) => b.buildingTypeId === buildType!.unlockedByBuildingTypeId)
            : tile.buildings.length === 0;

          if (isValidTarget) {
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

  // Keyboard shortcuts: Home (reset camera), Escape (exit build mode / deselect)
  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'Tab') {
        e.preventDefault();
        setStandingsOpen((o) => !o);
      }
      if (e.key === 'Home') {
        e.preventDefault();
        handleResetCamera();
      }
      if (e.key === 'Escape') {
        const storeState = useGameStore.getState();
        if (storeState.declareAttackMode !== 'idle') {
          storeState.cancelDeclareAttack();
          markDirty();
          return;
        }
        if (storeState.buildModeTypeId) {
          storeState.setBuildMode(null);
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
    },
    [markDirty],
  );

  // Auto-close scoreboard when game over fires
  const gameOver = useGameStore((s) => s.gameOver);
  useEffect(() => {
    if (gameOver) setStandingsOpen(false);
  }, [gameOver]);

  const isLoading = connectionStatus === 'connecting' || connectionStatus === 'disconnected' || (connectionStatus === 'connected' && !assetsReady);
  const isFailed = connectionStatus === 'failed';

  // Force redraw when canvas transitions from hidden to visible
  useEffect(() => {
    if (!isLoading) markDirty();
  }, [isLoading, markDirty]);

  if (isFailed) {
    return <ErrorScreen onRetry={() => gameId && connectToGame(gameId)} />;
  }

  return (
    <div
      className="relative flex flex-col w-full flex-1 min-h-0 overflow-hidden"
      style={factionColor ? {
        '--faction-color': factionColor,
        '--faction-color-glow': `${factionColor}40`,
      } as React.CSSProperties : undefined}
    >
      {isLoading && <LoadingScreen message={connectionStatus === 'connected' ? `Preparing the realm... (${loadProgress.loaded}/${loadProgress.total})` : undefined} />}
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
      {!isLoading && <PhaseBanner />}
      {!isLoading && <GameHud onStandingsToggle={() => setStandingsOpen((o) => !o)} />}
      {!isLoading && <Standings open={standingsOpen} onClose={() => setStandingsOpen(false)} />}
      {!isLoading && <SlotMachineOverlay />}
      {!isLoading && <BattleOverlay />}
      {!isLoading && <ArmyRosterDrawer />}
      {!isLoading && declareAttackMode === 'selectRiskedTile' && declareAttackTargetTileKey && (() => {
        const state = useGameStore.getState();
        const targetTile = state.tiles.get(declareAttackTargetTileKey);
        const defenderKingdom = declareAttackDefenderKingdomId
          ? state.kingdoms.get(declareAttackDefenderKingdomId)
          : null;
        if (!targetTile) return null;
        const canvas = canvasRef.current;
        if (!canvas) return null;
        const rect = canvas.getBoundingClientRect();
        const layout = { size: 30, origin: { x: rect.width / 2, y: rect.height / 2 } };
        const worldPos = axialToPixel({ q: targetTile.coordQ, r: targetTile.coordR }, layout);
        const cam = cameraRef.current;
        const screenX = worldPos.x * cam.zoom + cam.offsetX;
        const screenY = worldPos.y * cam.zoom + cam.offsetY;
        return (
          <DeclareAttackPrompt
            defenderKingdomName={defenderKingdom?.name ?? 'Enemy'}
            onCancel={() => useGameStore.getState().cancelDeclareAttack()}
            position={{ x: rect.left + screenX, y: rect.top + screenY }}
          />
        );
      })()}
      {!isLoading && showBuildingPanel && !isEliminated && (
        <BuildingPanel selectedTileKey={selectedTileKey} />
      )}
      {!isLoading && <ResetCameraButton onReset={handleResetCamera} />}
      {eliminationBanners.map((name, index) => (
        <EliminationBanner
          key={`${name}-${index}`}
          kingdomName={name}
          onFaded={() => setEliminationBanners((b) => b.filter((_, i) => i !== index))}
        />
      ))}
      {hoveredTileKey &&
        tooltipPos &&
        (() => {
          const state = useGameStore.getState();
          const tile = state.tiles.get(hoveredTileKey);
          if (!tile) return null;
          const kingdom = tile.kingdomId
            ? (state.kingdoms.get(tile.kingdomId) ?? null)
            : null;
          const armies = [...state.armies.values()].filter((a) => a.kingdomId === tile.kingdomId);
          return (
            <HexTooltip tile={tile} kingdom={kingdom} armies={armies} position={tooltipPos} />
          );
        })()}
      <GameOverOverlay />
    </div>
  );
}
