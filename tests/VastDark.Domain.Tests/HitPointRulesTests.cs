using VastDark.Domain;
using static TestKit;

internal static class HitPointRulesTests
{
    public static void Run()
    {
        RollsHitDicePlusConPerLevel();
        RollNeverGoesBelowOnePerLevel();
        DamageClampsAndHealCaps();
        DyingTimerUsesConModifier();
        DyingTicksToRevivalOrDeath();
        StabilizeUsesAHardIntelligenceCheck();
        TravelerOnHpTrackEntersDyingAtZero();
        FullRestRestoresHpTraveler();
    }

    private static void RollsHitDicePlusConPerLevel()
    {
        // Level 3, d8, CON +1: each level rolls 5 -> max(1, 5+1) = 6, total 18.
        var hp = HitPointRules.Roll(3, 8, 1, new ScriptedRandom(5, 5, 5));
        Assert(hp is { Maximum: 18, Current: 18 }, "HP must total a hit die plus CON per level.");
    }

    private static void RollNeverGoesBelowOnePerLevel()
    {
        // A brutal CON penalty still yields at least 1 HP per level.
        var hp = HitPointRules.Roll(2, 4, -4, new ScriptedRandom(1, 2));
        Assert(hp.Maximum == 2, "Each level must contribute at least 1 HP regardless of CON penalty.");
    }

    private static void DamageClampsAndHealCaps()
    {
        var hp = new HitPoints(10, 10);
        Assert(HitPointRules.ApplyDamage(hp, 14).Current == 0, "Damage must clamp current HP at 0.");
        Assert(HitPointRules.Heal(new HitPoints(10, 4), 99).Current == 10, "Healing must cap at maximum HP.");
    }

    private static void DyingTimerUsesConModifier()
    {
        var down = new HitPoints(10, 0);
        var dying = HitPointRules.EnterDying(down, 2, new ScriptedRandom(3)); // 1d4 = 3, +CON 2 = 5
        Assert(dying is { IsDying: true, DeathTimer: 5 }, "The death timer must be 1d4 + CON modifier.");
        Assert(ThrowsInvalidOperation(() => HitPointRules.EnterDying(new HitPoints(10, 4), 0, new ScriptedRandom(1))), "A combatant above 0 HP cannot enter dying.");
    }

    private static void DyingTicksToRevivalOrDeath()
    {
        var dying = new HitPoints(10, 0, IsDying: true, DeathTimer: 2);
        var revived = HitPointRules.TickDying(dying, new ScriptedRandom(20));
        Assert(revived is { Outcome: DyingOutcome.Revived, HitPoints.Current: 1 }, "A natural 20 must revive the character at 1 HP.");

        var stillDying = HitPointRules.TickDying(dying, new ScriptedRandom(10));
        Assert(stillDying is { Outcome: DyingOutcome.Dying, HitPoints.DeathTimer: 1 }, "A non-20 must count the death timer down.");

        var dead = HitPointRules.TickDying(dying with { DeathTimer = 1 }, new ScriptedRandom(10));
        Assert(dead.Outcome == DyingOutcome.Dead, "The death timer reaching 0 must kill the character.");
    }

    private static void StabilizeUsesAHardIntelligenceCheck()
    {
        var dying = new HitPoints(10, 0, IsDying: true, DeathTimer: 3);
        Assert(HitPointRules.Stabilize(dying, 3, new ScriptedRandom(12)).Stabilized, "INT +3 with a 12 (=15) must clear DC 15 to stabilize.");
        var failed = HitPointRules.Stabilize(dying, 3, new ScriptedRandom(5));
        Assert(!failed.Stabilized && failed.HitPoints.IsDying, "A failed stabilize must leave the target dying at 0 HP.");
    }

    private static void TravelerOnHpTrackEntersDyingAtZero()
    {
        // Default ability scores give CON +0, so the death timer equals the 1d4 roll.
        var traveler = new Traveler("Plain", health: 5);
        var resolution = traveler.TakeDamage(5, new ScriptedRandom(3));
        Assert(resolution is null, "A Traveler without vitality resolves damage on the HP track.");
        Assert(traveler is { Health: 0, IsDying: true, DeathTimer: 3 }, "Reaching 0 HP must start the death timer.");
    }

    private static void FullRestRestoresHpTraveler()
    {
        var traveler = new Traveler("Plain", health: 8);
        traveler.DealDamage(5);
        traveler.RecoverGritAfterRest(fullDayOfRest: true, new ScriptedRandom());
        Assert(traveler.Health == 8, "A full rest must restore an HP-track Traveler to maximum.");
    }
}
