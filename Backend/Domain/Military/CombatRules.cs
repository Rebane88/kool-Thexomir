using Domain.Map;

namespace Domain.Military;

public static class CombatRules
{
    /// <summary>
    /// Result of a single combat round between two armies.
    /// </summary>
    public record CombatRoundResult(
        EBattleOutcome InitiativeWinner,
        decimal AttackerInitiativeChance,
        decimal DefenderInitiativeChance,
        int DamageDealt,
        int ChipDamageDealt,
        int AttackerHPAfter,
        int DefenderHPAfter,
        Guid? ArmyDestroyedId);

    /// <summary>
    /// Result of a full battle between two lineups.
    /// </summary>
    public record BattleResult(
        EBattleOutcome Outcome,
        List<CombatRoundResult> Rounds,
        List<Guid> DestroyedArmyIds,
        Dictionary<Guid, int> SurvivingArmyHP);

    /// <summary>
    /// Validates whether an attack declaration is valid.
    /// Returns null on success, error message on failure.
    /// </summary>
    public static string? ValidateAttackDeclaration(
        Tile targetTile,
        Tile riskedTile,
        Guid attackerKingdomId,
        List<(int q, int r)> attackerTileCoords,
        int attackerArmyCount,
        HashSet<Guid> lockedTileIds)
    {
        // Target must be enemy-owned (not null, not attacker)
        if (targetTile.KingdomId is null)
            return "Target tile is not owned by any kingdom.";

        if (targetTile.KingdomId == attackerKingdomId)
            return "Cannot attack your own tile.";

        // Neither tile can be locked
        if (lockedTileIds.Contains(targetTile.Id))
            return "Target tile is already locked in another battle.";

        if (lockedTileIds.Contains(riskedTile.Id))
            return "Risked tile is already locked in another battle.";

        // Target must border attacker territory
        var targetNeighbors = HexGridHelper.GetNeighbors(targetTile.CoordQ, targetTile.CoordR);
        var bordersAttacker = targetNeighbors.Any(n => attackerTileCoords.Contains(n));
        if (!bordersAttacker)
            return "Target tile does not border your territory.";

        // Risked must be owned by attacker
        if (riskedTile.KingdomId != attackerKingdomId)
            return "Risked tile is not owned by you.";

        // Risked must be adjacent to target
        var riskedNeighbors = HexGridHelper.GetNeighbors(riskedTile.CoordQ, riskedTile.CoordR);
        var isAdjacent = riskedNeighbors.Any(n => n == (targetTile.CoordQ, targetTile.CoordR));
        if (!isAdjacent)
            return "Risked tile is not adjacent to target tile.";

        // Attacker must have armies
        if (attackerArmyCount <= 0)
            return "You have no armies to attack with.";

        return null;
    }

    /// <summary>
    /// Returns maximum armies allowed in battle (5 if either tile is castle, 3 otherwise).
    /// </summary>
    public static int GetMaxArmies(bool targetIsCastle, bool riskedIsCastle)
    {
        return (targetIsCastle || riskedIsCastle) ? 5 : 3;
    }

    /// <summary>
    /// Validates army selection for battle.
    /// Returns null on success, error message on failure.
    /// </summary>
    public static string? ValidateArmySelection(
        List<Guid> selectedArmyIds,
        int maxArmies,
        HashSet<Guid> committedArmyIds)
    {
        if (selectedArmyIds.Count == 0)
            return "Must select at least one army.";

        if (selectedArmyIds.Count > maxArmies)
            return $"Cannot select more than {maxArmies} armies.";

        var committed = selectedArmyIds.FirstOrDefault(id => committedArmyIds.Contains(id));
        if (committed != default)
            return "One or more selected armies are already committed to another battle.";

        return null;
    }

    /// <summary>
    /// Auto-selects strongest available armies by Attack descending, excluding committed armies.
    /// </summary>
    public static List<Army> AutoSelectArmies(
        List<(Army Army, ArmyType ArmyType)> availableArmies,
        int maxArmies,
        HashSet<Guid> committedArmyIds)
    {
        return availableArmies
            .Where(a => !committedArmyIds.Contains(a.Army.Id))
            .OrderByDescending(a => a.ArmyType.Attack)
            .Take(maxArmies)
            .Select(a => a.Army)
            .ToList();
    }

