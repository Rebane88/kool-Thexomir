using Domain.Map;
using Domain.Military;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class CombatRulesTests
{
    // --- Helpers ---

    private static Tile CreateTile(int q, int r, Guid? kingdomId, bool isCastle = false) => new()
    {
        Id = Guid.NewGuid(),
        CoordQ = q,
        CoordR = r,
        KingdomId = kingdomId,
        IsCastle = isCastle,
    };

    private static ArmyType CreateArmyType(int attack = 50, int hp = 100, int initiative = 10) => new()
    {
        Id = Guid.NewGuid(),
        Attack = attack,
        HP = hp,
        Initiative = initiative,
        DamageRangeMin = 0.8m,
        DamageRangeMax = 1.2m,
        ChipDamageRangeMin = 0.05m,
        ChipDamageRangeMax = 0.10m,
    };

    private static Army CreateArmy(Guid? id = null, int currentHP = 100, int maxHP = 100, Guid? armyTypeId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        ArmyTypeId = armyTypeId ?? Guid.NewGuid(),
        KingdomId = Guid.NewGuid(),
        BuildingId = Guid.NewGuid(),
        CurrentHP = currentHP,
        MaxHP = maxHP,
    };

    private static readonly Guid AttackerKingdomId = Guid.Parse("AAAAAAAA-0001-0000-0000-000000000001");
    private static readonly Guid DefenderKingdomId = Guid.Parse("DDDDDDDD-0001-0000-0000-000000000001");

    #region ValidateAttackDeclaration

    [Fact]
    public void ValidateAttackDeclaration_ValidScenario_ReturnsNull()
    {
        // Target at (1,0) owned by defender, borders attacker territory at (0,0)
        // Risked at (0,0) owned by attacker, adjacent to target
        var target = CreateTile(1, 0, DefenderKingdomId);
        var risked = CreateTile(0, 0, AttackerKingdomId);
        var attackerCoords = new List<(int q, int r)> { (0, 0), (0, -1) };
        var lockedTiles = new HashSet<Guid>();

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 1, lockedTiles);

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateAttackDeclaration_TargetUnowned_ReturnsError()
    {
        var target = CreateTile(1, 0, null); // unowned
        var risked = CreateTile(0, 0, AttackerKingdomId);
        var attackerCoords = new List<(int q, int r)> { (0, 0) };

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 1, []);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateAttackDeclaration_TargetOwnedByAttacker_ReturnsError()
    {
        var target = CreateTile(1, 0, AttackerKingdomId); // same kingdom
        var risked = CreateTile(0, 0, AttackerKingdomId);
        var attackerCoords = new List<(int q, int r)> { (0, 0) };

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 1, []);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateAttackDeclaration_TargetNotBorderingAttacker_ReturnsError()
    {
        // Target at (3,0), attacker only at (0,0) - not adjacent
        var target = CreateTile(3, 0, DefenderKingdomId);
        var risked = CreateTile(0, 0, AttackerKingdomId);
        var attackerCoords = new List<(int q, int r)> { (0, 0) };

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 1, []);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateAttackDeclaration_RiskedNotOwnedByAttacker_ReturnsError()
    {
        var target = CreateTile(1, 0, DefenderKingdomId);
        var risked = CreateTile(0, 0, DefenderKingdomId); // wrong owner
        var attackerCoords = new List<(int q, int r)> { (0, 0) };

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 1, []);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateAttackDeclaration_RiskedNotAdjacentToTarget_ReturnsError()
    {
        // Target at (1,0), risked at (3,3) - not adjacent
        var target = CreateTile(1, 0, DefenderKingdomId);
        var risked = CreateTile(3, 3, AttackerKingdomId);
        var attackerCoords = new List<(int q, int r)> { (3, 3), (0, 0) };

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 1, []);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateAttackDeclaration_ZeroArmies_ReturnsError()
    {
        var target = CreateTile(1, 0, DefenderKingdomId);
        var risked = CreateTile(0, 0, AttackerKingdomId);
        var attackerCoords = new List<(int q, int r)> { (0, 0) };

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 0, []);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateAttackDeclaration_TargetTileLocked_ReturnsError()
    {
        var target = CreateTile(1, 0, DefenderKingdomId);
        var risked = CreateTile(0, 0, AttackerKingdomId);
        var attackerCoords = new List<(int q, int r)> { (0, 0) };
        var lockedTiles = new HashSet<Guid> { target.Id };

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 1, lockedTiles);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateAttackDeclaration_RiskedTileLocked_ReturnsError()
    {
        var target = CreateTile(1, 0, DefenderKingdomId);
        var risked = CreateTile(0, 0, AttackerKingdomId);
        var attackerCoords = new List<(int q, int r)> { (0, 0) };
        var lockedTiles = new HashSet<Guid> { risked.Id };

        var result = CombatRules.ValidateAttackDeclaration(
            target, risked, AttackerKingdomId, attackerCoords, 1, lockedTiles);

        result.ShouldNotBeNull();
    }

    #endregion

    #region GetMaxArmies

    [Fact]
    public void GetMaxArmies_NeitherCastle_Returns3()
    {
        CombatRules.GetMaxArmies(false, false).ShouldBe(3);
    }

    [Fact]
    public void GetMaxArmies_TargetIsCastle_Returns5()
    {
        CombatRules.GetMaxArmies(true, false).ShouldBe(5);
    }

    [Fact]
    public void GetMaxArmies_RiskedIsCastle_Returns5()
    {
        CombatRules.GetMaxArmies(false, true).ShouldBe(5);
    }

    #endregion

    #region ValidateArmySelection

    [Fact]
    public void ValidateArmySelection_ValidSelection_ReturnsNull()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var result = CombatRules.ValidateArmySelection(ids, 3, []);
        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateArmySelection_ExceedsMax_ReturnsError()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var result = CombatRules.ValidateArmySelection(ids, 3, []);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateArmySelection_ArmyAlreadyCommitted_ReturnsError()
    {
        var committedId = Guid.NewGuid();
        var ids = new List<Guid> { committedId, Guid.NewGuid() };
        var committed = new HashSet<Guid> { committedId };
        var result = CombatRules.ValidateArmySelection(ids, 3, committed);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ValidateArmySelection_EmptyList_ReturnsError()
    {
        var result = CombatRules.ValidateArmySelection([], 3, []);
        result.ShouldNotBeNull();
    }

    #endregion

    #region AutoSelectArmies

    [Fact]
    public void AutoSelectArmies_SelectsStrongestByAttack()
    {
        var weakType = CreateArmyType(attack: 30);
        var strongType = CreateArmyType(attack: 80);
        var midType = CreateArmyType(attack: 50);

        var weak = CreateArmy(armyTypeId: weakType.Id);
        var strong = CreateArmy(armyTypeId: strongType.Id);
        var mid = CreateArmy(armyTypeId: midType.Id);

        var available = new List<(Army Army, ArmyType ArmyType)>
        {
            (weak, weakType), (strong, strongType), (mid, midType),
        };

        var result = CombatRules.AutoSelectArmies(available, 2, []);
        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(strong.Id);
        result[1].Id.ShouldBe(mid.Id);
    }

    [Fact]
    public void AutoSelectArmies_RespectsMaxLimit()
    {
        var type1 = CreateArmyType(attack: 80);
        var type2 = CreateArmyType(attack: 70);
        var type3 = CreateArmyType(attack: 60);
        var a1 = CreateArmy(armyTypeId: type1.Id);
        var a2 = CreateArmy(armyTypeId: type2.Id);
        var a3 = CreateArmy(armyTypeId: type3.Id);

        var available = new List<(Army Army, ArmyType ArmyType)>
        {
            (a1, type1), (a2, type2), (a3, type3),
        };

        var result = CombatRules.AutoSelectArmies(available, 2, []);
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void AutoSelectArmies_ExcludesCommittedArmies()
    {
        var type1 = CreateArmyType(attack: 80);
        var type2 = CreateArmyType(attack: 50);
        var a1 = CreateArmy(armyTypeId: type1.Id);
        var a2 = CreateArmy(armyTypeId: type2.Id);

        var committed = new HashSet<Guid> { a1.Id };
        var available = new List<(Army Army, ArmyType ArmyType)>
        {
            (a1, type1), (a2, type2),
        };

        var result = CombatRules.AutoSelectArmies(available, 3, committed);
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(a2.Id);
    }

    [Fact]
    public void AutoSelectArmies_AllCommitted_ReturnsEmpty()
    {
        var type1 = CreateArmyType(attack: 80);
        var a1 = CreateArmy(armyTypeId: type1.Id);
        var committed = new HashSet<Guid> { a1.Id };

        var available = new List<(Army Army, ArmyType ArmyType)> { (a1, type1) };
        var result = CombatRules.AutoSelectArmies(available, 3, committed);
        result.ShouldBeEmpty();
    }

    #endregion

    #region ResolveRound

    [Fact]
    public void ResolveRound_DefenderWinsInitiative_WhenRollAboveChance()
    {
        // Random(42): call 0 = 0.6681 -> >= 0.5 -> defender wins
        var rng = new Random(42);
        var result = CombatRules.ResolveRound(
            attackerEffAtk: 50, attackerInit: 10,
            atkDmgMin: 0.8m, atkDmgMax: 1.2m, atkChipMin: 0.05m, atkChipMax: 0.10m,
            attackerHP: 100, attackerArmyId: Guid.NewGuid(),
            defenderEffAtk: 40, defenderInit: 10,
            defDmgMin: 0.8m, defDmgMax: 1.2m, defChipMin: 0.05m, defChipMax: 0.10m,
            defenderHP: 100, defenderArmyId: Guid.NewGuid(),
            random: rng);

        result.InitiativeWinner.ShouldBe(EBattleOutcome.DefenderWon);
        result.AttackerInitiativeChance.ShouldBe(0.5m);
        result.DefenderInitiativeChance.ShouldBe(0.5m);
    }

    [Fact]
    public void ResolveRound_AttackerWinsInitiative_WhenRollBelowChance()
    {
        // Use init 15 vs 5 => attacker chance = 0.75
        // Random(42): call 0 = 0.6681 < 0.75 -> attacker wins
        var rng = new Random(42);
        var result = CombatRules.ResolveRound(
            attackerEffAtk: 50, attackerInit: 15,
            atkDmgMin: 0.8m, atkDmgMax: 1.2m, atkChipMin: 0.05m, atkChipMax: 0.10m,
            attackerHP: 100, attackerArmyId: Guid.NewGuid(),
            defenderEffAtk: 40, defenderInit: 5,
            defDmgMin: 0.8m, defDmgMax: 1.2m, defChipMin: 0.05m, defChipMax: 0.10m,
            defenderHP: 100, defenderArmyId: Guid.NewGuid(),
            random: rng);

        result.InitiativeWinner.ShouldBe(EBattleOutcome.AttackerWon);
    }

    [Fact]
    public void ResolveRound_DamageAppliedToLoser_ChipDamageToWinner()
    {
        // Random(42): calls 0=0.6681(init), 1=0.1409(dmg), 2=0.1255(chip)
        // Init: 10/(10+10)=0.5, roll 0.6681 >= 0.5 -> Defender wins
        // Damage: defender effAtk=40 * (0.8 + 0.1409*0.4) = 40 * 0.85636 = (int)34.254 = 34
        // Chip: attacker effAtk=50 * (0.05 + 0.1255*0.05) = 50 * 0.056276 = (int)2.8138 = 2
        var rng = new Random(42);
        var result = CombatRules.ResolveRound(
            attackerEffAtk: 50, attackerInit: 10,
            atkDmgMin: 0.8m, atkDmgMax: 1.2m, atkChipMin: 0.05m, atkChipMax: 0.10m,
            attackerHP: 100, attackerArmyId: Guid.NewGuid(),
            defenderEffAtk: 40, defenderInit: 10,
            defDmgMin: 0.8m, defDmgMax: 1.2m, defChipMin: 0.05m, defChipMax: 0.10m,
            defenderHP: 100, defenderArmyId: Guid.NewGuid(),
            random: rng);

        result.DamageDealt.ShouldBe(34);
        result.ChipDamageDealt.ShouldBe(2);
        result.AttackerHPAfter.ShouldBe(66);  // 100 - 34 (attacker is loser)
        result.DefenderHPAfter.ShouldBe(98);  // 100 - 2  (defender is winner, takes chip)
    }

    [Fact]
    public void ResolveRound_ArmyDestroyed_WhenHPReachesZero()
    {
        // Attacker has 5 HP, defender wins initiative
        var rng = new Random(42);
        var attackerArmyId = Guid.NewGuid();
        var result = CombatRules.ResolveRound(
            attackerEffAtk: 50, attackerInit: 10,
            atkDmgMin: 0.8m, atkDmgMax: 1.2m, atkChipMin: 0.05m, atkChipMax: 0.10m,
            attackerHP: 5, attackerArmyId: attackerArmyId,
            defenderEffAtk: 40, defenderInit: 10,
            defDmgMin: 0.8m, defDmgMax: 1.2m, defChipMin: 0.05m, defChipMax: 0.10m,
            defenderHP: 100, defenderArmyId: Guid.NewGuid(),
            random: rng);

        result.AttackerHPAfter.ShouldBe(0);
        result.ArmyDestroyedId.ShouldBe(attackerArmyId);
    }

    [Fact]
    public void ResolveRound_DoubleKill_InitiativeLoserDestroyed()
    {
        // Both armies have very low HP so damage kills both
        // Defender wins initiative (Random(42), equal init)
        // Damage kills attacker (loser), chip kills defender (winner)
        // Double-kill: ArmyDestroyedId = initiative loser = attacker
        var rng = new Random(42);
        var attackerArmyId = Guid.NewGuid();
        var defenderArmyId = Guid.NewGuid();
        var result = CombatRules.ResolveRound(
            attackerEffAtk: 50, attackerInit: 10,
            atkDmgMin: 0.8m, atkDmgMax: 1.2m, atkChipMin: 0.05m, atkChipMax: 0.10m,
            attackerHP: 1, attackerArmyId: attackerArmyId,
            defenderEffAtk: 40, defenderInit: 10,
            defDmgMin: 0.8m, defDmgMax: 1.2m, defChipMin: 0.05m, defChipMax: 0.10m,
            defenderHP: 1, defenderArmyId: defenderArmyId,
            random: rng);

        result.AttackerHPAfter.ShouldBe(0);
        result.DefenderHPAfter.ShouldBe(0);
        // Double-kill: initiative loser (attacker) is destroyed
        result.ArmyDestroyedId.ShouldBe(attackerArmyId);
    }

    [Fact]
    public void ResolveRound_InitiativeChancesCalculatedCorrectly()
    {
        var rng = new Random(42);
        var result = CombatRules.ResolveRound(
            attackerEffAtk: 50, attackerInit: 15,
            atkDmgMin: 0.8m, atkDmgMax: 1.2m, atkChipMin: 0.05m, atkChipMax: 0.10m,
            attackerHP: 100, attackerArmyId: Guid.NewGuid(),
            defenderEffAtk: 40, defenderInit: 5,
            defDmgMin: 0.8m, defDmgMax: 1.2m, defChipMin: 0.05m, defChipMax: 0.10m,
            defenderHP: 100, defenderArmyId: Guid.NewGuid(),
            random: rng);

        result.AttackerInitiativeChance.ShouldBe(0.75m);
        result.DefenderInitiativeChance.ShouldBe(0.25m);
    }

    #endregion

    #region ResolveBattle

    private static (Army Army, ArmyType ArmyType, ArmyRules.ArmyCombatStats Stats) CreateLineupEntry(
        int attack = 50, int initiative = 10, int hp = 100,
        decimal chipMin = 0.05m, decimal chipMax = 0.10m,
        decimal dmgMin = 0.8m, decimal dmgMax = 1.2m)
    {
        var armyType = new ArmyType
        {
            Id = Guid.NewGuid(),
            Attack = attack,
            HP = hp,
            Initiative = initiative,
            DamageRangeMin = dmgMin,
            DamageRangeMax = dmgMax,
            ChipDamageRangeMin = chipMin,
            ChipDamageRangeMax = chipMax,
        };
        var army = new Army
        {
            Id = Guid.NewGuid(),
            ArmyTypeId = armyType.Id,
            CurrentHP = hp,
            MaxHP = hp,
        };
        var stats = new ArmyRules.ArmyCombatStats(attack, initiative, chipMin, chipMax);
        return (army, armyType, stats);
    }

    [Fact]
    public void ResolveBattle_SingleVsSingle_ProducesOutcome()
    {
        var rng = new Random(42);
        var atk = CreateLineupEntry(attack: 50, hp: 100);
        var def = CreateLineupEntry(attack: 40, hp: 100);

        var result = CombatRules.ResolveBattle(
            [atk], [def], attackerIsAttacker: true, random: rng);

        result.Outcome.ShouldBeOneOf(EBattleOutcome.AttackerWon, EBattleOutcome.DefenderWon);
        result.Rounds.Count.ShouldBeGreaterThan(0);
        result.DestroyedArmyIds.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ResolveBattle_LineupAdvancement_NextArmyFightsAfterDeath()
    {
        // Attacker has 2 armies, defender has 1 strong army
        var rng = new Random(42);
        var atk1 = CreateLineupEntry(attack: 30, hp: 50);
        var atk2 = CreateLineupEntry(attack: 30, hp: 50);
        var def1 = CreateLineupEntry(attack: 80, hp: 200);

        var result = CombatRules.ResolveBattle(
            [atk1, atk2], [def1], attackerIsAttacker: true, random: rng);

        // Battle should have multiple rounds showing lineup advancement
        result.Rounds.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void ResolveBattle_EndsWhenOneSideHasNoArmies()
    {
        var rng = new Random(42);
        var atk = CreateLineupEntry(attack: 50, hp: 100);
        var def = CreateLineupEntry(attack: 40, hp: 100);

        var result = CombatRules.ResolveBattle(
            [atk], [def], attackerIsAttacker: true, random: rng);

        // One side must win
        (result.Outcome == EBattleOutcome.AttackerWon || result.Outcome == EBattleOutcome.DefenderWon).ShouldBeTrue();
    }

    [Fact]
    public void ResolveBattle_EmptyAttackerLineup_DefenderWins()
    {
        var rng = new Random(42);
        var def = CreateLineupEntry(attack: 40, hp: 100);

        var result = CombatRules.ResolveBattle(
            [], [def], attackerIsAttacker: true, random: rng);

        result.Outcome.ShouldBe(EBattleOutcome.DefenderWon);
        result.Rounds.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveBattle_EmptyDefenderLineup_AttackerWins()
    {
        var rng = new Random(42);
        var atk = CreateLineupEntry(attack: 50, hp: 100);

        var result = CombatRules.ResolveBattle(
            [atk], [], attackerIsAttacker: true, random: rng);

        result.Outcome.ShouldBe(EBattleOutcome.AttackerWon);
        result.Rounds.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveBattle_SurvivingArmyHP_TrackedCorrectly()
    {
        var rng = new Random(42);
        var atk = CreateLineupEntry(attack: 50, hp: 100);
        var def = CreateLineupEntry(attack: 40, hp: 100);

        var result = CombatRules.ResolveBattle(
            [atk], [def], attackerIsAttacker: true, random: rng);

        // Winner should have surviving HP tracked
        result.SurvivingArmyHP.Count.ShouldBeGreaterThan(0);
    }

    #endregion

    #region GetBattleOutcome

    [Fact]
    public void GetBattleOutcome_AttackerWon_CapturesDefenderTile()
    {
        var attackerRiskedTileId = Guid.NewGuid();
        var defenderTargetTileId = Guid.NewGuid();

        var (tileCapturedId, tileCapturedFromKingdomId) = CombatRules.GetBattleOutcome(
            EBattleOutcome.AttackerWon,
            AttackerKingdomId, DefenderKingdomId,
            attackerRiskedTileId, defenderTargetTileId);

        tileCapturedId.ShouldBe(defenderTargetTileId);
        tileCapturedFromKingdomId.ShouldBe(DefenderKingdomId);
    }

    [Fact]
    public void GetBattleOutcome_DefenderWon_CapturesAttackerRiskedTile()
    {
        var attackerRiskedTileId = Guid.NewGuid();
        var defenderTargetTileId = Guid.NewGuid();

        var (tileCapturedId, tileCapturedFromKingdomId) = CombatRules.GetBattleOutcome(
            EBattleOutcome.DefenderWon,
            AttackerKingdomId, DefenderKingdomId,
            attackerRiskedTileId, defenderTargetTileId);

        tileCapturedId.ShouldBe(attackerRiskedTileId);
        tileCapturedFromKingdomId.ShouldBe(AttackerKingdomId);
    }

    #endregion
}
