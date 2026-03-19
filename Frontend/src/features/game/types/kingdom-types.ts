export interface Kingdom {
  id: string;
  name: string;
  userId: string | null;
  factionTypeId: string;
  factionName: string | null;
  status: string;
  resources: Record<string, number>;
}
