using VastDark.Domain;
using static TestKit;

internal static class RuleTableTests
{
    public static void Run()
    {
        TravelerQuirksMatchPage6();
        NavigationWeatherAndRoamingHazardsMatchPages9To12();
        WastesEncountersAndFactionsMatchPages12To13();
        PillarTablesMatchPages14To15();
        SettlementTablesMatchPages16To19();
    }

    private static void TravelerQuirksMatchPage6()
    {
        var expectedNames = new[]
        {
            "Ruin Plucker",
            "Enigmatic Paranoia",
            "Hollow Fortitude",
            "Labrinthiosis",
            "Magnetoception",
            "Vacant Amygdala",
            "Distant Appetite",
            "Vampyr",
            "Vicious Abandon",
            "Wind Seer",
            "Dreamless",
            "Unreadable",
            "Psychitabolism",
            "Psionherd",
            "Long-walker",
            "Gentle Presence",
            "Candles in the Dark",
            "Cold Blood",
            "Dull Psyche",
            "Memetic",
        };

        AssertNames(
            Enumerable.Range(1, 20).Select(roll => TravelerQuirkRules.Get(roll).Name),
            expectedNames,
            "Page 6 Traveler quirk names must stay aligned to the 1d20 table.");
        Assert(TravelerQuirkRules.Get(1).CanBeTakenMultipleTimes, "Ruin Plucker is explicitly repeatable.");
        Assert(TravelerQuirkRules.Get(15).RuleText.Contains("6 extra miles", StringComparison.Ordinal), "Long-walker must preserve its travel benefit.");
        AssertThrows<ArgumentOutOfRangeException>(() => TravelerQuirkRules.Get(0), "Traveler quirks must reject rolls below 1.");
        AssertThrows<ArgumentOutOfRangeException>(() => TravelerQuirkRules.Get(21), "Traveler quirks must reject rolls above 20.");
    }

    private static void NavigationWeatherAndRoamingHazardsMatchPages9To12()
    {
        Assert(DailyNavigationService.BaseLostChanceInSix == 5, "Page 9 navigation starts with a 5-in-6 lost chance before assets.");
        AssertNames(
            Enum.GetValues<NavigationAsset>().Select(asset => asset.ToString()),
            ["Landmark", "Directions", "Tool", "Light", "DeadReckoning"],
            "Page 9 navigation assets changed.");
        Assert(DailyNavigationService.Resolve([NavigationAsset.Landmark, NavigationAsset.Directions, NavigationAsset.Tool, NavigationAsset.Light], new ScriptedRandom(1)).Effect == LostEffect.Late, "Four navigation assets must leave only the Late failure.");

        foreach (var roll in Enumerable.Range(2, 5))
        {
            Assert(WastesWeatherRules.Get(roll).Effect == WastesWeatherEffect.Calm, "Wastes weather 2-6 must be Calm.");
        }

        Assert(WastesWeatherRules.Get(7) is { Effect: WastesWeatherEffect.DustStorm, TravelMilesLost: 6, ObscuresLandmarks: true }, "Weather 7 must be Dust Storm with 6 miles lost and obscured landmarks.");
        Assert(WastesWeatherRules.Get(8) is { Effect: WastesWeatherEffect.WindBlast, DamageDice: "3d6" }, "Weather 8 must be Wind Blast with 3d6 damage.");
        Assert(WastesWeatherRules.Get(9) is { Effect: WastesWeatherEffect.StoneHail, SaveType: "Breath", DamageDice: "3d6" }, "Weather 9 must be Stone Hail with Breath save and 3d6 damage.");
        Assert(WastesWeatherRules.Get(10) is { Effect: WastesWeatherEffect.PillarFog, EncounterRollModifier: 6, ObscuresLandmarks: true }, "Weather 10 must be Pillar Fog with +6 encounter modifier.");
        Assert(WastesWeatherRules.Get(11) is { Effect: WastesWeatherEffect.GritSlide, TravelMilesLost: 6, SaveType: "Breath" }, "Weather 11 must be Grit Slide with travel loss and Breath save.");
        Assert(WastesWeatherRules.Get(12).Effect == WastesWeatherEffect.DuneWave, "Weather 12 must be Dune Wave.");
        AssertThrows<ArgumentOutOfRangeException>(() => WastesWeatherRules.Get(1), "Weather rolls below 2 must be invalid.");
        AssertThrows<ArgumentOutOfRangeException>(() => WastesWeatherRules.Get(13), "Weather rolls above 12 must be invalid.");

        var hazards = Enumerable.Range(1, 6).Select(RoamingHazardRules.Get).ToArray();
        AssertNames(
            hazards.Select(rule => rule.Name),
            ["Warband", "Maelstrom", "Crawlherd", "Collapse", "Void Lightning", "Singing Sand"],
            "Page 11 roaming hazard names changed.");
        Assert(hazards[0] is { Kind: RoamingHazardResolutionKind.Combat, EncounterDiceSides: 6 }, "Warband must spawn 5d6 Cutthroats.");
        Assert(hazards[1] is { Kind: RoamingHazardResolutionKind.DisplacementDamage, DamageDice: "3d20" }, "Maelstrom must displace and deal 3d20 damage.");
        Assert(hazards[2] is { Kind: RoamingHazardResolutionKind.Combat, EncounterDiceSides: 20 }, "Crawlherd must spawn 1d20 Crawl.");
        Assert(hazards[3] is { Kind: RoamingHazardResolutionKind.ExhaustionOrCrush, TerrainDestructionChanceInSix: 2 }, "Collapse must have a 2-in-6 terrain destruction chance.");
        Assert(hazards[4] is { Kind: RoamingHazardResolutionKind.ConditionalLightning, DamageDice: "10d6", TerrainDestructionChanceInSix: 3 }, "Void Lightning must preserve 10d6 damage and 3-in-6 terrain destruction.");
        Assert(hazards[5] is { Kind: RoamingHazardResolutionKind.SaveOrDisappear, SaveType: "Breath" }, "Singing Sand must require Breath saves.");
    }

