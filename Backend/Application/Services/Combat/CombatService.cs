using Application.Contracts;
using Application.Services.Combat.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Military;
using MilitaryArmy = Domain.Military.Army;

namespace Application.Services.Combat;

public class CombatService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : ICombatService
{
    public async Task<Result<DeclareAttackResponse>> DeclareAttackAsync(
        Guid gameId, Guid userId, DeclareAttackRequest request)
    {
        // 1. Validate game/kingdom access (costs 1 AP)
        var guardResult = await gameGuard.ValidateActionAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<DeclareAttackResponse>.Fail(guardResult.Error!);

        var game = guardResult.Value!.Game;
        var kingdom = guardResult.Value!.Kingdom;

        // 2. Load target tile
        var targetTile = await unitOfWork.Tiles.GetByIdAsync(request.TargetTileId);
        if (targetTile is null)
            return Result<DeclareAttackResponse>.Fail("Target tile not found.");

        // 3. Load risked tile
        var riskedTile = await unitOfWork.Tiles.GetByIdAsync(request.RiskedTileId);
        if (riskedTile is null)
            return Result<DeclareAttackResponse>.Fail("Risked tile not found.");

        // 4. Load attacker tiles for adjacency check
        var attackerTiles = await unitOfWork.Tiles.GetTilesForKingdomAsync(kingdom.Id);
        var attackerTileCoords = attackerTiles.Select(t => (t.CoordQ, t.CoordR)).ToList();

        // 5. Get attacker army count
        var armies = await unitOfWork.Armies.GetArmiesForKingdomAsync(kingdom.Id);
        var armyCount = armies.Count();

        // 6. Get locked tile IDs from existing declared attacks
        var lockedTileIds = await unitOfWork.DeclaredAttacks
            .GetLockedTileIdsForGameRoundAsync(game.Id, game.RoundNumber);

        // 7. Validate attack declaration
        var validationError = CombatRules.ValidateAttackDeclaration(
            targetTile, riskedTile, kingdom.Id,
            attackerTileCoords, armyCount, lockedTileIds);
        if (validationError is not null)
            return Result<DeclareAttackResponse>.Fail(validationError);

        // 8. Create DeclaredAttack entity
        var declaredAttack = new DeclaredAttack
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            RoundNumber = game.RoundNumber,
            AttackerKingdomId = kingdom.Id,
            DefenderKingdomId = targetTile.KingdomId!.Value,
            TargetTileId = request.TargetTileId,
            RiskedTileId = request.RiskedTileId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.DeclaredAttacks.AddAsync(declaredAttack);

        // 9. Create TurnLog
        var turnLog = new TurnLog
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            KingdomId = kingdom.Id,
            RoundNumber = game.RoundNumber,
            EventType = EEventType.AttackDeclared,
            Description = $"Declared attack on ({targetTile.CoordQ},{targetTile.CoordR}) risking ({riskedTile.CoordQ},{riskedTile.CoordR})",
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.TurnLogs.AddAsync(turnLog);

        // 10. Commit
        await unitOfWork.CommitAsync();

        // 11. Return response
        return Result<DeclareAttackResponse>.Ok(new DeclareAttackResponse
        {
            AttackId = declaredAttack.Id,
            TargetTileId = declaredAttack.TargetTileId,
            RiskedTileId = declaredAttack.RiskedTileId,
            AttackerKingdomId = declaredAttack.AttackerKingdomId,
            DefenderKingdomId = declaredAttack.DefenderKingdomId
        });
    }

    public async Task<Result<BattleSetupDto>> SelectArmiesAsync(
        Guid gameId, Guid userId, SelectArmiesRequest request)
    {
        // 1. Load game
        var game = await unitOfWork.Games.GetByIdWithLockAsync(gameId);
        if (game is null) return Result<BattleSetupDto>.Fail("Game not found.");
        if (game.Status != EGameStatus.InProgress) return Result<BattleSetupDto>.Fail("Game is not in progress.");
        if (game.CurrentPhase != EGamePhase.Battle) return Result<BattleSetupDto>.Fail("Army selection only available during Battle Phase.");

        // 2. Load player's kingdom
        var kingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, gameId);
        if (kingdom is null) return Result<BattleSetupDto>.Fail("You are not in this game.");

        // 3. Load declared attack
        var attack = await unitOfWork.DeclaredAttacks.GetByIdAsync(request.DeclaredAttackId);
        if (attack is null) return Result<BattleSetupDto>.Fail("Declared attack not found.");
        if (attack.GameId != gameId) return Result<BattleSetupDto>.Fail("Attack does not belong to this game.");

