export interface BuildingTypeRef {
  id: string;
  name: string;
  tier: number;
  chain: string;
  goldCost: number;
  woodCost: number;
  stoneCost: number;
  manaCost: number;
  foodYield: number;
  woodYield: number;
  stoneYield: number;
  goldYield: number;
  manaYield: number;
  description: string | null;
  prerequisiteBuildingTypeId: string | null;
  prerequisiteBuildingName: string | null;
}
