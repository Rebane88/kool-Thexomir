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
