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
    }

    private static void DiceExpressionsParseAndRoll()
    {
        var dice = DiceExpression.Parse("2d6");
        Assert(dice is { Count: 2, Sides: 6 }, "'2d6' must parse to 2 six-sided dice.");
        Assert(dice.Roll(new ScriptedRandom(3, 4)) == 7, "2d6 of 3 and 4 must total 7.");
        Assert(dice.Roll(new ScriptedRandom(1, 1, 1, 1), doubleDice: true) == 4, "A critical doubles the dice count (2d6 -> 4d6).");
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

    private static bool ThrowsFormat(Action action)
    {
        try { action(); }
        catch (FormatException) { return true; }
        return false;
    }
}
