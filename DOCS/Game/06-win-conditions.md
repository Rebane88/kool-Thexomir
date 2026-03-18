# Systems Reference — Win Conditions

> Exact rules for each win condition and how victory is checked.

---

## Overview

Win condition is set by the game creator in the lobby and stored as `Game.WinCondition`. It cannot be changed after the game starts.

Victory is checked at the end of the Income Phase each turn (after resource generation, upkeep, and events).

---

## Domination

**Trigger field:** `Game.DominationThreshold` (required when WinCondition = Domination)

**Win condition:** First kingdom to own `DominationThreshold`% or more of all tiles on the map wins.

```
ownedTiles = count of tiles where OwnerKingdomId = this kingdom
totalTiles = Game.MapWidth × Game.MapHeight
ownershipPercent = (ownedTiles / totalTiles) × 100

if ownershipPercent >= Game.DominationThreshold → this kingdom wins
```

**⚠️ OPEN QUESTION:** What is the recommended default DominationThreshold? Suggest 60% as a starting point — high enough to require real conquest but not so high the game drags. Host can configure.

**⚠️ OPEN QUESTION:** Does Domination threshold count barbarian-owned tiles in `totalTiles`? Current assumption: yes, all tiles count. If barbarian tiles are excluded, expansion is harder to calculate accurately.

---

## Elimination

**Win condition:** Last kingdom with `Status = Active` wins.

A kingdom is eliminated when it has zero owned tiles — `Status` is set to `Defeated` during the Income Phase win condition check.

```
if count of tiles where OwnerKingdomId = this kingdom == 0
  → Kingdom.Status = Defeated
  → broadcast KingdomDefeated

if count of Active kingdoms == 1
  → that kingdom wins
```

**⚠️ OPEN QUESTION:** If the last two kingdoms are defeated simultaneously in the same turn (e.g. both lose their last tile in the same battle resolution), who wins? Options: draw, or check alphabetically/by TurnOrder. Needs tiebreak rule.

---

## Score

**Trigger fields:** `Game.MaxTurns` (required when WinCondition = Score)

**Win condition:** After `MaxTurns` turns, the kingdom with the highest score wins.

### Score Formula

**⚠️ OPEN QUESTION:** The exact score formula is not yet defined. Needs to include military, economic, and territory components. Suggested formula:

```
score = (ownedTiles × 10)
      + (totalUnitStrength × 2)
      + (totalResourceIncome × 5)
      + (buildingCount × 15)
```

All weights are placeholder — tune after playtesting.

**⚠️ OPEN QUESTION:** Is the score visible to all players in real time, or revealed only at game end? Recommend visible — creates interesting strategic tension around score racing.

**⚠️ OPEN QUESTION:** Is there a tiebreak if two kingdoms have identical scores at MaxTurns? Suggested tiebreak: most tiles owned. If still tied, higher total resource income.

---

## Victory Processing

When a win condition is met:

1. Set `Game.Status = Finished`
2. Set `Game.WinnerKingdomId` = winning kingdom
3. Set `Game.FinishedAt = now`
4. Broadcast `GameOver` SignalR event to all clients
5. No further turns can be taken

**⚠️ OPEN QUESTION:** What happens to players who are still active when the game ends via Score or Domination? They simply stop being able to take actions. Confirm no cleanup needed.

---

## Kingdom Defeat (All Modes)

Regardless of win condition, a kingdom is defeated when it loses its last tile:

1. `Kingdom.Status = Defeated`
2. `Kingdom.DefeatedAt = now`
3. All remaining armies and units owned by that kingdom — **⚠️ OPEN QUESTION:** Are they deleted, or do they remain as neutral entities? Current assumption: deleted. Confirm.
4. TurnLog entry: `EventType = KingdomDefeated`
5. SignalR broadcasts `KingdomDefeated`
6. Defeated kingdom is skipped in all future turn orders
