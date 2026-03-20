export interface BuildingTypeRef {
  id: string;
  name: string;
  tier: number;
  chain: string;
  goldCost: number;
  foodCost: number;
  woodCost: number;
  stoneCost: number;
  manaCost: number;
  goldYield: number;
  foodYield: number;
  woodYield: number;
  stoneYield: number;
  manaYield: number;
  description: string | null;
  prerequisiteBuildingTypeId: string | null;
  prerequisiteBuildingName: string | null;
  armyCapacity: number;
}
