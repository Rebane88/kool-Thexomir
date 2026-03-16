namespace Domain.Game;

public record WinCheckResult(Guid? WinnerKingdomId, EWinCondition WinConditionType, bool GameOver);
