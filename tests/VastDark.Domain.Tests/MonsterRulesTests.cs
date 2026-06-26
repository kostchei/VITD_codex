using VastDark.Domain;
using static TestKit;

internal static class MonsterRulesTests
{
    public static void Run()
    {
        HitPointsClampAndReportDeath();
        ArmorDescriptorsMapToShadowdarkAc();
        MonstersBuildFromSourceStatBlocks();
        DemagogueStatBlockMatchesPage13();
        OgreSpawnStatBlockMatchesPage41();
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

    private static void DemagogueStatBlockMatchesPage13()
    {
        Assert(DemagogueRules.StatBlock is { HitDice: 5, HitPoints: 30, Defense: "Plate", Attack: "Lodestone Blade", DamageDice: "1d10" }, "The Demagogue stat block must match page 13.");
        Assert(DemagogueRules.VoiceOfTheDarkSave == "Charm", "Voice of the Dark must force a Charm save.");
        Assert(DemagogueRules.KnownSpellCount(new ScriptedRandom(2)) == 2, "The Demagogue must know 1d3 random spells.");
        Assert(DemagogueRules.RollArtifact(new ScriptedRandom(10)).Name == "Commune", "The Demagogue's artifact is a Great and Terrible treasure.");

        var demagogue = DemagogueRules.CreateMonster();
        Assert(demagogue is { Name: "Demagogue", ArmorClass: 15 } && demagogue.HitPoints.Maximum == 30, "The Demagogue must build as 30 HP / Plate AC 15.");
        // It can be cut down with the standard attack resolver; slaying it ends the Warband.
        var strike = AttackResolver.Strike(demagogue, attackModifier: 5, "10d6", new ScriptedRandom(15, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6));
        Assert(strike.Hit && demagogue.IsDead, "A 10d6 strike (60) must drop the 30-HP Demagogue.");
    }

    private static void OgreSpawnStatBlockMatchesPage41()
    {
        Assert(CrawlCreatureRules.OgreSpawnStatBlock is { HitDice: 1, HitPoints: 5, Attack: "1d6" }, "Ogre Spawn must be 1 HD / 5 HP / 1d6.");
        var spawn = CrawlCreatureRules.CreateOgreSpawn(3);
        Assert(spawn.Count == 3, "An Ogre at 10 HP splits into the rolled number of spawn.");
        Assert(spawn[0] is { Name: "Ogre Spawn", ArmorClass: 10 } && spawn[0].HitPoints.Maximum == 5, "Each Ogre Spawn must build as 5 HP / unarmored AC 10.");
    }

    private static bool ThrowsArgumentOutOfRange(Action action)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException) { return true; }
        return false;
    }
}