    /// <summary>
    /// Resolves a single combat round between two armies.
    /// Step 1: Initiative roll. Step 2: Winner deals damage to loser.
    /// Step 3: Loser deals chip damage to winner. Step 4: Apply HP changes.
    /// Double-kill: ArmyDestroyedId = initiative loser.
    /// </summary>
    public static CombatRoundResult ResolveRound(
        int attackerEffAtk, int attackerInit,
        decimal atkDmgMin, decimal atkDmgMax, decimal atkChipMin, decimal atkChipMax,
        int attackerHP, Guid attackerArmyId,
        int defenderEffAtk, int defenderInit,
        decimal defDmgMin, decimal defDmgMax, decimal defChipMin, decimal defChipMax,
        int defenderHP, Guid defenderArmyId,
        Random random)
    {
        // Step 1: Initiative
        decimal totalInit = attackerInit + defenderInit;
        decimal attackerChance = totalInit == 0 ? 0.5m : (decimal)attackerInit / totalInit;
        decimal defenderChance = 1m - attackerChance;
        double roll = random.NextDouble();
        var initiativeWinner = (decimal)roll < attackerChance
            ? EBattleOutcome.AttackerWon
            : EBattleOutcome.DefenderWon;

        // Determine winner/loser stats
        int winnerEffAtk, loserEffAtk;
        decimal winnerDmgMin, winnerDmgMax, loserChipMin, loserChipMax;

        if (initiativeWinner == EBattleOutcome.AttackerWon)
        {
            winnerEffAtk = attackerEffAtk;
            winnerDmgMin = atkDmgMin;
            winnerDmgMax = atkDmgMax;
            loserEffAtk = defenderEffAtk;
            loserChipMin = defChipMin;
            loserChipMax = defChipMax;
        }
        else
        {
            winnerEffAtk = defenderEffAtk;
            winnerDmgMin = defDmgMin;
            winnerDmgMax = defDmgMax;
            loserEffAtk = attackerEffAtk;
            loserChipMin = atkChipMin;
            loserChipMax = atkChipMax;
        }

        // Step 2: Winner deals damage to loser
        double dmgRoll = random.NextDouble();
        decimal dmgMultiplier = winnerDmgMin + (decimal)dmgRoll * (winnerDmgMax - winnerDmgMin);
        int damage = (int)(winnerEffAtk * dmgMultiplier);

        // Step 3: Loser deals chip damage to winner
        double chipRoll = random.NextDouble();
        decimal chipMultiplier = loserChipMin + (decimal)chipRoll * (loserChipMax - loserChipMin);
        int chipDamage = (int)(loserEffAtk * chipMultiplier);

        // Step 4: Apply damage
        int newAttackerHP, newDefenderHP;
        if (initiativeWinner == EBattleOutcome.AttackerWon)
        {
            // Attacker wins: damage to defender (loser), chip to attacker (winner)
            newDefenderHP = Math.Max(0, defenderHP - damage);
            newAttackerHP = Math.Max(0, attackerHP - chipDamage);
        }
        else
        {
            // Defender wins: damage to attacker (loser), chip to defender (winner)
            newAttackerHP = Math.Max(0, attackerHP - damage);
            newDefenderHP = Math.Max(0, defenderHP - chipDamage);
        }

        // Determine destroyed army
        Guid? destroyedId = null;
        bool attackerDead = newAttackerHP <= 0;
        bool defenderDead = newDefenderHP <= 0;

        if (attackerDead && defenderDead)
        {
            // Double-kill: initiative loser is "destroyed"
            destroyedId = initiativeWinner == EBattleOutcome.AttackerWon
                ? defenderArmyId
                : attackerArmyId;
        }
        else if (attackerDead)
        {
            destroyedId = attackerArmyId;
        }
        else if (defenderDead)
        {
            destroyedId = defenderArmyId;
        }

        return new CombatRoundResult(
            InitiativeWinner: initiativeWinner,
            AttackerInitiativeChance: attackerChance,
            DefenderInitiativeChance: defenderChance,
            DamageDealt: damage,
            ChipDamageDealt: chipDamage,
            AttackerHPAfter: newAttackerHP,
            DefenderHPAfter: newDefenderHP,
            ArmyDestroyedId: destroyedId);
    }

