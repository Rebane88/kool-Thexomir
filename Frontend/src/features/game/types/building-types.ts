export interface BuildingTypeRef {
  id: string;
  /** Culture-independent slug used for icon lookup. */
  code: string;
  name: string;
  tier: number;
  chain: string;
  costGold: number;
  costFood: number;
  costWood: number;
  costStone: number;
  costMana: number;
  baseYieldGold: number;
  baseYieldFood: number;
  baseYieldWood: number;
  baseYieldStone: number;
  baseYieldMana: number;
  description: string | null;
  unlockedByBuildingTypeId: string | null;
  unlockedByBuildingName: string | null;
  armyCapacity: number;
}