    private static void WastesEncountersAndFactionsMatchPages12To13()
    {
        AssertNames(
            Enumerable.Range(1, 18).Select(roll => WastesEncounterRules.GetEncounter(roll).Name),
            ["Nothing", "Nothing", "Nothing", "Nothing", "Nothing", "Lost Travelers", "Nomads", "Merchants", "Bandits", "Pilgrims", "Lodestone Prospectors", "Caravan", "Cutthroats", "Cyclops", "Harpies", "Medusa", "Shade", "Griffon"],
            "Page 12 Wastes encounter table changed.");
        Assert(WastesEncounterRules.GetEncounter(12).Description.Contains("limit 1000 coin", StringComparison.Ordinal), "Caravan trade limit must remain 1000 coin.");
        Assert(WastesEncounterRules.GetMood("Nomads", 1).Name == "Cautious", "Nomad mood 1 must be Cautious.");
        Assert(WastesEncounterRules.GetMood("Nomads", 6).Name == "Friendly", "Nomad mood 6 must be Friendly.");
        Assert(WastesEncounterRules.GetMood("Bandits", 3).Consequence.Contains("100 coins", StringComparison.Ordinal), "Bandit tribute must preserve the 100 coin demand.");
        Assert(WastesEncounterRules.GetMood("Cutthroats", 6).Name == "Recruit", "Cutthroat mood 6 must be Recruit.");

        AssertNames(
            Enumerable.Range(1, 20).Select(roll => WastesEncounterRules.GetCuriosity(roll).Name),
            ["Ruin outcropping", "Abandoned camp", "Stone totem", "Desiccated nomads", "Burial cairn", "Lodestone cache", "Nomad in black", "Collapsed tower", "Lodestone obelisk", "Tied Traveler", "Unearthed road", "Swarm", "Lonely graves", "Nest", "Crawl corpse", "Secret tunnel", "Message", "Lost caravan", "Bereft swordsman", "Forgotten treasure"],
            "Page 12 Wastes curiosity table changed.");

        AssertNames(
            Enum.GetValues<WastesFaction>().Select(faction => WastesFactionRules.Get(faction).Name),
            ["Lodestone Brokers", "Candlekeepers", "Dust Anglers", "Pillar Worms"],
            "Page 13 Wastes faction names changed.");
        Assert(WastesFactionRules.Get(WastesFaction.LodestoneBrokers).AbilityName == "What's Fair is Fair", "Lodestone Brokers ability name changed.");
        Assert(WastesFactionRules.Get(WastesFaction.Candlekeepers).AbilityName == "A Burden Shared", "Candlekeepers ability name changed.");
        Assert(WastesFactionRules.Get(WastesFaction.DustAnglers).Description.Contains("1d6 rations", StringComparison.Ordinal), "Dust Anglers must hunt 1d6 rations.");
        Assert(WastesFactionRules.Get(WastesFaction.PillarWorms).Description.Contains("required or useful tool", StringComparison.Ordinal), "Pillar Worms must substitute for tools.");
    }

