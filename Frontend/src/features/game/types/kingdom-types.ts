export interface Kingdom {
  id: string;
  name: string;
  userId: string | null;
  factionTypeId: string | null;
  factionName: string | null;
  isEliminated: boolean;
  resources: Record<string, number>;
}