    /// <summary>
    /// Resolves a full battle between two army lineups.
    /// Loops rounds until one side has no armies remaining.
    /// Uses ArmyRules.CalculateEffectiveAttack for HP-scaled attack.
    /// </summary>
    public static BattleResult ResolveBattle(
        List<(Army Army, ArmyType ArmyType, ArmyRules.ArmyCombatStats Stats)> attackerLineup,
        List<(Army Army, ArmyType ArmyType, ArmyRules.ArmyCombatStats Stats)> defenderLineup,
        bool attackerIsAttacker,
        Random random)
    {
        var rounds = new List<CombatRoundResult>();
        var destroyedIds = new List<Guid>();
        var survivingHP = new Dictionary<Guid, int>();

        // Empty lineup = instant loss
        if (attackerLineup.Count == 0)
        {
            return new BattleResult(EBattleOutcome.DefenderWon, rounds, destroyedIds, survivingHP);
        }

        if (defenderLineup.Count == 0)
        {
            // Track surviving attacker HP
            foreach (var entry in attackerLineup)
                survivingHP[entry.Army.Id] = entry.Army.CurrentHP;

            return new BattleResult(EBattleOutcome.AttackerWon, rounds, destroyedIds, survivingHP);
        }

        // Mutable HP tracking
        var attackerHP = new Dictionary<Guid, int>();
        foreach (var entry in attackerLineup)
            attackerHP[entry.Army.Id] = entry.Army.CurrentHP;

        var defenderHP = new Dictionary<Guid, int>();
        foreach (var entry in defenderLineup)
            defenderHP[entry.Army.Id] = entry.Army.CurrentHP;

        int atkIdx = 0;
        int defIdx = 0;

        while (atkIdx < attackerLineup.Count && defIdx < defenderLineup.Count)
        {
            var atk = attackerLineup[atkIdx];
            var def = defenderLineup[defIdx];

            int currentAtkHP = attackerHP[atk.Army.Id];
            int currentDefHP = defenderHP[def.Army.Id];

            // Calculate effective attack based on current HP
            int atkEffAtk = ArmyRules.CalculateEffectiveAttack(
                atk.Stats.Attack, currentAtkHP, atk.Army.MaxHP);
            int defEffAtk = ArmyRules.CalculateEffectiveAttack(
                def.Stats.Attack, currentDefHP, def.Army.MaxHP);

            var roundResult = ResolveRound(
                attackerEffAtk: atkEffAtk,
                attackerInit: atk.Stats.Initiative,
                atkDmgMin: atk.ArmyType.DamageRangeMin,
                atkDmgMax: atk.ArmyType.DamageRangeMax,
                atkChipMin: atk.Stats.ChipDamageRangeMin,
                atkChipMax: atk.Stats.ChipDamageRangeMax,
                attackerHP: currentAtkHP,
                attackerArmyId: atk.Army.Id,
                defenderEffAtk: defEffAtk,
                defenderInit: def.Stats.Initiative,
                defDmgMin: def.ArmyType.DamageRangeMin,
                defDmgMax: def.ArmyType.DamageRangeMax,
                defChipMin: def.Stats.ChipDamageRangeMin,
                defChipMax: def.Stats.ChipDamageRangeMax,
                defenderHP: currentDefHP,
                defenderArmyId: def.Army.Id,
                random: random);

            rounds.Add(roundResult);

            // Update HP
            attackerHP[atk.Army.Id] = roundResult.AttackerHPAfter;
            defenderHP[def.Army.Id] = roundResult.DefenderHPAfter;

            bool atkDead = roundResult.AttackerHPAfter <= 0;
            bool defDead = roundResult.DefenderHPAfter <= 0;

            if (atkDead && defDead)
            {
                // Double-kill: initiative winner's side advances
                destroyedIds.Add(atk.Army.Id);
                destroyedIds.Add(def.Army.Id);

                if (roundResult.InitiativeWinner == EBattleOutcome.AttackerWon)
                {
                    // Attacker won initiative: defender is "truly" destroyed first
                    // Both advance
                    atkIdx++;
                    defIdx++;
                }
                else
                {
                    // Defender won initiative: attacker is "truly" destroyed first
                    atkIdx++;
                    defIdx++;
                }
            }
            else if (atkDead)
            {
                destroyedIds.Add(atk.Army.Id);
                atkIdx++;
            }
            else if (defDead)
            {
                destroyedIds.Add(def.Army.Id);
                defIdx++;
            }
        }

        // Determine outcome
        EBattleOutcome outcome;
        if (atkIdx >= attackerLineup.Count && defIdx >= defenderLineup.Count)
        {
            // Both sides exhausted: last initiative winner determines outcome
            var lastRound = rounds[^1];
            outcome = lastRound.InitiativeWinner;
        }
        else if (atkIdx >= attackerLineup.Count)
        {
            outcome = EBattleOutcome.DefenderWon;
        }
        else
        {
            outcome = EBattleOutcome.AttackerWon;
        }

        // Track surviving army HP
        for (int i = atkIdx; i < attackerLineup.Count; i++)
        {
            var entry = attackerLineup[i];
            survivingHP[entry.Army.Id] = attackerHP[entry.Army.Id];
        }

        for (int i = defIdx; i < defenderLineup.Count; i++)
        {
            var entry = defenderLineup[i];
            survivingHP[entry.Army.Id] = defenderHP[entry.Army.Id];
        }

        return new BattleResult(outcome, rounds, destroyedIds, survivingHP);
    }

    /// <summary>
    /// Maps battle outcome to tile capture result.
    /// AttackerWon: captures defender target tile.
    /// DefenderWon: captures attacker risked tile.
    /// </summary>
    public static (Guid tileCapturedId, Guid tileCapturedFromKingdomId) GetBattleOutcome(
        EBattleOutcome outcome,
        Guid attackerKingdomId,
        Guid defenderKingdomId,
        Guid attackerRiskedTileId,
        Guid defenderTargetTileId)
    {
        return outcome == EBattleOutcome.AttackerWon
            ? (defenderTargetTileId, defenderKingdomId)
            : (attackerRiskedTileId, attackerKingdomId);
    }
}
