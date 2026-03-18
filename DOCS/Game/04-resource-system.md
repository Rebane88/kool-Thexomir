# Systems Reference — Resource System

> Exact rules for how resources are generated, spent, and managed. Reference this when implementing income phase logic and spending validation.

---

## The Five Resources

| Resource | Enum Value | Notes |
|----------|------------|-------|
| Gold | Gold | Primary currency — used for almost everything |
| Food | Food | Army upkeep — armies die without it |
| Wood | Wood | Basic construction |
| Stone | Stone | Advanced construction |
| Mana | Mana | Magical units only |

Each kingdom has exactly **5 KingdomResource rows**, one per type. Amount cannot go below 0.

---

## Starting Resources

Each kingdom starts with base resources plus their faction's starting bonus.

**⚠️ OPEN QUESTION:** Base starting resources for all kingdoms (before faction bonus) are not defined. Needs a decision:
- How much Gold does every player start with?
- How much Food, Wood, Stone, Mana?
- Suggested starting point to tune from: Gold 100, Food 50, Wood 30, Stone 10, Mana 0

Faction bonuses are added on top of base:

| Faction | Bonus |
|---------|-------|
| Iron Throne | +50 Gold |
| Mage Council | +30 Mana |
| Merchant Republic | +100 Gold |
| Forest Elves | +50 Wood |

---

## Resource Generation

Resources are generated during the **Income Phase** of each turn (see turn-structure.md).

### Formula

```
yield = BuildingType.BaseYield
      × terrainMultiplier
      × factionMultiplier
```

**terrainMultiplier:**
- Check if `TerrainType.BonusResourceType` matches `BuildingType.ResourceProduced`
- If yes: multiply by `TerrainType.ResourceMultiplier`
- If no match: multiplier = 1.0

**factionMultiplier:**
- Check `FactionType.BonusResourceType` against `BuildingType.ResourceProduced`
- If `BonusResourceType = null` (faction bonus applies to all): multiply by `FactionType.ResourceProductionBonus`
- If `BonusResourceType` matches: multiply by `FactionType.ResourceProductionBonus`
- If no match: multiplier = 1.0

**⚠️ OPEN QUESTION:** Are terrain multiplier and faction multiplier applied multiplicatively (stacked) or additively? Example: Forest Elves (+20% Wood) on a Forest tile (+20% Wood) — is it 1.44x or 1.4x total? Recommend multiplicative for interesting specialisation but needs decision.

**⚠️ OPEN QUESTION:** Do tiles without buildings generate any resources at all? Current assumption: no. Plains tiles with no Farm produce nothing.

---

## Spending Resources

Resources are deducted immediately when an action is taken. Server must validate the kingdom has sufficient resources before processing the action — return a 400 error if not.

**Actions and their costs:**

| Action | Resource Cost Source |
|--------|---------------------|
| Claim tile | Fixed Gold cost — **⚠️ OPEN QUESTION: what is the Gold cost to claim a tile?** |
| Construct building | `BuildingType.CostGold/Food/Wood/Stone/Mana` |
| Recruit unit | `UnitType.RecruitCostGold/Food/Wood/Stone/Mana` |

**Building costs with faction modifier:**
```
actualCost = BuildingType.cost × FactionType.BuildingCostModifier
```

Round to nearest integer. **⚠️ OPEN QUESTION:** Round up or round down? Recommend ceil to prevent exploiting small fractional discounts.

---

## Upkeep

Upkeep is collected during Income Phase Step 2 (after resource generation).

For each unit:
```
deduct UnitType.UpkeepGold from Gold
deduct UnitType.UpkeepFood from Food
```

### Upkeep Failure (Disbanding)

If after collecting upkeep a resource would go below 0:

1. Floor the resource at 0
2. Identify units the kingdom can no longer afford
3. Sort by most expensive upkeep first (define in open-questions.md)
4. Delete units one by one until remaining upkeep fits within available resources
5. Log each disbanded unit to TurnLog

**This means a kingdom that runs out of Food loses its army automatically.** This is intentional — it's the economic pressure mechanic.

---

## Resource Constraints

- Amount can never go below **0** — enforced at DB level and application level
- Amount has no defined upper cap — **⚠️ OPEN QUESTION:** Should there be a storage cap? E.g. max 500 of any resource without a Granary/Bank. A cap would make late-game resource management more interesting and prevent passive hoarding.
- `KingdomResource.UpdatedAt` is updated every time Amount changes

---

## Seeded Building Yields

**⚠️ OPEN QUESTION:** Exact BaseYield values per BuildingType are not defined. The schema has `BaseYield` but values need to be decided. Suggested starting values to balance from:

| Building | Tier | Resource | Suggested BaseYield |
|----------|------|----------|---------------------|
| Farm | 1 | Food | 10 |
| Windmill | 2 | Food | 20 |
| Granary | 3 | Food | 35 |
| Lumber Camp | 1 | Wood | 8 |
| Sawmill | 2 | Wood | 18 |
| Timber Hall | 3 | Wood | 30 |
| Quarry | 1 | Stone | 6 |
| Mason | 2 | Stone | 14 |
| Stoneworks | 3 | Stone | 25 |
| Market | 1 | Gold | 12 |
| Trading Post | 2 | Gold | 25 |
| Bank | 3 | Gold | 45 |
| Shrine | 1 | Mana | 5 |
| Wizard Tower | 2 | Mana | 12 |
| Arcane Sanctum | 3 | Mana | 22 |

These are placeholders — tune after playtesting.
