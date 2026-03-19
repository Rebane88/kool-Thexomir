export type GameStatus = 'Lobby' | 'InProgress' | 'Completed' | 'Cancelled';
export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'failed';
export type GamePhase = 'Action' | 'Battle' | 'Income' | 'RoundEnd';
export type BattleStep = 'DeclareAttack' | 'SelectArmies' | 'RevealArmies' | 'SetLineup' | 'Resolve';