        // 4. Verify player is attacker or defender
        bool isAttacker = attack.AttackerKingdomId == kingdom.Id;
        bool isDefender = attack.DefenderKingdomId == kingdom.Id;
        if (!isAttacker && !isDefender)
            return Result<BattleSetupDto>.Fail("You are not involved in this battle.");

        // 5. Load target and risked tiles for castle check
        var targetTile = await unitOfWork.Tiles.GetByIdAsync(attack.TargetTileId);
        var riskedTile = await unitOfWork.Tiles.GetByIdAsync(attack.RiskedTileId);
        var maxArmies = CombatRules.GetMaxArmies(targetTile!.IsCastle, riskedTile!.IsCastle);

        // 6. Validate army selection
        var validationError = CombatRules.ValidateArmySelection(request.ArmyIds, maxArmies, new HashSet<Guid>());
        if (validationError is not null)
            return Result<BattleSetupDto>.Fail(validationError);

        // 7. Verify all army IDs belong to the player's kingdom
        var playerArmies = await unitOfWork.Armies.GetArmiesForKingdomAsync(kingdom.Id);
        var playerArmyIds = playerArmies.Select(a => a.Id).ToHashSet();
        var invalidArmies = request.ArmyIds.Where(id => !playerArmyIds.Contains(id)).ToList();
        if (invalidArmies.Count != 0)
            return Result<BattleSetupDto>.Fail("One or more selected armies do not belong to your kingdom.");

        // 8. Store selection
        if (isAttacker)
            attack.AttackerSelectedArmyIds = string.Join(",", request.ArmyIds);
        else
            attack.DefenderSelectedArmyIds = string.Join(",", request.ArmyIds);

        attack.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.DeclaredAttacks.UpdateAsync(attack);
        await unitOfWork.CommitAsync();

