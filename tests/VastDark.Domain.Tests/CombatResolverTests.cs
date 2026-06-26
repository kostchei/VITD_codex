using VastDark.Domain;
using static TestKit;

internal static class CombatResolverTests
{
    public static void Run()
    {
        DiceExpressionsParseAndRoll();
        AttacksHitWhenTheRollMeetsAc();
        CriticalHitsDoubleTheDamageDice();
        StrikingAMonsterReducesItsHp();
        StrikingATravelerSpendsGrit();
        MoraleBreaksAtHalfStrength();
        CutthroatWeaponsApplyTheirSpecialRules();
        WarbandFieldsADemagogueAndCutthroats();
    }

    private static void DiceExpressionsParseAndRoll()
    {
        var dice = DiceExpression.Parse("2d6");
        Assert(dice is { Count: 2, Sides: 6 }, "'2d6' must parse to 2 six-sided dice.");
        Assert(dice.Roll(new ScriptedRandom(3, 4)) == 7, "2d6 of 3 and 4 must total 7.");
        Assert(dice.Roll(new ScriptedRandom(1, 1, 1, 1), diceMultiplier: 2) == 4, "A critical doubles the dice count (2d6 -> 4d6).");
        Assert(ThrowsFormat(() => DiceExpression.Parse("sword")), "A non-dice expression must throw rather than default.");
    }

    private static void AttacksHitWhenTheRollMeetsAc()
    {
        // Attack +3 vs AC 13: a 10 totals 13 and hits; a 9 totals 12 and misses (no damage die rolled).
        var hit = AttackResolver.Resolve(3, 13, "1d8", new ScriptedRandom(10, 5));
        Assert(hit is { Hit: true, Critical: false, Damage: 5 }, "Rolling to exactly the AC must hit and roll damage.");
        var miss = AttackResolver.Resolve(3, 13, "1d8", new ScriptedRandom(9));
        Assert(miss is { Hit: false, Damage: 0 }, "Falling short of AC must miss and deal no damage.");
        var fumble = AttackResolver.Resolve(10, 5, "1d8", new ScriptedRandom(1));
        Assert(!fumble.Hit, "A natural 1 must miss regardless of modifier.");
    }

    private static void CriticalHitsDoubleTheDamageDice()
    {
        // Natural 20 -> auto-hit critical; 1d8 damage rolls 2d8.
        var crit = AttackResolver.Resolve(0, 18, "1d8", new ScriptedRandom(20, 4, 4));
        Assert(crit is { Hit: true, Critical: true, Damage: 8 }, "A natural 20 must hit and double the damage dice.");
    }

    private static void StrikingAMonsterReducesItsHp()
    {
        var cyclops = Monster.FromCrawl(CrawlCreature.Cyclops); // 10 HP, AC 11
        var result = AttackResolver.Strike(cyclops, 5, "1d6", new ScriptedRandom(6, 4));
        Assert(result.Hit && cyclops.HitPoints.Current == 6, "A hit must subtract rolled damage from the monster's HP.");
    }

    private static void StrikingATravelerSpendsGrit()
    {
        // Default DEX 10 -> AC 10. A monster attack of +0 rolling 10 hits; 1d4 damage spends Grit.
        var traveler = new Traveler("Vael", vitality: new Vitality(5, 4));
        var result = AttackResolver.Strike(traveler, 0, "1d4", new ScriptedRandom(10, 3));
        Assert(result.Hit && traveler.Vitality!.Grit == 2, "A hit on a Traveler must spend Grit before Flesh.");
    }

    private static void MoraleBreaksAtHalfStrength()
    {
        Assert(MoraleRules.GroupMustCheck(6, 3) && !MoraleRules.GroupMustCheck(6, 4), "A group must check morale only once reduced to half size or fewer.");

        var bloodied = Monster.FromCrawl(CrawlCreature.Cyclops); // 10 HP
        bloodied.Damage(5);
        Assert(MoraleRules.SoloMustCheck(bloodied), "A solo enemy at half HP must check morale.");

        Assert(MoraleRules.Check(3, new ScriptedRandom(12)) is { Holds: true }, "WIS +3 with a 12 (=15) holds against DC 15.");
        Assert(MoraleRules.Check(3, new ScriptedRandom(5)) is { Flees: true }, "Failing the DC 15 morale check means the enemy flees.");
    }

    private static void CutthroatWeaponsApplyTheirSpecialRules()
    {
        // Vicious dagger: 1d4 + ability bonus, and a natural-20 crit multiplies the dice x3.
        var dagger = AttackResolver.Resolve(0, 10, CutthroatRules.ViciousDagger, abilityDamageModifier: 2, new ScriptedRandom(12, 3));
        Assert(dagger.Damage == 5, "A dagger hit must add the wielder's ability bonus (1d4=3 +2).");
        var daggerCrit = AttackResolver.Resolve(0, 10, CutthroatRules.ViciousDagger, abilityDamageModifier: 2, new ScriptedRandom(20, 3, 3, 3));
        Assert(daggerCrit is { Critical: true, Damage: 11 }, "A vicious dagger crit rolls 3 dice (3+3+3) plus the bonus.");

        // Rock: non-proficient, so the ability bonus is ignored.
        var rock = AttackResolver.Resolve(0, 10, CutthroatRules.Rock, abilityDamageModifier: 3, new ScriptedRandom(12, 4));
        Assert(rock.Damage == 4, "A non-proficient rock adds no ability bonus (1d6=4 only).");

        // Makeshift spear: 1d6 - 1, and it shatters on a natural-1 fumble.
        var spear = AttackResolver.Resolve(0, 10, CutthroatRules.MakeshiftSpear, abilityDamageModifier: 0, new ScriptedRandom(12, 4));
        Assert(spear.Damage == 3, "A makeshift spear deals spear damage minus one (1d6=4 -1).");
        var fumble = AttackResolver.Resolve(5, 10, CutthroatRules.MakeshiftSpear, abilityDamageModifier: 0, new ScriptedRandom(1));
        Assert(fumble is { Hit: false, WeaponBroke: true }, "A natural 1 misses and shatters the makeshift spear.");
    }

    private static void WarbandFieldsADemagogueAndCutthroats()
    {
        // 5d6 Cutthroats (all 6s = 30) led by one Demagogue.
        var warband = WarbandRules.Create(new ScriptedRandom(6, 6, 6, 6, 6));
        Assert(warband.Cutthroats.Count == 30, "A Warband fields 5d6 Cutthroats.");
        Assert(warband.Demagogue is { Name: "Demagogue", ArmorClass: 15 } && warband.Demagogue.HitPoints.Maximum == 30, "A Warband is led by the Demagogue boss.");
        Assert(warband.Cutthroats[0] is { Name: "Cutthroat", ArmorClass: 13 } && warband.Cutthroats[0].HitPoints.Maximum == 18, "Cutthroats use the 18 HP / Scale AC 13 stat block.");
    }

    private static bool ThrowsFormat(Action action)
    {
        try { action(); }
        catch (FormatException) { return true; }
        return false;
    }
}
