using VastDark.Domain;
using static TestKit;

internal static class EncounterResolverTests
{
    public static void Run()
    {
        WastesNothingProducesNoDecisions();
        WastesMonsterStartsCombatWithStatBlock();
        WastesMoodEncounterRecordsMood();
        WastesTraderOffersTrade();
        PillarMoodEncounterRecordsMood();
        RuinMonsterAndMoodResolve();
        DirectCrawlEncounterStartsCombat();
        RoamingHazardProducesSavesCombatAndDamage();
    }

    private static TravelParty SoloParty(string name = "Vael") => new([new Traveler(name)]);

    private static bool ThrowsArgumentOutOfRange(Action action)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException) { return true; }
        return false;
    }

    private static void WastesNothingProducesNoDecisions()
    {
        var resolution = EncounterResolver.ResolveWastes(1, SoloParty(), new ScriptedRandom());
        Assert(resolution.Source == EncounterSource.Wastes, "Wastes resolutions must be tagged as Wastes.");
        Assert(resolution.PendingDecisions.Count == 0, "A 'Nothing' encounter must leave nothing to resolve.");
        Assert(resolution.AppliedDamage.Count == 0, "A 'Nothing' encounter must apply no damage.");
    }

    private static void WastesMonsterStartsCombatWithStatBlock()
    {
        // Roll 14 = Cyclops, quantity 1d6 -> a single scripted die for the count.
        var resolution = EncounterResolver.ResolveWastes(14, SoloParty(), new ScriptedRandom(3));
        Assert(
            resolution.PendingDecisions.Single() is CombatDecision { EnemyGroup: "Cyclops", CombatantCount: 3, StatBlockName: "Cyclops", Disposition: "Hostile" },
            "A Cyclops encounter must start hostile combat with three Cyclops and a Crawl stat block.");
        Assert(resolution.Summary.Contains("Call in Dark", StringComparison.Ordinal), "The Cyclops special must surface in the summary.");
    }

    private static void WastesMoodEncounterRecordsMood()
    {
        // Roll 9 = Bandits (requires mood). Count die 1d6 first, then the d6 mood (3 = Tribute).
        var resolution = EncounterResolver.ResolveWastes(9, SoloParty(), new ScriptedRandom(4, 3));
        Assert(
            resolution.PendingDecisions.Single() is MoodDecision { EncounterName: "Bandits", MoodName: "Tribute" } mood &&
            mood.Consequence.Contains("100 coins", StringComparison.Ordinal),
            "Bandits rolling a 3 must record a Tribute mood demanding 100 coins.");
    }

    private static void WastesTraderOffersTrade()
    {
        // Roll 8 = Merchants, quantity 1d3, description carries a 100-coin trade limit.
        var resolution = EncounterResolver.ResolveWastes(8, SoloParty(), new ScriptedRandom(2));
        Assert(
            resolution.PendingDecisions.Single() is TradeDecision { Partner: "Merchants", CoinLimit: 100 },
            "Merchants must offer trade up to their printed 100-coin limit.");
    }

    private static void PillarMoodEncounterRecordsMood()
    {
        // Total 7 = Pillar Bandits (requires mood). Count 1d6, then mood 3 = Tribute.
        var resolution = EncounterResolver.ResolvePillar(7, SoloParty(), new ScriptedRandom(5, 3));
        Assert(resolution.Source == EncounterSource.Pillar, "Pillar resolutions must be tagged as Pillar.");
        Assert(
            resolution.PendingDecisions.Single() is MoodDecision { MoodName: "Tribute" } mood &&
            mood.Consequence.Contains("raw lodestone", StringComparison.OrdinalIgnoreCase),
            "Pillar Bandit Tribute must demand raw lodestone or rations.");
    }

    private static void RuinMonsterAndMoodResolve()
    {
        // Total 22 = Wyrm (quantity 1, no die rolled).
        var wyrm = EncounterResolver.ResolveRuin(22, SoloParty(), new ScriptedRandom());
        Assert(
            wyrm.PendingDecisions.Single() is CombatDecision { EnemyGroup: "Wyrm", CombatantCount: 1, StatBlockName: "Wyrm" },
            "A Ruin Wyrm must start single combat with a Wyrm stat block.");

        // Total 14 = Delvers (requires mood). Count 1d8, then mood 6 = Helpful.
        var delvers = EncounterResolver.ResolveRuin(14, SoloParty(), new ScriptedRandom(6, 6));
        Assert(
            delvers.PendingDecisions.Single() is MoodDecision { EncounterName: "Delvers", MoodName: "Helpful" } mood &&
            mood.Consequence.Contains("2000 coin", StringComparison.Ordinal),
            "Helpful Delvers must trade up to their printed 2000-coin limit.");
    }

    private static void DirectCrawlEncounterStartsCombat()
    {
        var resolution = EncounterResolver.ResolveCrawl(CrawlCreature.Wyrm, 1);
        Assert(resolution.Source == EncounterSource.Crawl, "Direct Crawl resolutions must be tagged as Crawl.");
        Assert(
            resolution.PendingDecisions.Single() is CombatDecision { StatBlockName: "Wyrm", CombatantCount: 1 },
            "A direct Crawl encounter must start combat with the named stat block.");
        Assert(ThrowsArgumentOutOfRange(() => EncounterResolver.ResolveCrawl(CrawlCreature.Wyrm, 0)), "A Crawl encounter requires at least one creature.");
    }

    private static void RoamingHazardProducesSavesCombatAndDamage()
    {
        // Die 6 = Singing Sand on non-solid ground -> one Breath save per traveler, no dice consumed.
        var singingSand = EncounterResolver.ResolveRoamingHazard(
            6,
            SoloParty(),
            new RoamingHazardContext(Terrain.Wastes, OnSolidOrRockyGround: false),
            new ScriptedRandom());
        Assert(singingSand.Source == EncounterSource.RoamingHazard, "Hazard resolutions must be tagged as RoamingHazard.");
        Assert(
            singingSand.PendingDecisions.Single() is SavingThrowDecision { SaveType: "Breath" },
            "Singing Sand on loose ground must demand a Breath save.");

        // Die 1 = Warband -> 5d6 hostile Cutthroats.
        var warband = EncounterResolver.ResolveRoamingHazard(
            1,
            SoloParty(),
            new RoamingHazardContext(Terrain.Wastes),
            new ScriptedRandom(6, 6, 6, 6, 6));
        Assert(
            warband.PendingDecisions.Single() is CombatDecision { EnemyGroup: "Warband", CombatantCount: 30, Disposition: "Hostile" },
            "A Warband must start hostile combat with the rolled 5d6 count.");

        // Die 2 = Maelstrom in the open -> displacement (d6 direction) plus 3d20 applied damage.
        var maelstrom = EncounterResolver.ResolveRoamingHazard(
            2,
            SoloParty(),
            new RoamingHazardContext(Terrain.Wastes),
            new ScriptedRandom(3, 10, 10, 10));
        Assert(
            maelstrom.AppliedDamage.Single() is { Amount: 30 },
            "Maelstrom must apply the rolled 3d20 damage to the caught Traveler.");
        Assert(maelstrom.RoamingHazard is not null, "Hazard resolutions must carry their mechanical detail for travel callers.");
    }
}