    private static void PillarTablesMatchPages14To15()
    {
        AssertNames(
            Enumerable.Range(1, 15).Select(roll => PillarEncounterRules.Get(roll).Name),
            ["Nothing", "Nothing", "Lost Travelers", "Lodestone Miners", "Merchants", "Cyclops", "Bandits", "Harpies", "Cutthroats", "Medusa", "Cyclops", "Ogre", "Harpies", "Shade", "Griffon"],
            "Page 14 Pillar encounter table changed.");
        Assert(PillarEncounterRules.Get(20).Name == "Griffon", "Pillar encounter rolls of 15+ must remain Griffon.");
        Assert(PillarEncounterRules.GetMood("Lodestone Miners", 1).Name == "Territorial", "Lodestone Miners mood 1 must be Territorial.");
        Assert(PillarEncounterRules.GetMood("Bandits", 4).Consequence.Contains("raw lodestone", StringComparison.OrdinalIgnoreCase), "Pillar Bandit tribute must demand Raw Lodestone or rations.");
        Assert(PillarEncounterRules.GetMood("Cutthroats", 6).Name == "Recruit", "Pillar Cutthroat mood 6 must be Recruit.");

        var gatheringInventory = new TravelerInventory(constitutionModifier: 2);
        var gathering = PillarMiningService.WorkHour(PillarWork.Gathering, hasMiningTools: false, gatheringInventory, new ScriptedRandom(2, 6));
        Assert(gathering.RawLodestoneRolled == 2 && gathering.RawLodestoneCollected == 2 && gathering.EncounterRollModifier == 6, "Pillar gathering must roll 1d2 Raw Lodestone and +1d6 encounter modifier.");
        AssertThrows<InvalidOperationException>(() => PillarMiningService.WorkHour(PillarWork.Mining, hasMiningTools: false, new TravelerInventory(2), new ScriptedRandom(1, 1, 1)), "Pillar mining must require tools.");
        var mining = PillarMiningService.WorkHour(PillarWork.Mining, hasMiningTools: true, new TravelerInventory(1), new ScriptedRandom(6, 5, 6));
        Assert(mining.RawLodestoneRolled == 6 && mining.RawLodestoneCollected == 1 && mining.EncounterRollModifier == 11, "Pillar mining must roll 1d6 Raw Lodestone and +2d6 encounter modifier.");

        AssertNames(
            Enumerable.Range(1, 6).Select(roll => PillarDelvingRules.GetShape(roll).Name),
            ["Constricting Squeeze", "Sheer Drop", "Tight Halls", "Winding Tunnel", "Jagged Ascent", "Cavernous"],
            "Page 15 tunnel shape table changed.");
        Assert(PillarDelvingRules.GetEvent(1).Name == "Chill Fog" && PillarDelvingRules.GetEvent(3).Name == "Chill Fog", "Pillar event 1-3 must be Chill Fog.");
        Assert(PillarDelvingRules.GetEvent(15).Name == "Call of the Dark" && PillarDelvingRules.GetEvent(30).Name == "Call of the Dark", "Pillar event 15+ must be Call of the Dark.");
        Assert(PillarDelvingRules.GetLoot(1).Name == "Forgotten Corpse" && PillarDelvingRules.GetLoot(3).Name == "Forgotten Corpse", "Pillar loot 1-3 must be Forgotten Corpse.");
        Assert(PillarDelvingRules.GetLoot(14).Name == "Hoard" && PillarDelvingRules.GetLoot(30).Name == "Hoard", "Pillar loot 14+ must be Hoard.");
    }

