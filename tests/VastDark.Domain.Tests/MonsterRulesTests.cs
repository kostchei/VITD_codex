using VastDark.Domain;
using static TestKit;

internal static class MonsterRulesTests
{
    public static void Run()
    {
        HitPointsClampAndReportDeath();
        ArmorDescriptorsMapToShadowdarkAc();
        MonstersBuildFromSourceStatBlocks();
    }

    private static void HitPointsClampAndReportDeath()
    {
        var hp = new HitPoints(10, 10);
        Assert(hp.Damage(4).Current == 6, "Damage must subtract from current HP.");
        Assert(hp.Damage(14) is { Current: 0, IsDead: true }, "Damage at or beyond current HP means dead (0).");
        Assert(new HitPoints(10, 3).Heal(99).Current == 10, "Healing must cap at maximum HP.");
    }

    private static void ArmorDescriptorsMapToShadowdarkAc()
    {
        Assert(MonsterArmor.ArmorClass("Hide") == 11, "Hide maps to AC 11.");
        Assert(MonsterArmor.ArmorClass("Leather") == 11, "Leather maps to AC 11.");
        Assert(MonsterArmor.ArmorClass("Scale") == 13, "Scale maps to AC 13.");
        Assert(MonsterArmor.ArmorClass("Chain-shirt") == 13, "Chain-shirt maps to AC 13.");
        Assert(MonsterArmor.ArmorClass("Plate") == 15, "Plate maps to AC 15.");
        Assert(MonsterArmor.ArmorClass("As Hide") == 11, "The 'As <armor>' source prefix must be accepted.");
        Assert(ThrowsArgumentOutOfRange(() => MonsterArmor.ArmorClass("Adamant")), "Unknown armor descriptors must be rejected, not defaulted.");
    }

    private static void MonstersBuildFromSourceStatBlocks()
    {
        var wyrm = Monster.FromCrawl(CrawlCreature.Wyrm);
        Assert(wyrm is { Name: "Wyrm", ArmorClass: 13 } && wyrm.HitPoints.Maximum == 150, "Wyrm must build as 150 HP / Scale AC 13.");
        var cyclops = Monster.FromCrawl(CrawlCreature.Cyclops);
        Assert(cyclops.HitPoints.Maximum == 10 && cyclops.ArmorClass == 11, "Cyclops must build as 10 HP / Hide AC 11.");

        cyclops.Damage(10);
        Assert(cyclops.IsDead, "A monster reduced to 0 HP is dead.");

        var delvers = Monster.FromStatBlock(RuinEncounterRules.GetStatBlock("Delvers"));
        Assert(delvers is { ArmorClass: 13 } && delvers.HitPoints.Maximum == 20, "Ruin Delvers must build as 20 HP / Scale AC 13.");
    }

    private static bool ThrowsArgumentOutOfRange(Action action)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException) { return true; }
        return false;
    }
}
