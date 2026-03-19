export interface Army {
  id: string;
  buildingId: string;
  kingdomId: string;
  armyTypeId: string;
  currentHP: number;
  maxHP: number;
}

export interface ArmyTypeRef {
  id: string;
  name: string;
  attack: number;
  hp: number;
  initiative: number;
  damageRangeMin: number;
  damageRangeMax: number;
  chipDamageRangeMin: number;
  chipDamageRangeMax: number;
  situationalBonusStat: string | null;
  situationalBonusValue: number | null;
  situationalBonusCondition: string | null;
  trainingCostGold: number;
  trainingCostFood: number;
  trainingCostStone: number;
  trainingCostMana: number;
  upkeepGold: number;
  upkeepFood: number;
  upkeepMana: number;
  requiredBuildingTypeId: string;
  requiredBuildingName: string | null;
}

export interface DeclaredAttack {
  attackId: string;
  targetTileId: string;
  riskedTileId: string;
  attackerKingdomId: string;
  defenderKingdomId: string;
}