    private static void SettlementTablesMatchPages16To19()
    {
        Assert(SettlementGenerationRules.GetPopulation(1).Population == SettlementPopulation.Barren && SettlementGenerationRules.GetPopulation(3).Population == SettlementPopulation.Barren, "Settlement population 1-3 must be Barren.");
        Assert(SettlementGenerationRules.GetPopulation(4).Population == SettlementPopulation.Middling && SettlementGenerationRules.GetPopulation(5).Population == SettlementPopulation.Middling, "Settlement population 4-5 must be Middling.");
        Assert(SettlementGenerationRules.GetPopulation(6).Population == SettlementPopulation.Overcrowded, "Settlement population 6 must be Overcrowded.");
        AssertNames(
            Enumerable.Range(1, 6).Select(roll => SettlementGenerationRules.GetScarcity(roll).Scarcity.ToString()),
            ["Desperate", "LimitedInventory", "SteepPrices", "DifficultBargains", "Middling", "Bountiful"],
            "Page 16 settlement scarcity table changed.");
        AssertNames(
            Enumerable.Range(1, 6).Select(roll => SettlementGenerationRules.GetAtmosphere(roll).Atmosphere.ToString()),
            ["Hidden", "Piety", "Mirth", "Despair", "Stoic", "Primal"],
            "Page 16 settlement atmosphere table changed.");

        Assert(!new SettlementMarket(SettlementScarcity.Desperate).Purchase(10, 1, supplies: true, offersBarterItem: false).Purchased, "Desperate settlements permit selling only.");
        Assert(new SettlementMarket(SettlementScarcity.SteepPrices).Purchase(10, 2, supplies: false, offersBarterItem: false).CoinCost == 40, "Steep Prices must double item costs.");
        Assert(!new SettlementMarket(SettlementScarcity.DifficultBargains).Purchase(10, 1, supplies: false, offersBarterItem: false).Purchased, "Difficult Bargains must require a barter item.");
        Assert(new SettlementMarket(SettlementScarcity.Bountiful).Purchase(10, 2, supplies: true, offersBarterItem: false).QuantityReceived == 3, "Bountiful settlements must grant one extra supply item.");
        var limited = new SettlementMarket(SettlementScarcity.LimitedInventory, new ScriptedRandom(2));
        Assert(limited.Purchase(10, 2, supplies: false, offersBarterItem: false).Purchased && limited.RemainingLimitedPurchases == 0, "Limited Inventory must track its rolled purchase count.");

        AssertNames(
            Enumerable.Range(1, 12).Select(roll => SettlementServiceRules.Get(roll).Name),
            ["Storyteller", "Scrap Smithy", "Apothecary", "Pyromancer Foundry", "Magus Sanctum", "Reservoir", "Bazaar", "Cartographer Roost", "Lodestone Carver", "Memorial Shrine", "Paddock", "Nomad Hold"],
            "Page 17 settlement locations changed.");
        Assert(SettlementServiceRules.LodestoneCarverCoins(3) == 300, "Lodestone Carver must exchange each Raw Lodestone for 100 coins.");
        Assert(SettlementServiceRules.Get(3).Services.Count == 5, "Apothecary must expose its five listed services.");

        AssertNames(
            Enum.GetValues<SettlementDenizen>().Select(denizen => SettlementDenizenRules.GetDenizen(denizen).Name),
            ["Nod", "Masque", "Flayed Dervish", "Sindr", "Old Tune", "Hool", "Skitter", "Dive"],
            "Page 18 settlement denizen names changed.");
        Assert(SettlementDenizenRules.GetDenizen(SettlementDenizen.Dive).Offer.Contains("safe ruin depth", StringComparison.Ordinal), "Dive must pay for safe ruin depths.");
        AssertNames(
            Enum.GetValues<SettlementFaction>().Select(faction => SettlementDenizenRules.GetFaction(faction).Name),
            ["Partisans of Flame", "Seeker Keepers", "Black Helms", "Grafters"],
            "Page 19 settlement faction names changed.");
        Assert(SettlementDenizenRules.GetFaction(SettlementFaction.SeekerKeepers).Ability.Contains("poor common tool", StringComparison.Ordinal), "Seeker Keepers ability must produce poor common tools.");
        Assert(SettlementDenizenRules.GetFaction(SettlementFaction.BlackHelms).Ability.Contains("memory lost", StringComparison.Ordinal), "Black Helms ability must key off memory loss.");
    }

    private static void AssertNames(IEnumerable<string> actual, IReadOnlyList<string> expected, string message)
    {
        var actualNames = actual.ToArray();
        Assert(actualNames.SequenceEqual(expected), $"{message} Expected [{string.Join(", ", expected)}], found [{string.Join(", ", actualNames)}].");
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
