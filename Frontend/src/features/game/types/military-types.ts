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
