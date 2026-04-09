export interface Tile {
  id: string;
  coordQ: number;
  coordR: number;
  terrainTypeId: string;
  /** Culture-independent slug used for asset/color/pattern lookup (e.g. "plains"). */
  terrainCode: string;
  /** Localized display name — for tooltips/labels, never for lookup. */
  terrainName: string;
  kingdomId: string | null;
  isCastle: boolean;
  buildings: Building[];
}

export interface Building {
  id: string;
  buildingTypeId: string;
  /** Culture-independent slug used for icon lookup (e.g. "farm"). */
  buildingCode: string;
  /** Localized display name — for tooltips/labels, never for lookup. */
  buildingName: string;
}
