# Systems Reference — Win Conditions

> Win condition rules and victory processing.

---

## Overview

The only win condition is **Elimination**. Last kingdom standing wins. Max 4 players per game.

Victory is checked at the end of the Income Phase each round (after resource generation and upkeep).

**Max round limit:** If the game reaches **100 rounds** without a winner, the game ends in a **draw** (admin-editable). `Game.WinnerKingdomId = null`.

---

## Elimination

**Win condition:** Last kingdom with `Status = Active` wins.

A kingdom is eliminated when their **Castle is destroyed** (their castle tile is captured by an opponent).

```
if kingdom's castle tile is captured
  → Kingdom.Status = Defeated
  → broadcast KingdomDefeated

if count of Active kingdoms == 1
  → that kingdom wins
```

### Castle Destruction

- The castle is destroyed when the tile it sits on is captured through combat
- Castle battles are 5v5 (instead of standard 3v3)
- Once a castle is destroyed, it **cannot be rebuilt**
- All armies owned by the defeated kingdom are **deleted**
- All buildings on the defeated kingdom's tiles are **destroyed**
- All tiles owned by the defeated kingdom become **unowned** — other players can expand into them via building

### Simultaneous Defeat

If two kingdoms lose their castles in the same combat phase (both attacked each other's castle), **both are eliminated**. If they are the last two kingdoms, the game ends in a **draw** — no winner.

```
if count of Active kingdoms == 0
  → Game ends in a draw (Game.WinnerKingdomId = null)
```

---

## Game Modes

### Free-for-All (2–4 players)
Standard game. Every player for themselves. Last one standing wins.

### 2v2 Team Mode (Future)
Two teams of two. A team wins when both opposing castles are destroyed. Deferred — design later.

---

## Victory Processing

When a win condition is met:

1. Set `Game.Status = Finished`
2. Set `Game.WinnerKingdomId` = winning kingdom
3. Set `Game.FinishedAt = now`
4. Broadcast `GameOver` SignalR event to all clients
5. No further rounds can be taken

---

## Kingdom Defeat

When a kingdom is eliminated:

1. `Kingdom.Status = Defeated`
2. `Kingdom.DefeatedAt = now`
3. All armies owned by the kingdom are **deleted**
4. All buildings on the kingdom's tiles are **destroyed**
5. All tiles owned by the kingdom become **unowned** — other players can expand into them via building
6. TurnLog entry: `EventType = KingdomDefeated`
7. SignalR broadcasts `KingdomDefeated`
8. Defeated kingdom is skipped in all future action phases
