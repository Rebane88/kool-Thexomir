export interface CameraState {
  offsetX: number;
  offsetY: number;
  zoom: number;
}

export const DEFAULT_CAMERA: CameraState = { offsetX: 0, offsetY: 0, zoom: 1.0 };

/**
 * Convert screen (canvas) coordinates to world coordinates by inverting
 * the forward camera transform (translate then scale).
 *
 * Forward: screenX = worldX * zoom + offsetX
 * Inverse: worldX = (screenX - offsetX) / zoom
 */
export function screenToWorld(
  screenX: number,
  screenY: number,
  camera: CameraState,
): { x: number; y: number } {
  return {
    x: (screenX - camera.offsetX) / camera.zoom,
    y: (screenY - camera.offsetY) / camera.zoom,
  };
}

/**
 * Compute new camera state after a zoom event, pivoting around the cursor
 * position so the world point under the cursor stays stationary.
 *
 * deltaY < 0 = zoom in, deltaY > 0 = zoom out (matches wheel convention).
 */
export function computeZoom(
  camera: CameraState,
  cursorX: number,
  cursorY: number,
  deltaY: number,
  zoomFactor: number = 1.1,
  minZoom: number = 0.5,
  maxZoom: number = 2.0,
): CameraState {
  const direction = deltaY < 0 ? zoomFactor : 1 / zoomFactor;
  const newZoom = Math.min(maxZoom, Math.max(minZoom, camera.zoom * direction));

  // World point under cursor (must remain fixed after zoom)
  const worldX = (cursorX - camera.offsetX) / camera.zoom;
  const worldY = (cursorY - camera.offsetY) / camera.zoom;

  return {
    offsetX: cursorX - worldX * newZoom,
    offsetY: cursorY - worldY * newZoom,
    zoom: newZoom,
  };
}
