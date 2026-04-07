import { describe, it, expect } from 'vitest';
import type { CameraState } from './camera';
import { DEFAULT_CAMERA, screenToWorld, computeZoom } from './camera';
import { axialToPixel, pixelToAxial } from './hex-math';
import type { HexLayoutConfig } from './types';

describe('DEFAULT_CAMERA', () => {
  it('equals { offsetX: 0, offsetY: 0, zoom: 1.0 }', () => {
    expect(DEFAULT_CAMERA).toEqual({ offsetX: 0, offsetY: 0, zoom: 1.0 });
  });
});

describe('screenToWorld', () => {
  it('returns identity at default camera (no pan, zoom 1.0)', () => {
    const result = screenToWorld(100, 100, { offsetX: 0, offsetY: 0, zoom: 1.0 });
    expect(result).toEqual({ x: 100, y: 100 });
  });

  it('applies pan offset', () => {
    const result = screenToWorld(100, 100, { offsetX: 50, offsetY: 50, zoom: 1.0 });
    expect(result).toEqual({ x: 50, y: 50 });
  });

  it('applies zoom (divides coordinates)', () => {
    const result = screenToWorld(200, 200, { offsetX: 0, offsetY: 0, zoom: 2.0 });
    expect(result).toEqual({ x: 100, y: 100 });
  });

  it('applies pan + zoom combined', () => {
    const result = screenToWorld(150, 150, { offsetX: 100, offsetY: 100, zoom: 0.5 });
    expect(result).toEqual({ x: 100, y: 100 });
  });
});

describe('computeZoom', () => {
  const center: CameraState = { offsetX: 0, offsetY: 0, zoom: 1.0 };

  it('increases zoom when deltaY < 0 (zoom in)', () => {
    const result = computeZoom(center, 400, 300, -100);
    expect(result.zoom).toBeGreaterThan(1.0);
  });

  it('decreases zoom when deltaY > 0 (zoom out)', () => {
    const result = computeZoom(center, 400, 300, 100);
    expect(result.zoom).toBeLessThan(1.0);
  });

  it('clamps at max zoom (2.0) when already at max and zooming in', () => {
    const atMax: CameraState = { offsetX: 0, offsetY: 0, zoom: 2.0 };
    const result = computeZoom(atMax, 400, 300, -100);
    expect(result.zoom).toBe(2.0);
  });

  it('clamps at min zoom (0.5) when already at min and zooming out', () => {
    const atMin: CameraState = { offsetX: 0, offsetY: 0, zoom: 0.5 };
    const result = computeZoom(atMin, 400, 300, 100);
    expect(result.zoom).toBe(0.5);
  });

  it('keeps world point under cursor stationary (cursor pivot)', () => {
    const camera: CameraState = { offsetX: 50, offsetY: 30, zoom: 1.2 };
    const cursorX = 300;
    const cursorY = 250;

    // World point under cursor before zoom
    const worldBefore = screenToWorld(cursorX, cursorY, camera);

    // Zoom in
    const newCamera = computeZoom(camera, cursorX, cursorY, -100);

    // World point under cursor after zoom
    const worldAfter = screenToWorld(cursorX, cursorY, newCamera);

    expect(worldAfter.x).toBeCloseTo(worldBefore.x, 10);
    expect(worldAfter.y).toBeCloseTo(worldBefore.y, 10);
  });
});

describe('round-trip: axialToPixel -> forward camera -> screenToWorld -> pixelToAxial', () => {
  const layout: HexLayoutConfig = { size: 30, origin: { x: 400, y: 300 } };
  const targetHex = { q: 2, r: -1 };

  function roundTrip(camera: CameraState) {
    // axialToPixel gives world-space pixel
    const worldPx = axialToPixel(targetHex, layout);

    // Forward camera transform: world -> screen
    const screenX = worldPx.x * camera.zoom + camera.offsetX;
    const screenY = worldPx.y * camera.zoom + camera.offsetY;

    // Inverse camera transform: screen -> world
    const worldBack = screenToWorld(screenX, screenY, camera);

    // pixelToAxial: world pixel -> axial coord
    return pixelToAxial(worldBack, layout);
  }

  it('returns (q=2, r=-1) at zoom 0.5 with pan offset', () => {
    expect(roundTrip({ offsetX: 120, offsetY: -50, zoom: 0.5 })).toEqual(targetHex);
  });

  it('returns (q=2, r=-1) at zoom 1.0 with pan offset', () => {
    expect(roundTrip({ offsetX: -30, offsetY: 40, zoom: 1.0 })).toEqual(targetHex);
  });

  it('returns (q=2, r=-1) at zoom 2.0 with pan offset', () => {
    expect(roundTrip({ offsetX: 200, offsetY: 100, zoom: 2.0 })).toEqual(targetHex);
  });
});
