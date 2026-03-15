export interface CreateLobbyResponse {
  lobbyId: string;
  inviteCode: string;
}

export interface LobbyResponse {
  id: string;
  lobbyCode: string;
  status: number; // EGameStatus: 0=Lobby, 1=InProgress, 2=Completed
  maxPlayers: number;
  winCondition: number; // EWinCondition: 0=Domination, 1=Elimination, 2=Score
  hostUserId: string | null;
  playerCount: number;
  players: PlayerInLobbyDto[];
  factions: FactionAvailabilityDto[];
}

export interface PlayerInLobbyDto {
  kingdomId: string;
  userId: string;
  userEmail: string;
  factionTypeId: string | null;
  factionName: string | null;
  isHost: boolean;
}

export interface FactionAvailabilityDto {
  factionTypeId: string;
  name: string;
  isAvailable: boolean;
}

// Display mappings for numeric enums
export const WIN_CONDITION_LABELS: Record<number, string> = {
  0: 'Domination',
  1: 'Elimination',
  2: 'Score',
};

export const GAME_STATUS = {
  Lobby: 0,
  InProgress: 1,
  Completed: 2,
} as const;
