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
        RuinTablesMatchPages20To31();
        DeepRitesAndCrawlMatchPages32To41();
        TextStaysFreeOfOcrArtifacts();
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

    private static void RuinTablesMatchPages20To31()
    {
        // Pages 22-27 room registry: the printed grid omits 32 and prints 45 twice.
        Assert(RuinRoomRegistry.Get(1, 1).Single().Name == "Plaza", "Ruin room 11 must be Plaza.");
        Assert(RuinRoomRegistry.Get(6, 6).Single().Name == "Observatory", "Ruin room 66 must be Observatory.");
        Assert(RuinRoomRegistry.Get(3, 2).Count == 0, "Ruin room code 32 is absent from the printed table and must stay empty.");
        AssertNames(
            RuinRoomRegistry.Get(4, 5).Select(room => room.Name),
            ["Reliquary", "Fountain"],
            "Ruin room code 45 must preserve both printed entries.");
        AssertThrows<ArgumentOutOfRangeException>(() => RuinRoomRegistry.Get(0, 1), "Ruin room rolls below 1 must be invalid.");
        AssertThrows<ArgumentOutOfRangeException>(() => RuinRoomRegistry.Get(1, 7), "Ruin room rolls above 6 must be invalid.");

        // Room effect procedures (saves/damage) must survive transcription.
        Assert(RuinRoomEffectRules.Get(45, "Reliquary").Count == 1 && RuinRoomEffectRules.Get(45, "Fountain").Count == 1, "Code 45 must resolve Reliquary and Fountain effects independently.");
        Assert(RuinRoomEffectRules.Get(15, "Oubliette").Single() is { Save: "Breath", Damage: "1d6×10 fall" }, "Oubliette must require a Breath save and a 1d6×10 fall.");
        Assert(RuinRoomEffectRules.Get(13, "Archive").Single().Damage == "10d6", "Archive domino must keep its 10d6 damage.");
        Assert(RuinRoomEffectRules.Get(63, "Quarry").Single() is { Save: "Breath", Damage: "5d6" }, "Quarry crossing must keep its Breath save and 5d6 damage.");

        // Page 28 features: the printed table duplicates 25 and omits 24.
        Assert(RuinFeatureRules.Get(1).Single().Name == "Warning", "Feature 1 must be Warning.");
        Assert(RuinFeatureRules.Get(24).Count == 0, "Feature code 24 is absent from the printed table and must stay empty.");
        AssertNames(
            RuinFeatureRules.Get(25).Select(feature => feature.Name),
            ["Vein of Metal", "Hideout"],
            "Feature code 25 must preserve both printed entries.");
        Assert(RuinFeatureRules.Get(16).Single().Name == "Stairs Down", "Feature 16 must be Stairs Down.");
        Assert(RuinFeatureRules.Get(40).Single().Name == "Entrance to the Deep", "Feature 31+ (depth-adjusted) must remain Entrance to the Deep.");
        AssertThrows<ArgumentOutOfRangeException>(() => RuinFeatureRules.Get(0), "Feature totals below 1 must be invalid.");

        // Page 29 discoveries.
        AssertNames(
            Enumerable.Range(1, 6).Select(roll => RuinDiscoveryRules.Get(roll).Name),
            ["Inscrutable Art", "Esoteric Records", "Curious Currency", "Lost Architecture", "Lost Habitation", "Dangerous Artifact"],
            "Page 29 discovery table changed.");
        Assert(RuinDiscoveryRules.Get(6).Effect.Contains("Great and Terrible", StringComparison.Ordinal), "Dangerous Artifact must reference Something Great and Terrible.");
        AssertThrows<ArgumentOutOfRangeException>(() => RuinDiscoveryRules.Get(7), "Discovery rolls above 6 must be invalid.");

        // Page 30 encounters (1d12 + depth) and stat blocks.
        Assert(RuinEncounterRules.Get(1).Name == "Nothing", "Ruin encounter 1-5 must be Nothing.");
        Assert(RuinEncounterRules.Get(6).Name == "Lost Travelers", "Ruin encounter 6 must be Lost Travelers.");
        Assert(RuinEncounterRules.Get(22).Name == "Wyrm" && RuinEncounterRules.Get(30).Name == "Wyrm", "Ruin encounter 22+ must remain Wyrm.");
        Assert(RuinEncounterRules.GetMood("Bandits", 3).Name == "Tribute", "Ruin Bandit mood 3 must be Tribute.");
        Assert(RuinEncounterRules.GetMood("Delvers", 6).Name == "Helpful", "Ruin Delver mood 6 must be Helpful.");
        Assert(RuinEncounterRules.GetStatBlock("Delvers") is { HitDice: 5, HitPoints: 20 }, "Delver stat block must remain 5 HD / 20 HP.");
        AssertThrows<ArgumentOutOfRangeException>(() => RuinEncounterRules.Get(0), "Ruin encounter totals below 1 must be invalid.");

        // Page 31 treasures: depth-banded magnitude and category contents.
        Assert(RuinTreasureRules.GetMagnitude(10) == RuinTreasureMagnitude.Useful, "Treasure totals 1-10 must be Useful.");
        Assert(RuinTreasureRules.GetMagnitude(11) == RuinTreasureMagnitude.Special && RuinTreasureRules.GetMagnitude(19) == RuinTreasureMagnitude.Special, "Treasure totals 11-19 must be Special.");
        Assert(RuinTreasureRules.GetMagnitude(20) == RuinTreasureMagnitude.GreatAndTerrible, "Treasure totals 20+ must be Great and Terrible.");
        AssertThrows<ArgumentOutOfRangeException>(() => RuinTreasureRules.GetMagnitude(0), "Treasure totals below 1 must be invalid.");
        Assert(RuinTreasureRules.Get(RuinTreasureMagnitude.Useful, 2).Name == "Spell Eater", "Useful treasure 2 must be Spell Eater.");
        Assert(RuinTreasureRules.Get(RuinTreasureMagnitude.GreatAndTerrible, 10).Name == "Commune", "Great and Terrible treasure 10 must be Commune.");
        Assert(Enumerable.Range(1, 12).All(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.Useful, roll) is not null), "Useful treasures must cover rolls 1-12.");
        Assert(Enumerable.Range(1, 12).All(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.Special, roll) is not null), "Special treasures must cover rolls 1-12.");
        Assert(Enumerable.Range(1, 10).All(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.GreatAndTerrible, roll) is not null), "Great and Terrible treasures must cover rolls 1-10.");
        AssertThrows<ArgumentOutOfRangeException>(() => RuinTreasureRules.Get(RuinTreasureMagnitude.GreatAndTerrible, 11), "Great and Terrible treasures stop at roll 10.");
    }

    private static void DeepRitesAndCrawlMatchPages32To41()
    {
        // Page 32 Gifts of the Deep.
        AssertNames(
            Enumerable.Range(1, 10).Select(roll => DeepGiftRules.Get(roll).Gift.ToString()),
            ["Torpor", "VoiceOfTheCrawl", "GraftedLimbs", "GiftOfTheCyclops", "TasteForFlesh", "VoidOfPresence", "LodestoneHunger", "UnhollowCannibal", "Melder", "GravitySpider"],
            "Page 32 Gifts of the Deep table changed.");
        Assert(DeepGiftRules.Get(3).OncePerDay && DeepGiftRules.Get(6).OncePerDay, "Grafted Limbs and Void of Presence must remain once-per-day gifts.");
        Assert(!DeepGiftRules.Get(1).OncePerDay, "Torpor must not be once-per-day.");
        AssertThrows<ArgumentOutOfRangeException>(() => DeepGiftRules.Get(11), "Gift rolls above 10 must be invalid.");

        // Page 33 Minotaur and Touch effects.
        Assert(MinotaurRules.StatBlock is { HitDice: 20, Attack: "Touch of the Minotaur" }, "Minotaur stat block changed.");
        Assert(MinotaurRules.ResolveTouch(1, new ScriptedRandom(4)).Effect == MinotaurTouchEffect.WitherTools, "Minotaur touch 1 must wither tools.");
        Assert(MinotaurRules.ResolveTouch(5, new ScriptedRandom(2)) is { Effect: MinotaurTouchEffect.DrinkFlesh, Permanent: true }, "Minotaur touch 5 must permanently drink Flesh.");
        Assert(MinotaurRules.ResolveTouch(6, new ScriptedRandom()).MemoryLost, "Minotaur touch 6 must devour a memory.");
        AssertThrows<ArgumentOutOfRangeException>(() => MinotaurRules.ResolveTouch(7, new ScriptedRandom()), "Minotaur touch rolls above 6 must be invalid.");

        // Pages 34-35 trials.
        AssertNames(
            Enum.GetValues<DeepTrial>().Select(trial => DeepTrialRules.Get(trial).Trial.ToString()),
            ["Scale", "Repetition", "Change", "Emptiness", "Sacrifice", "Lies"],
            "Pages 34-35 trial set changed.");
        Assert(DeepTrialRules.Get(DeepTrial.Emptiness).Danger.Contains("No gravity", StringComparison.Ordinal), "Emptiness trial must keep its no-gravity danger.");
        Assert(DeepTrialRules.Get(DeepTrial.Scale).WayOut.Contains("original entry", StringComparison.Ordinal), "Scale trial way-out text changed.");

        // Page 37 Leaving the Vast ritual must enforce its source ordering.
        var ritual = new EscapeTheVastRitual();
        Assert(ThrowsInvalidOperation(() => ritual.SearchCannyClearBenign()), "The ritual must reject steps taken out of order.");
        ritual.SeekLushMendedFamiliar();
        ritual.SearchCannyClearBenign();
        ritual.HideSpotOutsideSenses();
        ritual.SpeakWishToLeaveTheVast();
        Assert(ritual.FallForward() == VastTerminalOutcome.EscapedHome, "Completing the ritual sequence must escape home.");

        // Page 38 Rite ledger.
        var ledger = new RiteLedger();
        Assert(ledger.GainFromMotion("ruin-entrance") && !ledger.GainFromMotion("ruin-entrance"), "Motions of the Labyrinth must reward a location only once.");
        Assert(ThrowsInvalidOperation(() => ledger.GainFromShunningLight(false)), "Shunning Light must require a sacrifice.");
        ledger.GainFromErosionOfSelf();
        Assert(ledger.LockedErosionExhaustion == 1, "Erosion of Self must lock one exhaustion.");
        Assert(ledger.TrySpendToCast() && !new RiteLedger().TrySpendToCast(), "Casting must spend an available Rite and fail with none.");

        // Pages 38-39 rite spells and their schools.
        Assert(Enum.GetValues<RiteSpell>().Length == 15, "There must remain 15 rites across the four schools.");
        Assert(Enum.GetValues<RiteSpell>().Count(spell => RiteSpellRules.Get(spell).School == RiteSchool.Labyrinth) == 4, "Labyrinth must hold four rites.");
        Assert(Enum.GetValues<RiteSpell>().Count(spell => RiteSpellRules.Get(spell).School == RiteSchool.Sparks) == 3, "Sparks must hold three rites.");
        Assert(!RiteSpellRules.Get(RiteSpell.Cinderhowl).CostsRite && !RiteSpellRules.Get(RiteSpell.BrightHand).CostsRite, "Spark rites are free-cast and must not cost a Rite.");
        Assert(RiteSpellRules.Get(RiteSpell.WildSeeking).CostsRite, "Non-Spark rites must cost a Rite.");
        Assert(RiteSpellRules.FickleDescentLevels(3, 2, true) == 2 && RiteSpellRules.FickleDescentLevels(3, 2, false) == -2, "Fickle Descent must ascend on face and descend on tail.");
        AssertThrows<ArgumentOutOfRangeException>(() => RiteSpellRules.FickleDescentLevels(3, 4, true), "Fickle Descent cannot exceed caster level.");
        Assert(RiteSpellRules.SunderDurationSeconds(2) == 12, "Sunder to Dust must last six seconds per level.");

        // Pages 40-41 Crawl creatures.
        AssertNames(
            Enum.GetValues<CrawlCreature>().Select(creature => CrawlCreatureRules.Get(creature).Creature.ToString()),
            ["Cyclops", "Medusa", "Harpy", "Griffon", "Siren", "Centaur", "Hydra", "Shade", "Ogre", "Wyrm"],
            "Pages 40-41 Crawl roster changed.");
        Assert(CrawlCreatureRules.Get(CrawlCreature.Wyrm) is { HitDice: 15, HitPoints: 150 }, "Wyrm stat block must remain 15 HD / 150 HP.");
        Assert(CrawlCreatureRules.Get(CrawlCreature.Cyclops) is { HitDice: 2, HitPoints: 10 }, "Cyclops stat block must remain 2 HD / 10 HP.");
        Assert(CrawlCreatureRules.CyclopsCallsAlly(new ScriptedRandom(1)), "Cyclops Call must trigger on a rolled 1.");
        Assert(CrawlCreatureRules.MedusaScream(charmSaveSucceeded: true) == (false, true), "A successful Medusa save must avoid stun and grant future advantage.");
        Assert(CrawlCreatureRules.GriffonSwallows(15, holdSaveSucceeded: false), "Griffon must swallow at 15 devour damage on a failed Hold save.");
        Assert(CrawlCreatureRules.WyrmHowl(breathSaveSucceeded: true, new ScriptedRandom(2, 2, 2)) == (3, false, 0), "A successful Wyrm Howl save must halve damage and avoid deafness.");
    }

    private static void TextStaysFreeOfOcrArtifacts()
    {
        var procedures = RuinRoomEffectRules.All
            .SelectMany(rule => new[] { rule.RoomName, rule.Procedure, rule.Save, rule.Damage })
            .Concat(Enumerable.Range(1, 31).SelectMany(total => RuinFeatureRules.Get(total)).SelectMany(feature => new[] { feature.Name, feature.Effect }))
            .Concat(Enumerable.Range(1, 6).Select(roll => RuinDiscoveryRules.Get(roll).Effect))
            .Concat(Enumerable.Range(1, 22).Select(total => RuinEncounterRules.Get(total).Description))
            .Concat(Enumerable.Range(1, 12).Select(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.Useful, roll).Effect))
            .Concat(Enumerable.Range(1, 12).Select(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.Special, roll).Effect))
            .Concat(Enumerable.Range(1, 10).Select(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.GreatAndTerrible, roll).Effect))
            .Concat(Enumerable.Range(1, 10).Select(roll => DeepGiftRules.Get(roll).Effect))
            .Concat(Enum.GetValues<CrawlCreature>().Select(creature => CrawlCreatureRules.Get(creature).Special))
            .Concat(Enum.GetValues<RiteSpell>().Select(spell => RiteSpellRules.Get(spell).Effect))
            .Where(text => text is not null)
            .Select(text => text!)
            .ToArray();

        // The source uses '×' for dice multiplication (e.g. 1d6×10). OCR commonly turns this into 'x' or '*'.
        Assert(procedures.Any(text => text.Contains('×')), "At least one rule must retain a real multiplication sign, confirming the guard is wired to live data.");
        foreach (var text in procedures)
        {
            for (var index = 1; index < text.Length - 1; index++)
            {
                var isFakeMultiplier = (text[index] is 'x' or 'X' or '*') && char.IsDigit(text[index - 1]) && char.IsDigit(text[index + 1]);
                Assert(!isFakeMultiplier, $"Suspected OCR-broken multiplication sign in: '{text}'.");
            }

            Assert(!text.Any(character => character is '‘' or '’' or '“' or '”'), $"Smart quotes must be normalized to straight quotes in: '{text}'.");
        }

        // Merged-heading guard: a name field that absorbed body text would gain a colon or grow oversized.
        var names = Enumerable.Range(1, 6)
            .SelectMany(first => Enumerable.Range(1, 6).SelectMany(second => RuinRoomRegistry.Get(first, second)))
            .Select(room => room.Name)
            .Concat(Enumerable.Range(1, 31).SelectMany(total => RuinFeatureRules.Get(total)).Select(feature => feature.Name))
            .ToArray();
        foreach (var name in names)
        {
            Assert(!name.Contains(':') && name.Length is > 0 and < 40, $"Suspected merged heading in name: '{name}'.");
        }
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
