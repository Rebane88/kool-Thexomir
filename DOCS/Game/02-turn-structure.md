# Systems Reference — Turn Structure

> This document defines the exact sequence of events within a single turn. Refer here when implementing turn processing logic.

---

## Turn Phases (In Order)

```
1. Turn Start
2. Action Phase        ← only the active player acts
3. Resolution Phase    ← battles resolve
4. Income Phase        ← resources generated, upkeep paid, events fire
5. Turn End
```

---

## Phase 1 — Turn Start

- Server sets `Game.CurrentTurnKingdomId` to the active kingdom
- If `Game.TurnTimeLimit` is set, calculate `Game.TurnDeadline = now + TurnTimeLimit`
- SignalR broadcasts `TurnChanged` event to all connected clients
- A `TurnLog` entry is created with `EventType = TurnStarted`

**⚠️ OPEN QUESTION:** What happens if `TurnDeadline` is reached and the player has not ended their turn? Does the server auto-end it? If so, which server process handles this check — a background job, or on the next incoming request?

---

## Phase 2 — Action Phase

The active player may perform any number of the following actions in any order via REST API calls. No other players may take actions during this phase.

### Available Actions

| Action | Endpoint (TBD) | Cost | Validation |
|--------|---------------|------|------------|
| Claim tile | POST /tiles/{id}/claim | Gold | Must be adjacent to owned tile, tile must be unclaimed and have no barbarian army |
| Construct building | POST /tiles/{id}/buildings | Resources | Tile must be owned, no existing building, unlock prereq met |
| Upgrade building | PUT /buildings/{id}/upgrade | Resources | Next tier must exist, prereq building on same tile |
| Recruit unit | POST /armies/{id}/units | Resources | Required building must exist on the tile the army is on |
| Move army | POST /armies/{id}/move | — | Destination must be owned tile or adjacent enemy tile |
| Attack | POST /armies/{id}/attack | — | Target tile must be adjacent, must be enemy-owned |

**⚠️ OPEN QUESTION:** Is there an action limit per turn (e.g. 5 actions max)? Or unlimited? Decision pending — see open-questions.md.

**⚠️ OPEN QUESTION:** Can a player attack multiple different tiles in one turn, or only one attack per turn?

**⚠️ OPEN QUESTION:** Can the same army both move AND attack in one turn, or does moving use up the attack?

### Action Logging

Every action creates a `TurnLog` entry with the appropriate `EventType`, human-readable `Description`, and JSON `Metadata`.

---

## Phase 3 — Resolution Phase

Triggered immediately when an attack action is taken (not batched to end of turn).

- Combat formula is evaluated (see combat-system.md)
- `Battle` record is created
- Units are destroyed (hard deleted), casualties recorded on `Battle`
- If attacker wins: `Tile.OwnerKingdomId` → attacker's KingdomId, `TileChangedOwner = true`
- If defender wins: tile ownership unchanged
- Surviving defender units remain on tile; surviving attacker units retreat if they lost

**Retreat rules:**
- Surviving attacker units after a loss move back to the tile they attacked from
- Surviving defender units after a win stay on the tile
- **⚠️ OPEN QUESTION:** What if the attacker's original tile was captured by someone else during the same turn? Where do retreating units go? Define fallback tile logic.
- **⚠️ OPEN QUESTION:** What if the defender loses and has no adjacent owned tile to retreat to? Are units destroyed? Captured?

### Barbarian Outcome

- If defending barbarian army is defeated: `Tile.OwnerKingdomId` remains null (does not transfer to attacker)
- Tile becomes claimable normally on the attacker's next action

---

## Phase 4 — Income Phase

Runs automatically after the player clicks End Turn. Processes in this exact order:

### Step 1 — Resource Generation

For each owned tile with a building:
```
yield = BuildingType.BaseYield
      × TerrainType.ResourceMultiplier (if terrain matches BonusResourceType)
      × FactionType.ResourceProductionBonus (if faction's BonusResourceType matches, or null = all)
```
Add yield to `KingdomResource.Amount` for the matching resource type.

**⚠️ OPEN QUESTION:** Do tiles without buildings generate any base resources, or only tiles with buildings?

### Step 2 — Upkeep Collection

For each unit owned by the kingdom:
- Deduct `UnitType.UpkeepGold` from Gold
- Deduct `UnitType.UpkeepFood` from Food

If Gold or Food would go below 0:
- Collect what is available (floor at 0)
- Disband (delete) units starting from most expensive upkeep first until the kingdom can afford remaining upkeep
- Log each disbanded unit as a `TurnLog` entry

**⚠️ OPEN QUESTION:** Define "most expensive" — is it UpkeepGold, UpkeepFood, or combined? What is the tiebreak order?

### Step 3 — GameEvent Check

**⚠️ OPEN QUESTION:** Random events are not yet implemented. Placeholder for when they are. See open-questions.md for event design decisions needed.

### Step 4 — Win Condition Check

After all income is processed:
- Check if any kingdom meets the active win condition (see win-conditions.md)
- If a kingdom is defeated (0 tiles): set `Kingdom.Status = Defeated`, `Kingdom.DefeatedAt = now`, log `KingdomDefeated` TurnLog entry, broadcast `KingdomDefeated` SignalR event
- If game is won: set `Game.Status = Finished`, `Game.WinnerKingdomId`, `Game.FinishedAt = now`, broadcast `GameOver` SignalR event

---

## Phase 5 — Turn End

- Advance `Game.TurnNumber` by 1
- Set `Game.CurrentTurnKingdomId` to the next kingdom in TurnOrder sequence
- Skip any kingdoms with `Status = Defeated`
- Create `TurnLog` entry with `EventType = TurnEnded`
- Begin Phase 1 for the next kingdom

**Turn order wrapping:** After the last kingdom in TurnOrder takes their turn, wrap back to TurnOrder = 1.

**⚠️ OPEN QUESTION:** Barbarian kingdom has TurnOrder = 0 and never takes a turn. Ensure turn order logic explicitly skips `IsBarbarianCamp = true` kingdoms at all times.

---

## TurnLog EventTypes Reference

| EventType | When Created |
|-----------|-------------|
| TurnStarted | Phase 1 |
| TileClaimed | Action Phase — tile claim |
| BuildingConstructed | Action Phase — build |
| UnitRecruited | Action Phase — recruit |
| ArmyMoved | Action Phase — move |
| BattleOccurred | Resolution Phase |
| ResourcesEarned | Income Phase Step 1 |
| UpkeepPaid | Income Phase Step 2 |
| KingdomDefeated | Income Phase Step 4 |
| TurnEnded | Phase 5 |
