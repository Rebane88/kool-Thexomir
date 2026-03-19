export interface Tile {
  id: string;
  coordQ: number;
  coordR: number;
  terrainTypeId: string;
  terrainName: string;
  kingdomId: string | null;
  isCastle: boolean;
  isCapital: boolean;
  buildings: Building[];
}

export interface Building {
  id: string;
  buildingTypeId: string;
  buildingName: string;
}