        return Result<BattleSetupDto>.Ok(new BattleSetupDto
        {
            DeclaredAttackId = attack.Id,
            KingdomId = kingdom.Id,
            ArmiesSelected = request.ArmyIds.Count,
            MaxArmies = maxArmies
        });
    }

    public async Task<Result<ArmyRevealDto>> GetArmyRevealAsync(
        Guid gameId, Guid userId, Guid declaredAttackId)
    {
        // 1. Validate game/player/attack access
        var game = await unitOfWork.Games.GetByIdWithLockAsync(gameId);
        if (game is null) return Result<ArmyRevealDto>.Fail("Game not found.");
        if (game.Status != EGameStatus.InProgress) return Result<ArmyRevealDto>.Fail("Game is not in progress.");
        if (game.CurrentPhase != EGamePhase.Battle) return Result<ArmyRevealDto>.Fail("Army reveal only available during Battle Phase.");

        var kingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, gameId);
        if (kingdom is null) return Result<ArmyRevealDto>.Fail("You are not in this game.");

        var attack = await unitOfWork.DeclaredAttacks.GetByIdAsync(declaredAttackId);
        if (attack is null) return Result<ArmyRevealDto>.Fail("Declared attack not found.");
        if (attack.GameId != gameId) return Result<ArmyRevealDto>.Fail("Attack does not belong to this game.");

        bool isAttacker = attack.AttackerKingdomId == kingdom.Id;
        bool isDefender = attack.DefenderKingdomId == kingdom.Id;
        if (!isAttacker && !isDefender)
            return Result<ArmyRevealDto>.Fail("You are not involved in this battle.");

        // 2. Parse pre-selected army IDs
        var attackerArmyIds = ParseArmyIds(attack.AttackerSelectedArmyIds);
        var defenderArmyIds = ParseArmyIds(attack.DefenderSelectedArmyIds);

        // 3. Load armies with types for both sides
        var attackerArmies = await LoadRevealedArmies(attack.AttackerKingdomId, attackerArmyIds);
        var defenderArmies = await LoadRevealedArmies(attack.DefenderKingdomId, defenderArmyIds);

        return Result<ArmyRevealDto>.Ok(new ArmyRevealDto
        {
            DeclaredAttackId = attack.Id,
            AttackerKingdomId = attack.AttackerKingdomId,
            DefenderKingdomId = attack.DefenderKingdomId,
            AttackerArmies = attackerArmies,
            DefenderArmies = defenderArmies
        });
    }

    public async Task<Result<BattleSetupDto>> SetLineupAsync(
        Guid gameId, Guid userId, SetLineupRequest request)
    {
        // 1. Validate game/player/attack access
        var game = await unitOfWork.Games.GetByIdWithLockAsync(gameId);
        if (game is null) return Result<BattleSetupDto>.Fail("Game not found.");
        if (game.Status != EGameStatus.InProgress) return Result<BattleSetupDto>.Fail("Game is not in progress.");
        if (game.CurrentPhase != EGamePhase.Battle) return Result<BattleSetupDto>.Fail("Lineup ordering only available during Battle Phase.");

        var kingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, gameId);
        if (kingdom is null) return Result<BattleSetupDto>.Fail("You are not in this game.");

        var attack = await unitOfWork.DeclaredAttacks.GetByIdAsync(request.DeclaredAttackId);
        if (attack is null) return Result<BattleSetupDto>.Fail("Declared attack not found.");
        if (attack.GameId != gameId) return Result<BattleSetupDto>.Fail("Attack does not belong to this game.");

        bool isAttacker = attack.AttackerKingdomId == kingdom.Id;
        bool isDefender = attack.DefenderKingdomId == kingdom.Id;
        if (!isAttacker && !isDefender)
            return Result<BattleSetupDto>.Fail("You are not involved in this battle.");

        // 2. Validate the lineup contains exactly the same army IDs as the selection
        var selectedIds = ParseArmyIds(isAttacker
            ? attack.AttackerSelectedArmyIds
            : attack.DefenderSelectedArmyIds);

        if (selectedIds.Count == 0)
            return Result<BattleSetupDto>.Fail("You must select armies before setting lineup.");

        if (request.ArmyIdsInOrder.Count != selectedIds.Count)
            return Result<BattleSetupDto>.Fail($"Lineup must contain exactly {selectedIds.Count} armies.");

        var selectedSet = selectedIds.ToHashSet();
        var lineupSet = request.ArmyIdsInOrder.ToHashSet();
        if (!selectedSet.SetEquals(lineupSet))
            return Result<BattleSetupDto>.Fail("Lineup must contain the same armies as your selection.");

        // 3. Store lineup order (overwrites selection order)
        if (isAttacker)
            attack.AttackerSelectedArmyIds = string.Join(",", request.ArmyIdsInOrder);
        else
            attack.DefenderSelectedArmyIds = string.Join(",", request.ArmyIdsInOrder);

        attack.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.DeclaredAttacks.UpdateAsync(attack);
        await unitOfWork.CommitAsync();

        return Result<BattleSetupDto>.Ok(new BattleSetupDto
        {
            DeclaredAttackId = attack.Id,
            KingdomId = kingdom.Id,
            ArmiesSelected = request.ArmyIdsInOrder.Count,
            MaxArmies = selectedIds.Count
        });
    }

    private static List<Guid> ParseArmyIds(string? armyIdsString)
    {
        if (string.IsNullOrEmpty(armyIdsString)) return [];
        return armyIdsString.Split(',').Select(Guid.Parse).ToList();
    }

    private async Task<List<RevealedArmyDto>> LoadRevealedArmies(Guid kingdomId, List<Guid> selectedArmyIds)
    {
        var armies = (await unitOfWork.Armies.GetArmiesWithTypeForKingdomAsync(kingdomId)).ToList();

        var selected = selectedArmyIds.Count > 0
            ? armies.Where(a => selectedArmyIds.Contains(a.Id)).ToList()
            : armies;

        return selected.Select(a => new RevealedArmyDto
        {
            ArmyId = a.Id,
            ArmyTypeId = a.ArmyTypeId,
            ArmyTypeName = a.ArmyType!.Name.ToString(),
            CurrentHP = a.CurrentHP,
            MaxHP = a.MaxHP,
            Attack = a.ArmyType.Attack,
            Initiative = a.ArmyType.Initiative
        }).ToList();
    }

    public async Task<List<BattleResultDto>> ResolveBattlesAsync(Domain.Game.Game game)
    {
        var results = new List<BattleResultDto>();

        // 1. Load declared attacks for this round
        var declaredAttacks = await unitOfWork.DeclaredAttacks
            .GetForGameRoundAsync(game.Id, game.RoundNumber);

        if (declaredAttacks.Count == 0)
            return results;

        // 2. Track committed army IDs across all battles
        var committedArmyIds = new HashSet<Guid>();

        // 3. Resolve each declared attack
        foreach (var attack in declaredAttacks)
        {
            var battleResult = await ResolveSingleBattleAsync(game, attack, committedArmyIds);
            results.Add(battleResult);
        }

        // 4. Delete declared attacks for this round
        await unitOfWork.DeclaredAttacks.DeleteForGameRoundAsync(game.Id, game.RoundNumber);

        return results;
    }

    private async Task<BattleResultDto> ResolveSingleBattleAsync(
        Domain.Game.Game game, DeclaredAttack attack, HashSet<Guid> committedArmyIds)
    {
        // a. Load attacker armies with types
        var attackerArmies = (await unitOfWork.Armies
            .GetArmiesWithTypeForKingdomAsync(attack.AttackerKingdomId)).ToList();

        // b. Load defender armies with types
        var defenderArmies = (await unitOfWork.Armies
            .GetArmiesWithTypeForKingdomAsync(attack.DefenderKingdomId)).ToList();

        // c. Load attacker kingdom + faction
        var attackerKingdom = await unitOfWork.Kingdoms.GetByIdAsync(attack.AttackerKingdomId);
        var attackerFaction = await unitOfWork.FactionTypes.GetByIdAsync(attackerKingdom!.FactionTypeId);

        // d. Load defender kingdom + faction
        var defenderKingdom = await unitOfWork.Kingdoms.GetByIdAsync(attack.DefenderKingdomId);
        var defenderFaction = await unitOfWork.FactionTypes.GetByIdAsync(defenderKingdom!.FactionTypeId);

        // e. Determine max armies
        var maxArmies = CombatRules.GetMaxArmies(attack.TargetTile!.IsCastle, attack.RiskedTile!.IsCastle);

        // f. Use pre-selected armies if available, otherwise auto-select
        var attackerPreselected = ParseArmyIds(attack.AttackerSelectedArmyIds);
        var defenderPreselected = ParseArmyIds(attack.DefenderSelectedArmyIds);

        List<MilitaryArmy> selectedAttackers;
        if (attackerPreselected.Count > 0)
        {
            selectedAttackers = attackerPreselected
                .Select(id => attackerArmies.FirstOrDefault(a => a.Id == id))
                .Where(a => a is not null)
                .ToList()!;
        }
        else
        {
            var attackerAvailable = attackerArmies
                .Select(a => (a, a.ArmyType!))
                .ToList();
            selectedAttackers = CombatRules.AutoSelectArmies(attackerAvailable, maxArmies, committedArmyIds);
        }

        List<MilitaryArmy> selectedDefenders;
        if (defenderPreselected.Count > 0)
        {
            selectedDefenders = defenderPreselected
                .Select(id => defenderArmies.FirstOrDefault(a => a.Id == id))
                .Where(a => a is not null)
                .ToList()!;
        }
        else
        {
            var defenderAvailable = defenderArmies
                .Select(a => (a, a.ArmyType!))
                .ToList();
            selectedDefenders = CombatRules.AutoSelectArmies(defenderAvailable, maxArmies, committedArmyIds);
        }

        // g. Add selected army IDs to committedArmyIds
        foreach (var army in selectedAttackers)
            committedArmyIds.Add(army.Id);
        foreach (var army in selectedDefenders)
            committedArmyIds.Add(army.Id);

        // h. Compute combat stats with faction modifiers and situational bonuses
        var attackerLineup = BuildLineup(selectedAttackers, attackerFaction!, isAttacker: true);
        var defenderLineup = BuildLineup(selectedDefenders, defenderFaction!, isAttacker: false);

        // i. Resolve battle
        var result = CombatRules.ResolveBattle(attackerLineup, defenderLineup, true, new Random());

        // j. Apply results: update surviving army HP, delete destroyed armies
        foreach (var (armyId, hp) in result.SurvivingArmyHP)
        {
            var army = attackerArmies.FirstOrDefault(a => a.Id == armyId)
                       ?? defenderArmies.FirstOrDefault(a => a.Id == armyId);
            if (army is not null)
            {
                army.CurrentHP = hp;
                await unitOfWork.Armies.UpdateAsync(army);
            }
        }

        foreach (var destroyedId in result.DestroyedArmyIds)
        {
            await unitOfWork.Armies.DeleteAsync(destroyedId);
        }

        // k. Get tile capture outcome
        var (tileCapturedId, tileCapturedFromKingdomId) = CombatRules.GetBattleOutcome(
            result.Outcome,
            attack.AttackerKingdomId,
            attack.DefenderKingdomId,
            attack.RiskedTileId,
            attack.TargetTileId);

        // l. Transfer tile ownership
        var capturedTile = await unitOfWork.Tiles.GetByIdAsync(tileCapturedId);
        var winnerKingdomId = result.Outcome == EBattleOutcome.AttackerWon
            ? attack.AttackerKingdomId
            : attack.DefenderKingdomId;
        capturedTile!.KingdomId = winnerKingdomId;
        await unitOfWork.Tiles.UpdateAsync(capturedTile);

        // m. Destroy building on captured tile
        var buildings = await unitOfWork.Buildings.GetBuildingsForKingdomAsync(tileCapturedFromKingdomId);
        var buildingsOnCapturedTile = buildings.Where(b => b.TileId == tileCapturedId).ToList();
        foreach (var building in buildingsOnCapturedTile)
        {
            await unitOfWork.Armies.DeleteArmiesForBuildingAsync(building.Id);
            await unitOfWork.Buildings.DeleteAsync(building.Id);
        }

        // n. Check castle capture = elimination
        if (capturedTile.IsCastle)
        {
            var loserKingdom = await unitOfWork.Kingdoms.GetByIdAsync(tileCapturedFromKingdomId);
            loserKingdom!.Status = EKingdomStatus.Defeated;
            loserKingdom.DefeatedAt = DateTime.UtcNow;
            await unitOfWork.Kingdoms.UpdateAsync(loserKingdom);

            // Delete all loser armies
            var loserArmies = await unitOfWork.Armies.GetArmiesForKingdomAsync(tileCapturedFromKingdomId);
            foreach (var army in loserArmies)
                await unitOfWork.Armies.DeleteAsync(army.Id);

            // Destroy all loser buildings
            var loserBuildings = await unitOfWork.Buildings.GetBuildingsForKingdomAsync(tileCapturedFromKingdomId);
            foreach (var building in loserBuildings)
                await unitOfWork.Buildings.DeleteAsync(building.Id);

            // Set all loser tiles to unowned
            var loserTiles = await unitOfWork.Tiles.GetTilesForKingdomAsync(tileCapturedFromKingdomId);
            foreach (var tile in loserTiles)
            {
                tile.KingdomId = null;
                await unitOfWork.Tiles.UpdateAsync(tile);
            }

            // Log elimination
            await AddTurnLogAsync(game, winnerKingdomId, EEventType.KingdomEliminated,
                $"Kingdom {loserKingdom.Name} has been eliminated");
        }

        // o. Create Battle entity
        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            RoundNumber = game.RoundNumber,
            AttackerKingdomId = attack.AttackerKingdomId,
            DefenderKingdomId = attack.DefenderKingdomId,
            AttackerTileId = attack.RiskedTileId,
            DefenderTileId = attack.TargetTileId,
            Outcome = result.Outcome,
            TileCapturedId = tileCapturedId,
            TileCapturedFromKingdomId = tileCapturedFromKingdomId,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.Battles.AddAsync(battle);

        // p. Create BattleRound entities
        for (var i = 0; i < result.Rounds.Count; i++)
        {
            var round = result.Rounds[i];
            var attackerArmy = attackerLineup.Count > i ? attackerLineup[i].Army : attackerLineup[^1].Army;
            var defenderArmy = defenderLineup.Count > i ? defenderLineup[i].Army : defenderLineup[^1].Army;

            // Determine which armies were actually fighting in this round
            var atkArmyId = FindFightingArmyId(attackerLineup, result.Rounds, i, isAttacker: true);
            var defArmyId = FindFightingArmyId(defenderLineup, result.Rounds, i, isAttacker: false);

            var battleRound = new BattleRound
            {
                Id = Guid.NewGuid(),
                BattleId = battle.Id,
                RoundNumber = i + 1,
                AttackerArmyId = atkArmyId,
                DefenderArmyId = defArmyId,
                InitiativeWinner = round.InitiativeWinner,
                AttackerInitiativeChance = round.AttackerInitiativeChance,
                DefenderInitiativeChance = round.DefenderInitiativeChance,
                DamageDealt = round.DamageDealt,
                ChipDamageDealt = round.ChipDamageDealt,
                AttackerArmyHPAfter = round.AttackerHPAfter,
                DefenderArmyHPAfter = round.DefenderHPAfter,
                ArmyDestroyedId = round.ArmyDestroyedId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await unitOfWork.BattleRounds.AddAsync(battleRound);
        }

        // q. Add TurnLogs
        await AddTurnLogAsync(game, attack.AttackerKingdomId, EEventType.BattleStarted,
            $"Battle started: attacking ({attack.TargetTile!.CoordQ},{attack.TargetTile.CoordR})");
        await AddTurnLogAsync(game, attack.AttackerKingdomId, EEventType.BattleEnded,
            $"Battle ended: {result.Outcome}");
        await AddTurnLogAsync(game, winnerKingdomId, EEventType.TileCaptured,
            $"Tile ({capturedTile.CoordQ},{capturedTile.CoordR}) captured");

        return new BattleResultDto
        {
            BattleId = battle.Id,
            AttackerKingdomId = attack.AttackerKingdomId,
            DefenderKingdomId = attack.DefenderKingdomId,
            Outcome = result.Outcome.ToString(),
            TileCapturedId = tileCapturedId,
            TileCapturedFromKingdomId = tileCapturedFromKingdomId,
            Rounds = result.Rounds.Select((r, idx) => new BattleRoundResultDto
            {
                RoundNumber = idx + 1,
                AttackerArmyId = FindFightingArmyId(attackerLineup, result.Rounds, idx, isAttacker: true),
                DefenderArmyId = FindFightingArmyId(defenderLineup, result.Rounds, idx, isAttacker: false),
                InitiativeWinner = r.InitiativeWinner.ToString(),
                AttackerInitiativeChance = r.AttackerInitiativeChance,
                DefenderInitiativeChance = r.DefenderInitiativeChance,
                DamageDealt = r.DamageDealt,
                ChipDamageDealt = r.ChipDamageDealt,
                AttackerArmyHPAfter = r.AttackerHPAfter,
                DefenderArmyHPAfter = r.DefenderHPAfter,
                ArmyDestroyedId = r.ArmyDestroyedId
            }).ToList()
        };
    }

    private static List<(MilitaryArmy Army, ArmyType ArmyType, ArmyRules.ArmyCombatStats Stats)> BuildLineup(
        List<MilitaryArmy> selectedArmies,
        Domain.Factions.FactionType faction,
        bool isAttacker)
    {
        return selectedArmies.Select(army =>
        {
            var armyType = army.ArmyType!;

            // Apply faction modifiers
            var stats = ArmyRules.ApplyFactionModifiers(
                armyType.Attack, armyType.Initiative,
                armyType.ChipDamageRangeMin, armyType.ChipDamageRangeMax,
                faction.AttackModifier, faction.InitiativeModifier, faction.ChipDamageModifier);

            // Apply situational bonus
            var bonus = ArmyRules.ApplySituationalBonus(armyType, isAttacker);
            if (bonus is not null)
            {
                var (stat, multiplier) = bonus.Value;
                stats = stat switch
                {
                    "Attack" => stats with { Attack = (int)(stats.Attack * multiplier) },
                    "Initiative" => stats with { Initiative = (int)(stats.Initiative * multiplier) },
                    _ => stats
                };
            }

            return (army, armyType, stats);
        }).ToList();
    }

    /// <summary>
    /// Finds the army ID that was fighting in a given round by tracking eliminations.
    /// The lineup advances when an army is destroyed.
    /// </summary>
    private static Guid FindFightingArmyId(
        List<(MilitaryArmy Army, ArmyType ArmyType, ArmyRules.ArmyCombatStats Stats)> lineup,
        List<CombatRules.CombatRoundResult> rounds,
        int currentRoundIndex,
        bool isAttacker)
    {
        if (lineup.Count == 0)
            return Guid.Empty;

        int idx = 0;
        for (int i = 0; i < currentRoundIndex; i++)
        {
            var round = rounds[i];
            if (round.ArmyDestroyedId is not null)
            {
                var currentArmyId = idx < lineup.Count ? lineup[idx].Army.Id : lineup[^1].Army.Id;
                if (round.ArmyDestroyedId == currentArmyId)
                {
                    idx++;
                }
                // Also check double-kill where both advance
                else if (isAttacker && round.AttackerHPAfter <= 0)
                {
                    idx++;
                }
                else if (!isAttacker && round.DefenderHPAfter <= 0)
                {
                    idx++;
                }
            }
        }

        return idx < lineup.Count ? lineup[idx].Army.Id : lineup[^1].Army.Id;
    }

    private async Task AddTurnLogAsync(Domain.Game.Game game, Guid kingdomId, EEventType eventType, string description)
    {
        var turnLog = new TurnLog
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            KingdomId = kingdomId,
            RoundNumber = game.RoundNumber,
            EventType = eventType,
            Description = description,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await unitOfWork.TurnLogs.AddAsync(turnLog);
    }
}
