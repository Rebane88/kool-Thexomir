import { describe, it, expect, vi } from 'vitest';
import { drawHexGrid } from './hex-renderer';
import type { HexLayoutConfig } from './types';
import { generateAxialCoords } from './hex-math';

function createMockCtx() {
  return {
    beginPath: vi.fn(),
    moveTo: vi.fn(),
    lineTo: vi.fn(),
    closePath: vi.fn(),
    stroke: vi.fn(),
    fillText: vi.fn(),
    fillStyle: '',
    strokeStyle: '',
    lineWidth: 0,
    font: '',
    textAlign: '',
    textBaseline: '',
  } as unknown as CanvasRenderingContext2D;
}

describe('drawHexGrid', () => {
  const layout: HexLayoutConfig = {
    size: 30,
    origin: { x: 400, y: 300 },
  };

  it('calls beginPath 169 times for radius 7', () => {
    const ctx = createMockCtx();
    const coords = generateAxialCoords(7);
    drawHexGrid(ctx, coords, layout);
    expect((ctx.beginPath as ReturnType<typeof vi.fn>).mock.calls).toHaveLength(169);
  });

  it('calls fillText 169 times with coordinate strings', () => {
    const ctx = createMockCtx();
    const coords = generateAxialCoords(7);
    drawHexGrid(ctx, coords, layout);
    expect((ctx.fillText as ReturnType<typeof vi.fn>).mock.calls).toHaveLength(169);
  });

  it('sets strokeStyle to amber-600 (#d97706)', () => {
    const ctx = createMockCtx();
    const coords = generateAxialCoords(7);
    drawHexGrid(ctx, coords, layout);
    expect(ctx.strokeStyle).toBe('#d97706');
  });

  it('sets font to 10px monospace', () => {
    const ctx = createMockCtx();
    const coords = generateAxialCoords(7);
    drawHexGrid(ctx, coords, layout);
    expect(ctx.font).toBe('10px monospace');
  });

  it('calls fillText with "0,0" for the center hex', () => {
    const ctx = createMockCtx();
    const coords = generateAxialCoords(7);
    drawHexGrid(ctx, coords, layout);
    const fillTextCalls = (ctx.fillText as ReturnType<typeof vi.fn>).mock.calls;
    const centerCall = fillTextCalls.find(
      (call: unknown[]) => call[0] === '0,0',
    );
    expect(centerCall).toBeDefined();
  });
});
