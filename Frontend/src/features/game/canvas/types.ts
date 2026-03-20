export interface Point2D {
  x: number;
  y: number;
}

export interface AxialCoord {
  q: number;
  r: number;
}

export interface HexLayoutConfig {
  size: number;
  origin: Point2D;
}

export const HEX_BORDER_COLOR = '#1a1a24';
export const SELECTION_COLOR = '#c9a84c';
export const HOVER_COLOR = 'rgba(201, 168, 76, 0.5)';
export const CAPITAL_COLOR = '#c9a84c';

/** Per-kingdom index colors assigned by insertion order */
export const KINGDOM_COLORS: string[] = [
  '#cc3333', // K1: Red
  '#3366cc', // K2: Blue
  '#ccaa33', // K3: Gold
  '#33aa55', // K4: Green
  '#33aaaa', // K5: Teal
  '#cc7733', // K6: Orange
  '#cc66aa', // K7: Pink
  '#8b6640', // K8: Brown
];

export interface PlacementAnimation {
  tileKey: string;
  startTime: number;
  claimedTileKeys: string[];
}

/** State passed from GamePage to the draw function for hover/selection */
export interface MapRenderState {
  hoveredTileKey: string | null;
  selectedTileKey: string | null;
  buildModeTypeId: string | null;
  /** When build mode is an upgrade, the building type ID it upgrades from */
  buildModeUpgradesFrom: string | null;
  armyHighlightTileKey: string | null;
  assetsReady: boolean;
  declareAttackGlowTileKeys: Set<string> | null;
  placementAnimations: PlacementAnimation[];
}
