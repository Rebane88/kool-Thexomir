export interface Army {
  id: string;
  tileId: string;
  kingdomId: string;
  units: ArmyUnit[];
}

export interface ArmyUnit {
  unitTypeId: string;
  unitTypeName: string;
  quantity: number;
}

export interface Casualty {
  unitTypeId: string;
  unitTypeName: string;
  before: number;
  lost: number;
}

export interface UnitTypeRef {
  id: string;
  name: string;
  baseStrength: number;
  goldCost: number;
  foodCost: number;
  woodCost: number;
  stoneCost: number;
  manaCost: number;
  upkeep: number;
  description: string | null;
  producedByBuildingTypeIds: string[];
}
