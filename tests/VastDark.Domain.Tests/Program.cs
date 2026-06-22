using VastDark.Domain;

var regional = new RegionalMap(new Random(12345));
Assert(regional.Cells.Count == 80, "Regional map must contain 10 × 8 cells.");
Assert(regional.Contains(new RegionalCoord(9, 7)), "Regional map must include its final cell.");
Assert(!regional.Contains(new RegionalCoord(10, 7)), "Regional map must reject out-of-bounds cells.");
Assert(regional.DiceRolls.Count == RegionalMap.DiceCount, "Regional generation must place exactly eight dice.");
Assert(regional.DiceRolls.Keys.Distinct().Count() == RegionalMap.DiceCount, "Regional dice must occupy distinct hexes.");
foreach (var (coordinate, roll) in regional.DiceRolls)
{
    var expected = roll switch { 1 => Terrain.Wastes, <= 4 => Terrain.Ruins, _ => Terrain.Pillars };
    Assert(regional.GetTerrain(coordinate) == expected, "Regional die result must select the documented terrain.");
}
Assert(Enumerable.Range(1, 6).Select(RegionalMap.GetTerrainForRoll).SequenceEqual([Terrain.Wastes, Terrain.Ruins, Terrain.Ruins, Terrain.Ruins, Terrain.Pillars, Terrain.Pillars]), "Regional d6 terrain table must match page 10 for every roll.");
Assert(Enumerable.Range(1, 6).Select(LocalMap.GetRequestedDiceCountForDensityRoll).SequenceEqual([6, 6, 6, 12, 12, 32]), "Local density d6 table must match page 10 for every roll.");
Assert(Enumerable.Range(1, 6).Select(roll => LocalMap.GetTerrainForRoll(Terrain.Ruins, roll)).SequenceEqual([Terrain.Wastes, Terrain.Ruins, Terrain.Ruins, Terrain.Ruins, Terrain.Settlements, Terrain.Settlements]), "Ruins local d6 terrain table must match page 10 for every roll.");
Assert(Enumerable.Range(1, 6).Select(roll => LocalMap.GetTerrainForRoll(Terrain.Wastes, roll)).SequenceEqual([Terrain.Wastes, Terrain.Wastes, Terrain.Wastes, Terrain.Wastes, Terrain.Wastes, Terrain.Ruins]), "Wastes local d6 terrain table must match page 10 for every roll.");
Assert(Enumerable.Range(1, 6).Select(roll => LocalMap.GetTerrainForRoll(Terrain.Pillars, roll)).All(terrain => terrain == Terrain.Pillars), "Pillars local maps must remain Pillars for every density die result.");

var local = new LocalMap(new RegionalCoord(0, 0));
Assert(local.Cells.Count == 91, "The local tessellation must cover every clipped edge subhex.");
Assert(LocalMap.SideLengthInSubhexes == 6, "Local map must have six one-mile subhexes along each edge.");
Assert(LocalMap.FlatToFlatSubhexes == 6, "Local map must span six one-mile hex widths flat-to-flat.");
Assert(local.Contains(HexCoord.Zero), "Local map must include its centre.");
Assert(local.Contains(new HexCoord(5, 0)), "Local map must include its sixth edge subhex.");
Assert(!local.Contains(new HexCoord(6, 0)), "Local map must exclude cells outside its six-side footprint.");
Assert(HexCoord.Zero.DistanceTo(new HexCoord(2, -1)) == 2, "Axial hex distance must be correct.");
Assert(local.RoamingHazards.Count is >= 1 and <= 6, "Local maps must start with 1d6 roaming hazards.");
Assert(local.VisibleCells.Count == 41, $"The local map must expose the 41 fully visible subhexes; found {local.VisibleCells.Count}.");
Assert(local.RoamingHazards.Keys.All(local.VisibleCells.Contains), "Roaming hazards must start on fully visible local cells.");
Assert(local.RoamingHazards.Values.All(roll => roll is >= 1 and <= 6), "Roaming hazard faces must be valid d6 values.");
var originalHazardFaces = local.RoamingHazards.Values.Order().ToArray();
local.AdvanceRoamingHazards(new Random(98765));
Assert(local.RoamingHazardDay == 1, "Advancing hazards must increment the local hazard day.");
Assert(local.RoamingHazards.Count == originalHazardFaces.Length, "Advancing hazards must preserve the number of hazards.");
Assert(local.RoamingHazards.Keys.Distinct().Count() == originalHazardFaces.Length, "Roaming hazards must occupy distinct cells after moving.");
Assert(local.RoamingHazards.Values.Order().SequenceEqual(originalHazardFaces), "Moving hazards must preserve their face values.");
Assert(Enumerable.Range(1, 6).Select(LocalMap.GetRoamingHazardName).SequenceEqual(["Warband", "Maelstrom", "Crawlherd", "Collapse", "Void Lightning", "Singing Sand"]), "Roaming hazard names must match the full page 11 d6 table.");
Assert(VitalityRules.StartingGrit(2, 1, [5, 7]) == 13 && VitalityRules.StartingFlesh(2, 3) == 5, "Starting Grit and Flesh must follow the page 7 formulas.");
var vitalityScores = new AbilityScores(8, 10, 14, 12, 18, 7);
var vitalityTraveler = Traveler.CreateWithVitality("Vital", vitalityScores, level: 2, d8Rolls: [5, 7]);
Assert(vitalityTraveler.Vitality == new Vitality(14, 6), "Traveler vitality must derive Grit from Constitution and Flesh from the highest ability modifier.");
Assert(AbilityScoreRules.Roll3d6(new ScriptedRandom(1, 6, 3)) == 10, "DCC ability scores must roll 3d6.");
Assert(AbilityScoreRules.Modifier(3) == -4 && AbilityScoreRules.Modifier(9) == -1 && AbilityScoreRules.Modifier(10) == 0 && AbilityScoreRules.Modifier(11) == 0 && AbilityScoreRules.Modifier(18) == 4, "DCC ability modifiers must use floor((score - 10) / 2).");
var travelerQuirks = Enumerable.Range(1, 20).Select(TravelerQuirkRules.Get).ToArray();
Assert(travelerQuirks.Select(quirk => quirk.Name).Distinct().Count() == 20 && travelerQuirks.All(quirk => !string.IsNullOrWhiteSpace(quirk.RuleText)), "Every Traveler quirk d20 face must resolve to a distinct documented rule.");
Assert(TravelerQuirkRules.Get(1).CanBeTakenMultipleTimes && TravelerQuirkRules.Roll(new ScriptedRandom(15)).Name == "Long-walker", "Ruin Plucker must be repeatable and quirk rolling must use d20 faces.");
var harrowing = new Harrowing(["Books", "First kiss", "Knowledge", "Escape", "My name"]);
foreach (var (trigger, memory) in Enum.GetValues<HarrowingTrigger>().Zip(["Books", "First kiss", "Knowledge", "Escape"]))
{
    var harrowingResult = harrowing.Resolve(trigger, losesMemory: true, memoryToLose: memory);
    Assert(harrowingResult.MemoryLost && !harrowingResult.FinalMemoryLost, "Every documented Harrowing trigger must be able to remove a remaining memory.");
}
Assert(!harrowing.Resolve(HarrowingTrigger.GreatTragedy, losesMemory: false).MemoryLost, "A Harrowing trigger must not remove a memory unless the caller's source-defined chance succeeds.");
Assert(harrowing.Resolve(HarrowingTrigger.DroppingToZeroHitPoints, losesMemory: true, "My name").FinalMemoryLost, "The fifth lost memory must signal the source-defined final-memory outcome.");
AssertThrows(() => harrowing.Resolve(HarrowingTrigger.GreatTragedy, true, "Books"), "A lost Harrowing memory cannot be lost again.");
var scores = new AbilityScores(3, 8, 14, 10, 18, 11);
var scoredTraveler = new Traveler("Scored", abilityScores: scores);
Assert(scoredTraveler.GetAbilityScore(Ability.Constitution) == 14 && scoredTraveler.GetAbilityModifier(Ability.Constitution) == 2 && scores.HighestModifier == 4, "Travelers must retain score values and derive their DCC modifiers.");
Assert(new Traveler(scoredTraveler.ToState()).AbilityScores == scores && new Traveler("Legacy").AbilityScores == AbilityScores.Average, "Traveler ability scores must persist while legacy travelers receive average scores.");
Assert(InventoryRules.GetPack(PackType.Bindle) == new PackRule(PackType.Bindle, 2, 20) && InventoryRules.GetPack(PackType.Backpack) == new PackRule(PackType.Backpack, 10, 120), "Pack slots and costs must match page 7.");
Assert(InventoryRules.DailyMilesWithTransport(18, CargoTransportType.Pulk, 1) == 12 && InventoryRules.DailyMilesWithTransport(18, CargoTransportType.Sleigh, 2) == 12 && InventoryRules.DailyMilesWithTransport(18, CargoTransportType.Sleigh, 3) == 18, "Cargo transport speed restrictions must match page 7 puller limits.");
var inventory = new TravelerInventory(scoredTraveler.GetAbilityModifier(Ability.Constitution));
Assert(inventory.Capacity == 2, "A Traveler's base inventory slots must equal their Constitution bonus.");
Assert(inventory.AssignLoadoutAtSettlement("Navigation", 2, availableCoins: 20, atSettlement: true) == 20 && inventory.UsedSlots == 2, "Settlement loadouts must cost 10 coins and reserve their slots.");
inventory.DrawCommonItem("Navigation", new InventoryItem("Compass", 1));
Assert(inventory.Items.Single().Name == "Compass" && inventory.Loadouts.Single().Slots == 1 && inventory.UsedSlots == 2, "A common item must replace, not add to, its assigned loadout slots.");
var packCost = inventory.BuyPackAtSettlement(PackType.Backpack, availableCoins: 120, atSettlement: true);
inventory.RecordOwnItem(new InventoryItem("Relic", 3, isUniqueOrMagical: true));
Assert(packCost == 120 && inventory.Capacity == 12 && inventory.AvailableSlots == 7, "Packs must expand capacity and unique items must consume their own slots.");
AssertThrows(() => inventory.AssignLoadoutAtSettlement("Defense", 1, 10, atSettlement: false), "Inventory loadouts must be restricted to settlements.");
AssertThrows(() => inventory.DrawCommonItem("Navigation", new InventoryItem("Magic Compass", 1, isUniqueOrMagical: true)), "Unique or magical items cannot be drawn from a common-item loadout.");
var exhaustedTraveler = new Traveler("Exhausted");
exhaustedTraveler.AddExhaustion(1, ExhaustionSource.LostSleep);
exhaustedTraveler.AddExhaustion(1, ExhaustionSource.SevereWound);
exhaustedTraveler.AddExhaustion(1, ExhaustionSource.Hunger);
exhaustedTraveler.AddExhaustion(1, ExhaustionSource.ForcedMarch);
Assert(exhaustedTraveler.ExhaustionSources.SequenceEqual([ExhaustionSource.LostSleep, ExhaustionSource.SevereWound, ExhaustionSource.Hunger, ExhaustionSource.ForcedMarch]), "Exhaustion must retain each documented cause.");
Assert(exhaustedTraveler.RecoverExhaustionFromFullRest() && exhaustedTraveler.Exhaustion == 3 && new Traveler(exhaustedTraveler.ToState()).ExhaustionSources.Count == 3, "A full day of rest must remove one exhaustion level and preserve remaining sources.");
var absorbedDamage = VitalityRules.ApplyDamage(new Vitality(6, 4), 5);
Assert(absorbedDamage.Vitality == new Vitality(1, 4) && absorbedDamage.FleshDamage == 0, "Grit must absorb damage before Flesh.");
var fleshDamage = VitalityRules.ApplyDamage(new Vitality(2, 4), 5);
Assert(fleshDamage.Vitality.Flesh == 1 && fleshDamage.FleshDamage == 3 && fleshDamage.InjuryRequired, "Damage beyond Grit must reduce Flesh and require an injury.");
var injured = vitalityTraveler.TakeDamage(16, new ScriptedRandom(3));
Assert(injured is { InjuryRequired: true } && vitalityTraveler.Vitality!.InjuredAbility == Ability.Wisdom && vitalityTraveler.ExhaustionSources.Last() == ExhaustionSource.SevereWound, "Flesh damage must randomly record an ability injury and add severe-wound exhaustion.");
Assert(VitalityRules.RecoverGritAfterRest(new Vitality(0, 1), true, new ScriptedRandom(4, 6)).Grit == 10 && VitalityRules.RecoverFleshAtSettlement(new Vitality(0, 1)).Flesh == 2, "Full rest must heal 2d6 Grit and settlement recovery must heal one Flesh.");
Assert(RoamingHazardRules.Get(1).EncounterDiceSides == 6 && RoamingHazardRules.Get(1).Kind == RoamingHazardResolutionKind.Combat, "Warband must spawn 5d6 Cutthroats led by a Demagogue.");
Assert(RoamingHazardRules.Get(2).DamageDice == "3d20" && RoamingHazardRules.Get(2).Avoidance!.Contains("shelter", StringComparison.Ordinal), "Maelstrom must use 3d20 damage and shelter avoidance.");
Assert(RoamingHazardRules.Get(3).EncounterDiceSides == 20, "Crawlherd must spawn 1d20 Crawl.");
Assert(RoamingHazardRules.Get(4).TerrainDestructionChanceInSix == 2, "Collapse must have a 2-in-6 terrain destruction chance.");
Assert(RoamingHazardRules.Get(5).DamageDice == "10d6" && RoamingHazardRules.Get(5).TerrainDestructionChanceInSix == 3, "Void Lightning must use a 3-in-6 metal strike for 10d6 damage.");
Assert(RoamingHazardRules.Get(6).SaveType == "Breath", "Singing Sand must require a Breath save.");
var hazardParty = new TravelParty([new Traveler("Hazard A"), new Traveler("Hazard B")]);
var warband = RoamingHazardService.Resolve(1, hazardParty, new RoamingHazardContext(Terrain.Wastes), new ScriptedRandom(6, 6, 6, 6, 6));
Assert(warband.CombatantCount == 30, "Warbands must spawn 5d6 Cutthroats.");
var maelstrom = RoamingHazardService.Resolve(2, hazardParty, new RoamingHazardContext(Terrain.Wastes), new ScriptedRandom(0, 20, 20, 20, 5, 20, 20, 20));
Assert(maelstrom.Displacements.Count == 2 && maelstrom.Damage.All(hit => hit.Amount == 60), "An exposed Maelstrom must displace each Traveler and deal 3d20 damage.");
Assert(RoamingHazardService.Resolve(2, hazardParty, new RoamingHazardContext(Terrain.Ruins), new ScriptedRandom()).Damage.Count == 0, "Ruins must avoid Maelstrom effects.");
var collapse = RoamingHazardService.Resolve(4, hazardParty, new RoamingHazardContext(Terrain.Ruins), new ScriptedRandom(2));
Assert(collapse.ExhaustedTravelers.Count == 2 && collapse.TerrainReducedToWastes, "Running from a Collapse must add exhaustion and Ruins must become Wastes on 2-in-6.");
var lightning = RoamingHazardService.Resolve(5, hazardParty, new RoamingHazardContext(Terrain.Wastes, HasExposedMetal: true), new ScriptedRandom(3, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 4));
Assert(lightning.Damage.Count == 1 && lightning.Damage[0].Amount == 60, "Exposed metal must face a 3-in-6 Void Lightning strike for 10d6 damage.");
Assert(RoamingHazardService.Resolve(6, hazardParty, new RoamingHazardContext(Terrain.Wastes), new ScriptedRandom()).BreathSaveTravelers.Count == 2 && RoamingHazardService.Resolve(6, hazardParty, new RoamingHazardContext(Terrain.Wastes, OnSolidOrRockyGround: true), new ScriptedRandom()).BreathSaveTravelers.Count == 0, "Singing Sand must require Breath saves only on non-solid ground.");
Assert(Enumerable.Range(2, 11).Select(WastesWeatherRules.Get).Select(rule => rule.Effect).SequenceEqual([WastesWeatherEffect.Calm, WastesWeatherEffect.Calm, WastesWeatherEffect.Calm, WastesWeatherEffect.Calm, WastesWeatherEffect.Calm, WastesWeatherEffect.DustStorm, WastesWeatherEffect.WindBlast, WastesWeatherEffect.StoneHail, WastesWeatherEffect.PillarFog, WastesWeatherEffect.GritSlide, WastesWeatherEffect.DuneWave]), "Every 2d6 Wastes weather total must match the page 12 table.");
var weatherParty = new TravelParty([new Traveler("Weather A"), new Traveler("Weather B")]);
var dustStorm = WastesWeatherService.Resolve(7, weatherParty, new WastesWeatherContext(), new ScriptedRandom());
Assert(dustStorm.TravelMilesLost == 6 && dustStorm.LandmarksObscured, "Dust Storm must cost 6 miles and obscure landmarks.");
var windBlast = WastesWeatherService.Resolve(8, weatherParty, new WastesWeatherContext(InOpen: true), new ScriptedRandom(6, 6, 6, 6, 6, 6));
Assert(windBlast.LightsExtinguished && windBlast.Damage.All(hit => hit.Amount == 18), "Wind Blast must extinguish lights and damage exposed Travelers for 3d6.");
Assert(WastesWeatherService.Resolve(9, weatherParty, new WastesWeatherContext(Protected: false), new ScriptedRandom()).BreathSaveTravelers.Count == 2 && WastesWeatherService.Resolve(9, weatherParty, new WastesWeatherContext(Protected: true), new ScriptedRandom()).BreathSaveTravelers.Count == 0, "Stone Hail must require Breath saves only from unprotected Travelers.");
Assert(WastesWeatherService.Resolve(10, weatherParty, new WastesWeatherContext(), new ScriptedRandom()) is { EncounterRollModifier: 6, LandmarksObscured: true }, "Pillar Fog must add 6 to the encounter roll and obscure landmarks.");
Assert(WastesWeatherService.Resolve(11, weatherParty, new WastesWeatherContext(), new ScriptedRandom()) is { TravelMilesLost: 6, BreathSaveTravelers: { Count: 2 } }, "Grit Slide must cost 6 miles and require Breath saves.");
Assert(WastesWeatherService.Resolve(12, weatherParty, new WastesWeatherContext(RunFromDuneWave: true), new ScriptedRandom()).ExhaustedTravelers.Count == 2 && WastesWeatherService.Resolve(12, weatherParty, new WastesWeatherContext(RunFromDuneWave: false), new ScriptedRandom()).BuriedTravelers.Count == 2, "Dune Wave must exhaust runners or bury those who do not run.");
Assert(Enumerable.Range(1, 18).Select(WastesEncounterRules.GetEncounter).All(rule => !string.IsNullOrWhiteSpace(rule.Name)) && WastesEncounterRules.GetEncounter(7).RequiresMood && WastesEncounterRules.GetEncounter(13).RequiresMood, "Every Wastes encounter total must resolve and the three source mood encounters must require a mood roll.");
Assert(WastesEncounterRules.GetMood("Nomads", 1).Name == "Cautious" && WastesEncounterRules.GetMood("Nomads", 6).Name == "Friendly" && WastesEncounterRules.GetMood("Bandits", 3).Name == "Tribute" && WastesEncounterRules.GetMood("Cutthroats", 6).Name == "Recruit", "Wastes mood bands must match the source d6 tables.");
Assert(Enumerable.Range(1, 20).Select(WastesEncounterRules.GetCuriosity).Select(curiosity => curiosity.Name).Distinct().Count() == 20 && WastesEncounterRules.GetCuriosity(11).Description.Contains("landmark", StringComparison.OrdinalIgnoreCase), "Every Wastes curiosity d20 face must resolve to unique source content.");
Assert(Enum.GetValues<WastesFaction>().Select(WastesFactionRules.Get).All(rule => !string.IsNullOrWhiteSpace(rule.TrainingRequirement)) && WastesFactionRules.Get(WastesFaction.PillarWorms).AbilityName == "Grit and Bear It", "Every page 13 Wastes faction must retain its ability and training requirement.");
Assert(WastesFactionService.CanBarterWithoutCost(false, false) && WastesFactionService.CanBarterWithoutCost(true, true) && !WastesFactionService.CanBarterWithoutCost(false, true), "Lodestone Brokers must barter only matching common or magic item categories at no cost.");
var burdenBearer = new Traveler("Bearer");
Assert(WastesFactionService.TryShareBurden(burdenBearer, new Traveler("Ally"), withinArmsReach: true, allyWouldGainExhaustion: false, allyWouldLoseMemory: true) && burdenBearer.Exhaustion == 1 && !WastesFactionService.TryShareBurden(burdenBearer, new Traveler("Distant"), withinArmsReach: false, allyWouldGainExhaustion: false, allyWouldLoseMemory: true), "Candlekeepers must transfer either listed burden only in arm's reach.");
Assert(WastesFactionService.HuntInWastes(true, true, true, new ScriptedRandom(6)) == 6 && WastesFactionService.HuntInWastes(false, true, true, new ScriptedRandom(6)) == 0, "Dust Anglers must require Wastes travel, tools, and a day to gain 1d6 rations.");
WastesFactionService.SubstituteForTool(burdenBearer);
Assert(burdenBearer.Exhaustion == 2, "Pillar Worms must gain one exhaustion to substitute a useful tool.");
var miningInventory = new TravelerInventory(4);
var gathering = PillarMiningService.WorkHour(PillarWork.Gathering, hasMiningTools: false, miningInventory, new ScriptedRandom(2, 6));
Assert(gathering.RawLodestoneRolled == 2 && gathering.RawLodestoneCollected == 2 && gathering.EncounterRollModifier == 6 && miningInventory.UsedSlots == 2, "Pillar gathering must yield 1d2 raw lodestone, consume one slot each, and add 1d6 to encounters.");
var mining = PillarMiningService.WorkHour(PillarWork.Mining, hasMiningTools: true, miningInventory, new ScriptedRandom(6, 5, 4));
Assert(mining.RawLodestoneRolled == 6 && mining.RawLodestoneCollected == 2 && mining.EncounterRollModifier == 9 && miningInventory.UsedSlots == 4, "Pillar mining must require tools, roll 1d6 lodestone, add 2d6 encounter pressure, and respect inventory slots.");
Assert(PillarMiningService.RefineAtSettlement(miningInventory, 2, new ScriptedRandom(1, 10)) == 110 && miningInventory.UsedSlots == 2, "Each raw lodestone must refine at a settlement for 1d10 × 10 coins and free its slot.");
AssertThrows(() => PillarMiningService.WorkHour(PillarWork.Mining, hasMiningTools: false, new TravelerInventory(1), new ScriptedRandom()), "Pillar mining without tools must be rejected.");
Assert(Enumerable.Range(1, 14).Select(PillarEncounterRules.Get).All(rule => !string.IsNullOrWhiteSpace(rule.Name)) && PillarEncounterRules.Get(15).Name == "Griffon" && PillarEncounterRules.Get(99).Name == "Griffon", "Pillar encounter results must resolve every documented total and cap 15+ as Griffon.");
Assert(PillarEncounterRules.GetMood("Lodestone Miners", 1).Name == "Territorial" && PillarEncounterRules.GetMood("Lodestone Miners", 5).Name == "Friendly" && PillarEncounterRules.GetMood("Bandits", 3).Name == "Tribute" && PillarEncounterRules.GetMood("Cutthroats", 6).Name == "Recruit", "Pillar encounter mood bands must match the source.");
Assert(Enumerable.Range(1, 6).Select(PillarDelvingRules.GetShape).Select(shape => shape.Name).Distinct().Count() == 6, "Every Pillar tunnel d6 shape must resolve uniquely.");
var delve = new PillarDelve();
var firstTunnel = delve.EnterTunnel(new ScriptedRandom(2));
var splitTunnel = delve.EnterTunnel(new ScriptedRandom(2, 5));
Assert(firstTunnel.Depth == 1 && splitTunnel.Depth == 2 && splitTunnel.SplitMarker == 5 && PillarDelve.MinutesToTravelTunnel == 10 && PillarDelve.MinutesToSearchTunnel == 30, "Pillar delving must track depth, duplicate-shape splits, and documented careful travel/search time.");
Assert(delve.RollEvent(new ScriptedRandom(3)).Name == "Wind Blast" && delve.RollLoot(new ScriptedRandom(5)).Name == "Lodestone Idols", "Pillar events must escalate by prior rolls and loot by tunnel depth.");
Assert(PillarDelvingRules.GetEvent(15).Name == "Call of the Dark" && PillarDelvingRules.GetEvent(99).Name == "Call of the Dark" && PillarDelvingRules.GetLoot(14).Name == "Hoard" && PillarDelvingRules.GetLoot(99).Name == "Hoard", "Pillar event and loot tables must cap at their 15+ and 14+ outcomes.");
Assert(Enumerable.Range(1, 6).Select(SettlementGenerationRules.GetPopulation).Select(rule => rule.Population).SequenceEqual([SettlementPopulation.Barren, SettlementPopulation.Barren, SettlementPopulation.Barren, SettlementPopulation.Middling, SettlementPopulation.Middling, SettlementPopulation.Overcrowded]), "Settlement population d6 bands must match page 16.");
Assert(Enumerable.Range(1, 6).Select(SettlementGenerationRules.GetScarcity).Select(rule => rule.Scarcity).SequenceEqual(Enum.GetValues<SettlementScarcity>()) && Enumerable.Range(1, 6).Select(SettlementGenerationRules.GetAtmosphere).Select(rule => rule.Atmosphere).SequenceEqual(Enum.GetValues<SettlementAtmosphere>()), "Every scarcity and atmosphere d6 face must resolve to the source table.");
Assert(SettlementGenerationRules.Generate(new ScriptedRandom(6, 3, 5)) is { Population: { Population: SettlementPopulation.Overcrowded }, Scarcity: { Scarcity: SettlementScarcity.SteepPrices }, Atmosphere: { Atmosphere: SettlementAtmosphere.Stoic } }, "Settlement generation must independently roll population, scarcity, and atmosphere.");
Assert(!new SettlementMarket(SettlementScarcity.Desperate).Purchase(10, 1, supplies: true, offersBarterItem: true).Purchased && !new SettlementMarket(SettlementScarcity.DifficultBargains).Purchase(10, 1, supplies: false, offersBarterItem: false).Purchased, "Desperate and difficult-bargain scarcity must enforce their purchase restrictions.");
var limitedMarket = new SettlementMarket(SettlementScarcity.LimitedInventory, new ScriptedRandom(2));
Assert(limitedMarket.Purchase(10, 2, supplies: false, offersBarterItem: false).Purchased && !limitedMarket.Purchase(10, 1, supplies: false, offersBarterItem: false).Purchased, "Limited inventory must share a 1d6 collective purchase budget.");
Assert(new SettlementMarket(SettlementScarcity.SteepPrices).Purchase(10, 2, supplies: false, offersBarterItem: false).CoinCost == 40 && new SettlementMarket(SettlementScarcity.Bountiful).Purchase(10, 2, supplies: true, offersBarterItem: false).QuantityReceived == 3, "Steep Prices must double costs and Bountiful supplies must grant one extra item.");
var storytellerTraveler = new Traveler("Storyteller");
storytellerTraveler.AddExhaustion(1);
Assert(SettlementRestService.TryRecoverStorytellerExhaustion(storytellerTraveler, new ScriptedRandom(1)) && storytellerTraveler.Exhaustion == 0 && !SettlementRestService.TryRecoverStorytellerExhaustion(storytellerTraveler, new ScriptedRandom(2)), "Storytellers must grant extra recovery only on a 1-in-6 result when exhaustion exists.");
Assert(Enumerable.Range(1, 12).Select(SettlementServiceRules.Get).Select(location => location.Location).Distinct().Count() == 12 && SettlementServiceRules.Get(3).Services.Count == 5, "Every page 17 settlement location and its documented services must resolve.");
Assert(SettlementServiceRules.LodestoneCarverCoins(3) == 300 && SettlementServiceRules.ResolveRemedy(false, new ScriptedRandom(6)).Effect == "Recover 6 Grit." && SettlementServiceRules.ResolveRemedy(true, new ScriptedRandom(3)).Effect == "Recover 3 Flesh.", "Lodestone Carver value and Apothecary Remedy recovery must match the source.");
Assert(SettlementServiceRules.ResolveMalady(new ScriptedRandom(1)).Effect == "Blinded." && SettlementServiceRules.ResolveMalady(new ScriptedRandom(2)).Effect == "2d6 damage." && SettlementServiceRules.ResolveMalady(new ScriptedRandom(3)).Effect == "Paralysis for 1 hour.", "Apothecary Malady must resolve all three documented contact outcomes.");
Assert(Enum.GetValues<SettlementDenizen>().Select(SettlementDenizenRules.GetDenizen).All(rule => !string.IsNullOrWhiteSpace(rule.Offer) && !string.IsNullOrWhiteSpace(rule.Obligation)) && Enum.GetValues<SettlementFaction>().Select(SettlementDenizenRules.GetFaction).All(rule => !string.IsNullOrWhiteSpace(rule.TrainingRequirement)), "Every page 18 denizen and page 19 faction must retain its offer, obligation, ability, and trigger.");
var factionUses = new SettlementFactionUses();
Assert(factionUses.ProduceSeekerKeeperItem("rope").Contains("Poor rope") && ThrowsInvalidOperation(() => factionUses.ProduceSeekerKeeperItem("torch")), "Seeker Keeper Inscrutable Pockets must be limited to once per day.");
factionUses.ResetDay();
Assert(factionUses.ProduceSeekerKeeperItem("torch").Contains("Poor torch"), "Seeker Keeper Inscrutable Pockets must reset after rest.");
Assert(SettlementFactionService.CraftJarredFireHours(true, new ScriptedRandom(3)) == 3 && SettlementFactionService.BlackHelmMemoryLossBenefit(2, new ScriptedRandom(1, 6)) == (7, 2) && SettlementFactionService.GraftFromWillingHost(true, new ScriptedRandom(4)) == (4, 1), "Settlement faction abilities must preserve their source dice and resource conversions.");
var generatedRuin = RuinGenerationRules.Generate([3, 2, 4, 1, 5]);
Assert(generatedRuin.Rooms.Count == 15 && generatedRuin.Rooms.GroupBy(room => room.SourceFaceIndex).Select(group => group.Count()).SequenceEqual([3, 2, 4, 1, 5]), "Ruin generation must create room counts from each of the five visible d6 faces.");
Assert(generatedRuin.Passages.Count >= generatedRuin.Rooms.Count - 1 && IsConnected(generatedRuin), "Generated Ruin rooms must be connected by passages.");
Assert(RuinGenerationRules.RollAndGenerate(new ScriptedRandom(1, 2, 3, 4, 5)).VisibleFaces.SequenceEqual([1, 2, 3, 4, 5]), "Ruin generation must roll five visible d6 faces when they are not supplied.");
var exploration = new RuinExploration(RuinGenerationRules.Generate([2, 1, 1, 1, 1]), new GridCoord(0, 0));
Assert(!exploration.TryMoveToRoom(new GridCoord(99, 99)), "Ruin exploration must reject movement without a connecting passage.");
Assert(exploration.TryMoveToRoom(new GridCoord(1, 0)) && exploration.ElapsedMinutes == 10, "Moving room-to-room through a connecting passage must take 10 minutes and discover the room.");
exploration.TravelHallwayPoint();
exploration.SearchCurrentRoom();
Assert(exploration.ElapsedMinutes == 50 && exploration.SearchedRooms.Contains(exploration.CurrentRoom), "Hallway movement and room searching must use the documented 10/30-minute careful times.");
var deeperRuin = RuinGenerationRules.Generate([1, 1, 1, 1, 1]);
exploration.Descend(deeperRuin, new GridCoord(0, 0));
Assert(exploration.Depth == 2 && exploration.Layout == deeperRuin && exploration.VisitedRooms.SetEquals([new GridCoord(0, 0)]), "A depth passage must create and enter a new Ruin layout at the next depth.");
Assert(RuinRoomRegistry.Get(1, 1).Single().Name == "Plaza" && RuinRoomRegistry.Get(6, 6).Single().Name == "Observatory", "The Ruin room registry must resolve documented two-d6 endpoints.");
Assert(RuinRoomRegistry.Get(4, 5).Select(room => room.Name).SequenceEqual(["Reliquary", "Fountain"]) && RuinRoomRegistry.Get(3, 2).Count == 0, "The Ruin room registry must preserve the printed duplicate 45 and the absent 32 rather than inventing an outcome.");
Assert(RuinFeatureRules.Get(1).Single().Name == "Warning" && RuinFeatureRules.Get(31).Single().Name == "Entrance to the Deep" && RuinFeatureRules.Get(99).Single().Name == "Entrance to the Deep", "Ruin features must cover 1 through 31+ with depth-adjusted totals.");
Assert(RuinFeatureRules.Get(25).Select(feature => feature.Name).SequenceEqual(["Vein of Metal", "Hideout"]) && RuinFeatureRules.Get(24).Count == 0, "Ruin feature data must preserve the printed duplicate 25 and missing 24.");
var collapsedRuin = RuinCollapseService.CollapseRoom(generatedRuin, new GridCoord(0, 0));
Assert(collapsedRuin.Rooms.All(room => room.Coordinate != new GridCoord(0, 0)) && collapsedRuin.Passages.All(passage => passage.From != new GridCoord(0, 0) && passage.To != new GridCoord(0, 0)), "An Unstable room collapse must remove the room and all its pathways.");
Assert(Enumerable.Range(1, 6).Select(RuinDiscoveryRules.Get).Select(rule => rule.Name).Distinct().Count() == 6, "Every Ruin discovery d6 face must resolve to source content.");
var discoveries = new RuinDiscoveryTracker();
var discoveryRoom = new GridCoord(4, 4);
Assert(discoveries.EnterRoom(1, discoveryRoom, new ScriptedRandom(18)).Discovered == false && discoveries.EnterRoom(1, new GridCoord(5, 4), new ScriptedRandom(19, 5)) is { Discovered: true, CheckTotal: 20, Discovery: { Name: "Lost Habitation" } }, "Ruin Remnants must trigger at 20+ on 1d20 + depth.");
Assert(!discoveries.EnterRoom(1, discoveryRoom, new ScriptedRandom(20, 1)).Discovered, "A Ruin room must not resolve a discovery more than once.");
Assert(RuinEncounterRules.Get(1).Name == "Nothing" && RuinEncounterRules.Get(22).Name == "Wyrm" && RuinEncounterRules.Get(99).Name == "Wyrm", "Ruin encounter totals must cover the source 1d12 + depth table through 22+.");
Assert(RuinEncounterRules.GetMood("Bandits", 3).Name == "Tribute" && RuinEncounterRules.GetMood("Delvers", 1).Name == "Vicious" && RuinEncounterRules.GetMood("Delvers", 6).Name == "Helpful", "Ruin Bandit and Delver mood bands must match page 30.");
Assert(RuinEncounterRules.GetStatBlock("Bandits") == new RuinCreatureStatRule("Bandits", 2, 10, "Hide", "Attack as weapon; retreats at half health.") && RuinEncounterRules.GetStatBlock("Delvers").HitDice == 5, "Page 30 short stat blocks must retain their combat values.");
Assert(RuinTreasureRules.GetMagnitude(1) == RuinTreasureMagnitude.Useful && RuinTreasureRules.GetMagnitude(11) == RuinTreasureMagnitude.Special && RuinTreasureRules.GetMagnitude(20) == RuinTreasureMagnitude.GreatAndTerrible, "Ruin treasure magnitude must follow 1d20 + depth bands.");
Assert(Enumerable.Range(1,12).Select(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.Useful,roll)).Select(rule => rule.Name).Distinct().Count() == 12 && Enumerable.Range(1,12).Select(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.Special,roll)).Select(rule => rule.Name).Distinct().Count() == 12 && Enumerable.Range(1,10).Select(roll => RuinTreasureRules.Get(RuinTreasureMagnitude.GreatAndTerrible,roll)).Select(rule => rule.Name).Distinct().Count() == 10, "Every page 31 treasure entry must be uniquely represented in its category.");
Assert(RuinTreasureRules.Get(RuinTreasureMagnitude.Useful, 2).Effect.Contains("Once daily") && RuinTreasureRules.Get(RuinTreasureMagnitude.GreatAndTerrible, 9).Effect.Contains("Grit 0"), "Treasure data must retain source-defined persistent constraints and costs.");
Assert(Enumerable.Range(1,10).Select(DeepGiftRules.Get).Select(rule => rule.Gift).Distinct().Count() == 10 && DeepGiftRules.Get(3).OncePerDay && DeepGiftRules.Get(6).OncePerDay, "Every Gift of the Deep d10 face and its daily limit must match page 32.");
var deepGifts = new DeepGiftState();
Assert(deepGifts.GainOnEnterOrNewLevel(new ScriptedRandom(3)) == DeepGift.GraftedLimbs && !deepGifts.TryUse(DeepGift.VoidOfPresence) && deepGifts.TryUse(DeepGift.GraftedLimbs) && !deepGifts.TryUse(DeepGift.GraftedLimbs), "Entering the Deep/new level must grant a gift and daily use must require ownership and remain limited.");
deepGifts.ResetDay();
Assert(deepGifts.TryUse(DeepGift.GraftedLimbs), "Gift daily limits must reset on a new day.");
Assert(MinotaurRules.StatBlock == new MinotaurRule(20, "Cannot be harmed", "Half Standard", "Touch of the Minotaur"), "The Minotaur stat block must retain source combat constraints.");
var minotaur = new MinotaurPursuit();
Assert(!minotaur.EnterDeepArea(new ScriptedRandom(2)) && minotaur.EnterDeepArea(new ScriptedRandom(1)) && minotaur.HasArrived, "The Minotaur must arrive on a 1-in-6 check when entering a Deep room or area and persist afterward.");
Assert(MinotaurRules.ResolveTouch(1, new ScriptedRandom(6)) == new MinotaurTouchResult(MinotaurTouchEffect.WitherTools, 6) && MinotaurRules.ResolveTouch(2, new ScriptedRandom(6,6,6)) == new MinotaurTouchResult(MinotaurTouchEffect.ErodeBody,18) && MinotaurRules.ResolveTouch(4, new ScriptedRandom(6)) == new MinotaurTouchResult(MinotaurTouchEffect.BreakSpirit,6) && MinotaurRules.ResolveTouch(5, new ScriptedRandom(3)) == new MinotaurTouchResult(MinotaurTouchEffect.DrinkFlesh,3,true) && MinotaurRules.ResolveTouch(6, new ScriptedRandom()).MemoryLost, "Every Touch of the Minotaur d6 branch must resolve with its source quantity and permanence.");
Assert(Enum.GetValues<DeepTrial>().Select(DeepTrialRules.Get).All(rule => !string.IsNullOrWhiteSpace(rule.Danger) && !string.IsNullOrWhiteSpace(rule.WayOut)), "Every pages 34-35 Deep trial must retain its danger and exit procedure.");
var scaleTrial = new DeepTrialState(DeepTrial.Scale); scaleTrial.EnterUnexploredScaleRoom(); scaleTrial.EnterUnexploredScaleRoom();
Assert(scaleTrial.ScaleReturnDistance == 4 && !scaleTrial.ReturnToScaleOrigin(3) && scaleTrial.ReturnToScaleOrigin(4), "Scale must double backtrack distance for each unexplored room and exit at the original entry.");
var repetitionTrial = new DeepTrialState(DeepTrial.Repetition); repetitionTrial.EnterReflectionRoom();
var changeTrial = new DeepTrialState(DeepTrial.Change); changeTrial.EnterChangeRoom(); changeTrial.EnterChangeRoom();
Assert(repetitionTrial.SimulacraActive && changeTrial.ChangeRotationDegrees == 180 && changeTrial.ReachChangeCenter(true), "Repetition reflection rooms must activate Simulacra and Change must rotate 90 degrees per entry until the center exit.");
var emptinessTrial = new DeepTrialState(DeepTrial.Emptiness);
Assert(!emptinessTrial.TraverseEmptiness(101, false) && emptinessTrial.TraverseEmptiness(100, false) && emptinessTrial.TraverseEmptiness(900, true) && emptinessTrial.ExitOpen, "Emptiness must require safe 100-foot slab hops or rigging and open its exit at 1000 feet.");
Assert(!new DeepTrialState(DeepTrial.Sacrifice).ResolveSacrifice(false) && new DeepTrialState(DeepTrial.Sacrifice).ResolveSacrifice(true), "Sacrifice must keep its exit open only while a mortal remains behind.");
var liesTrial = new DeepTrialState(DeepTrial.Lies); liesTrial.EnterLiesLight(); liesTrial.DieInFalseWorld();
Assert(!liesTrial.InFalseWorld && liesTrial.ReawakenedInLiesRoom, "Lies must reawaken a Traveler in the trial room when they die in its false world.");
var escapeRitual = new EscapeTheVastRitual();
AssertThrows(() => escapeRitual.FallForward(), "Leaving the Vast must reject an incomplete ritual.");
escapeRitual.SeekLushMendedFamiliar(); escapeRitual.SearchCannyClearBenign(); escapeRitual.HideSpotOutsideSenses(); escapeRitual.SpeakWishToLeaveTheVast();
Assert(escapeRitual.FallForward() == VastTerminalOutcome.EscapedHome, "The complete page 37 ritual must return the Traveler home.");
var rites = new RiteLedger();
Assert(rites.GainFromMotion("Pillar-1") && !rites.GainFromMotion("Pillar-1"), "Motions of the Labyrinth must grant a Rite only on first Pillar entry or Ruin descent.");
rites.GainFromShunningLight(true); rites.GainFromEmbracingDark(true); rites.GainFromErosionOfSelf();
Assert(rites.Rites == 4 && rites.LockedErosionExhaustion == 1, "All page 38 Rite gain methods must add one Rite and Erosion must lock its exhaustion.");
Assert(rites.TrySpendToCast() && rites.Rites == 3 && rites.LockedErosionExhaustion == 0, "Casting must cost one Rite and release one Erosion exhaustion lock when spent.");
Assert(Enum.GetValues<RiteSpell>().Select(RiteSpellRules.Get).Count() == 15 && RiteSpellRules.Get(RiteSpell.Cinderhowl).CostsRite == false && RiteSpellRules.Get(RiteSpell.Mazewalk).CostsRite, "Every page 38-39 rite must retain its school and Rite-versus-body cost.");
Assert(RiteSpellRules.FickleDescentLevels(3, 2, coinFace: true) == 2 && RiteSpellRules.FickleDescentLevels(3, 2, coinFace: false) == -2 && RiteSpellRules.SunderDurationSeconds(3) == 18, "Fickle Descent and Sunder to Dust must use their source level scaling.");
Assert(RiteSpellRules.SparkGritCost(true, new ScriptedRandom(6)) == 1 && RiteSpellRules.SparkGritCost(false, new ScriptedRandom(6)) == 6 && RiteSpellRules.CinderhowlAlertsEncounter(new ScriptedRandom(1)) && !RiteSpellRules.CinderhowlAlertsEncounter(new ScriptedRandom(2)), "Sparks must use the specified body costs and Cinderhowl encounter chance.");
Assert(RiteSpellRules.LikeClay(new AbilityScores(3,4,5,6,7,8), Ability.Strength, Ability.Charisma) == new AbilityScores(8,4,5,6,7,3), "Like Clay must swap exactly two ability scores.");
Assert(Enum.GetValues<CrawlCreature>().Select(CrawlCreatureRules.Get).All(rule => rule.HitDice > 0 && rule.HitPoints > 0) && CrawlCreatureRules.Get(CrawlCreature.Wyrm).HitPoints == 150, "Every Crawl stat block must preserve source HD, HP, attack, defense, and special data.");
Assert(CrawlCreatureRules.CyclopsCallsAlly(new ScriptedRandom(1)) && !CrawlCreatureRules.CyclopsCallsAlly(new ScriptedRandom(2)) && CrawlCreatureRules.MedusaScream(false).Stunned && CrawlCreatureRules.MedusaScream(true).FutureSaveAdvantage, "Cyclops Call and Medusa Scream must preserve their chance and save branches.");
Assert(CrawlCreatureRules.HarpyMeld(false,false,false,new ScriptedRandom(6,3)) == (true,6,3) && !CrawlCreatureRules.HarpyMeld(true,false,false,new ScriptedRandom()).Adheres && CrawlCreatureRules.GriffonSwallows(15,false), "Harpy Meld protections and Griffon Devour threshold must match the source.");
Assert(CrawlCreatureRules.HydraFrenzyAttacks(new ScriptedRandom(1,6)) == 6 && CrawlCreatureRules.HydraVenom(false,new ScriptedRandom(3,2)) == (3,2) && CrawlCreatureRules.ShadeEyeBite(new ScriptedRandom(1,3)) == (true,3), "Hydra and Shade special branches must retain source dice and saves.");
Assert(CrawlCreatureRules.OgreSpawnCount(10,new ScriptedRandom(6)) == 6 && CrawlCreatureRules.OgreSpawnCount(11,new ScriptedRandom(6)) == 0 && CrawlCreatureRules.WyrmHowl(false,new ScriptedRandom(6,6,6,6)) == (18,true,6) && CrawlCreatureRules.WyrmHowl(true,new ScriptedRandom(6,6,6)) == (9,false,0), "Ogre Split and Wyrm Howl must preserve their source thresholds, damage, and save result.");
var collisionState = local.ToState() with
{
    RoamingHazards = [
        new RoamingHazardState(HexCoord.Zero.Q, HexCoord.Zero.R, 1),
        new RoamingHazardState(HexCoord.Zero.Neighbour(0).Q, HexCoord.Zero.Neighbour(0).R, 2),
    ],
};
var collisionMap = new LocalMap(collisionState);
collisionMap.AdvanceRoamingHazards(new ScriptedSystemRandom(0, 0, 0));
Assert(collisionMap.RoamingHazards.Count == 2 && collisionMap.RoamingHazards.Keys.Distinct().Count() == 2, "A roaming-hazard collision must re-drop rather than merge hazards.");

var noAssetNavigation = DailyNavigationService.Resolve([], new ScriptedRandom(1));
Assert(noAssetNavigation.IsLost && noAssetNavigation.Effect == LostEffect.UtterlyLost && noAssetNavigation.RequiresRepeatedNavigation, "A roll of 1 without navigation assets must leave the party utterly lost.");
var dangerousNavigation = DailyNavigationService.Resolve([], new ScriptedRandom(2));
Assert(dangerousNavigation.Effect == LostEffect.DangerouslyOffCourse && dangerousNavigation.DistanceMiles == 12, "A roll of 2 without assets must leave the party dangerously off course by 12 miles.");
var offCourseNavigation = DailyNavigationService.Resolve([], new ScriptedRandom(3));
Assert(offCourseNavigation.Effect == LostEffect.OffCourse && offCourseNavigation.DistanceMiles == 6, "A roll of 3 without assets must leave the party off course by 6 miles.");
var lateNavigation = DailyNavigationService.Resolve([], new ScriptedRandom(4));
Assert(lateNavigation.Effect == LostEffect.Late && lateNavigation.DistanceMiles == 6, "A roll of 4 without assets must leave the party late by 6 miles.");
Assert(!DailyNavigationService.Resolve([], new ScriptedRandom(6)).IsLost, "A roll of 6 must navigate successfully without assets.");
var preparedNavigation = DailyNavigationService.Resolve([NavigationAsset.Landmark, NavigationAsset.Directions, NavigationAsset.Tool, NavigationAsset.Light], new ScriptedRandom(1));
Assert(preparedNavigation.LostChanceInSix == 1 && preparedNavigation.Effect == LostEffect.Late, "Four distinct navigation assets must reduce the remaining failure to the Late effect shown in the source example.");
Assert(!DailyNavigationService.Resolve([NavigationAsset.Landmark, NavigationAsset.Directions, NavigationAsset.Tool, NavigationAsset.Light, NavigationAsset.DeadReckoning], new ScriptedRandom(1)).IsLost, "Five distinct navigation assets must reduce the lost chance to zero.");
Assert(DailyNavigationService.Resolve([NavigationAsset.Light, NavigationAsset.Light], new ScriptedRandom(4)).IsLost, "Duplicate navigation assets must not reduce the lost chance more than once.");

var clearWeather = new TravelEventTable([new TravelEventDefinition("Clear skies")]);
var quietEncounter = new TravelEventTable([new TravelEventDefinition("No encounter")]);
var unusedDeck = new WastesDeck([
    new WastesCard("Unused", CreateWastesOutcomes(new WastesEntry("Unused", []))),
]);
var travelWorld = new TravelWorld(
    unusedDeck,
    new Dictionary<Terrain, TerrainTravelProfile>
    {
        [Terrain.Wastes] = new TerrainTravelProfile(clearWeather, quietEncounter),
    },
    TravelEventCadence.PerTravelPeriod);
var alice = new Traveler("Alice", rations: 1);
var bob = new Traveler("Bob");
var party = new TravelParty([alice, bob]);
var travel = new TravelService().TravelDay(
    party,
    [new TravelSegment(Terrain.Wastes, 30)],
    travelWorld,
    forcedMarchLevels: 1,
    new SystemRandomSource(new Random(67890)));
Assert(travel.MilesTravelled == 24, "A party must travel 18 miles plus 6 miles per forced-march exhaustion level.");
Assert(alice.Rations == 0 && alice.Exhaustion == 1, "A fed traveler must spend one ration and gain forced-march exhaustion.");
Assert(bob.Exhaustion == 2, "An unfed traveler must gain exhaustion for starvation and forced marching.");
Assert(travel.Events.Count == 4 && travel.Events.All(@event => @event.Terrain == Terrain.Wastes), "Weather and encounters must resolve from the terrain profile for each travel period.");
Assert(travel.RestRequired && party.MustStopTraveling, "A travel day must require rest after movement ends.");

var dangerousOutcomes = CreateWastesOutcomes(new WastesEntry("Quiet road", []));
dangerousOutcomes[18] = new WastesEntry("Raider Ambush", [
    new EffectsStep([new AddConditionEffect("Shaken")]),
    new CombatStep("Raiders"),
]);
var dangerousCard = new WastesCard("Scorched Road", dangerousOutcomes);
var wastesWorld = new TravelWorld(
    new WastesDeck([dangerousCard]),
    new Dictionary<Terrain, TerrainTravelProfile>());
var wastesParty = new TravelParty([new Traveler("Scout")]);
var wastesResult = new WastesService().ResolveWastesEncounter(
    wastesParty,
    wastesWorld,
    new ScriptedRandom(0, 12, 6));
Assert(wastesResult.RollTotal == 18, "Wastes cards must use 1d12 + 1d6, with a maximum total of 18.");
Assert(wastesResult.CombatEnemyGroup == "Raiders", "Combat steps must expose the enemy group to the caller.");
Assert(wastesParty.Members[0].Conditions.Contains("Shaken"), "Wastes effects must be committed after the entry resolves.");

var movementCampaign = new Campaign(new Random(76543));
var earlyRestCampaign = new Campaign(new Random(4242));
Assert(earlyRestCampaign.TryRestParty().Moved && earlyRestCampaign.PartyTravel.Day == 2 && earlyRestCampaign.PartyTravel.DailyMiles == 0 && earlyRestCampaign.Party.TotalExhaustion == 1, "An early rest must begin a new 18-mile day and add hunger exhaustion when no ration is consumed.");
var partyRegionalCoordinate = movementCampaign.PartyTravel.RegionalCoordinate;
var partyMap = movementCampaign.GetLocalMap(partyRegionalCoordinate);
var firstStep = HexCoord.Zero.Neighbour(0);
Assert(partyMap.VisibleCells.Contains(firstStep), "The party movement test must use a visible local hex.");
for (var step = 0; step < PartyTravelState.NormalDailyMiles; step++)
{
    var target = step % 2 == 0 ? firstStep : HexCoord.Zero;
    var move = movementCampaign.TryMoveParty(partyRegionalCoordinate, target);
    Assert(move.Moved, "The party must be able to move one adjacent local hex per mile.");
}

Assert(movementCampaign.PartyTravel.DailyMiles == 18 && movementCampaign.PartyTravel.RestRequired, "The party must require rest after 18 local miles.");
Assert(!movementCampaign.TryMoveParty(partyRegionalCoordinate, firstStep).Moved, "The party cannot move after 18 miles without forced marching or resting.");
var forcedMarch = movementCampaign.TryBeginForcedMarch();
Assert(forcedMarch.Moved && movementCampaign.PartyTravel.DailyMileLimit == 24 && movementCampaign.Party.TotalExhaustion == 1, "Forced march must add one exhaustion and six more miles of travel.");
for (var step = 0; step < PartyTravelState.ForcedMarchMiles; step++)
{
    var target = step % 2 == 0 ? firstStep : HexCoord.Zero;
    Assert(movementCampaign.TryMoveParty(partyRegionalCoordinate, target).Moved, "Forced march must permit six additional local moves.");
}

Assert(movementCampaign.PartyTravel.DailyMiles == 24 && movementCampaign.PartyTravel.RestRequired, "The party must require rest after its forced march allowance is used.");
movementCampaign.Party.Members[0].AddRations(1);
var hazardDayBeforeRest = movementCampaign.GetLocalMap(partyRegionalCoordinate).RoamingHazardDay;
Assert(movementCampaign.TryRestParty().Moved && movementCampaign.PartyTravel.DailyMiles == 0 && movementCampaign.PartyTravel.Day == 2 && movementCampaign.Party.TotalRations == 0, "Rest must consume one ration, begin a new travel day, and reset daily miles.");
Assert(movementCampaign.GetLocalMap(partyRegionalCoordinate).RoamingHazardDay == hazardDayBeforeRest + 1, "Rest must advance roaming hazards in the party's local chunk.");
var exhaustionBeforeUnfedRest = movementCampaign.Party.TotalExhaustion;
Assert(movementCampaign.TryRestParty().Moved && movementCampaign.Party.TotalExhaustion == exhaustionBeforeUnfedRest + 1, "A hungry full-day rest must recover prior exhaustion but still add one exhaustion for the missed ration.");
Assert(movementCampaign.TravelLog.Count >= 3 && movementCampaign.TravelLog.Last().Message.Contains("Day 3 begins", StringComparison.Ordinal), "Successful travel actions must append a persisted travel log entry.");

var boundaryCampaign = new Campaign(new Random(97531));
var boundaryOrigin = boundaryCampaign.PartyTravel.RegionalCoordinate;
var boundaryPath = FindBoundaryPath(boundaryCampaign.GetLocalMap(boundaryOrigin), boundaryCampaign.Regional, boundaryOrigin);
foreach (var coordinate in boundaryPath.Skip(1))
{
    Assert(boundaryCampaign.TryMoveParty(boundaryOrigin, coordinate).Moved, "The party must be able to walk to a local-map boundary.");
}

var boundaryExit = boundaryCampaign.GetAvailablePartyMoves().First(move => move.RegionalCoordinate != boundaryOrigin);
Assert(boundaryCampaign.TryMoveParty(boundaryExit.RegionalCoordinate, boundaryExit.LocalCoordinate).Moved, "The party must be able to cross a local-map edge into an adjacent regional chunk.");
Assert(boundaryCampaign.PartyTravel.RegionalCoordinate == boundaryExit.RegionalCoordinate &&
       boundaryCampaign.PartyTravel.LocalCoordinate == boundaryExit.LocalCoordinate,
    "A boundary crossing must update the party's regional and local coordinates.");

var ruinsLocal = new LocalMap(new RegionalCoord(0, 0), Terrain.Ruins, new Random(23456));
Assert(ruinsLocal.DiceCount is 6 or 12 or 32, "Local density must choose 6, 12, or 32 dice.");
Assert(ruinsLocal.DiceRolls.Count == ruinsLocal.DiceCount, "Local dice must occupy distinct hexes.");
Assert(ruinsLocal.DiceRolls.Keys.All(ruinsLocal.VisibleCells.Contains), "Terrain dice must only occupy fully visible local cells.");
foreach (var (coordinate, roll) in ruinsLocal.DiceRolls)
{
    var expected = roll switch { 1 => Terrain.Wastes, <= 4 => Terrain.Ruins, _ => Terrain.Settlements };
    Assert(ruinsLocal.GetTerrain(coordinate) == expected, "Ruins local table must match its d6 result.");
}

var wastesLocal = new LocalMap(new RegionalCoord(0, 0), Terrain.Wastes, new Random(34567));
foreach (var (coordinate, roll) in wastesLocal.DiceRolls)
{
    Assert(wastesLocal.GetTerrain(coordinate) == (roll == 6 ? Terrain.Ruins : Terrain.Wastes), "Wastes local table must only produce ruins on a 6.");
}

var pillarsLocal = new LocalMap(new RegionalCoord(0, 0), Terrain.Pillars, new Random(45678));
Assert(pillarsLocal.DiceRolls.Count == pillarsLocal.DiceCount, "Pillars local maps must still place the density roll's dice.");
Assert(pillarsLocal.TerrainByCell.Values.All(terrain => terrain == Terrain.Pillars), "Pillars local maps must remain entirely pillar structures.");

var savePath = Path.Combine(Path.GetTempPath(), $"vastdark-{Guid.NewGuid():N}.json");
try
{
    var generatedCampaign = new Campaign(new Random(56789));
    var generatedLocal = generatedCampaign.GetLocalMap(new RegionalCoord(2, 2));
    var generatedTraveler = generatedCampaign.Party.Members[0];
    generatedTraveler.AddRations(2);
    generatedTraveler.SetSkill("Survival", 3);
    generatedTraveler.SetResource("Water", 4);
    generatedTraveler.AddCondition("Irradiated");
    CampaignFile.Save(generatedCampaign, savePath);
    var loadedCampaign = CampaignFile.LoadOrCreate(savePath);
    Assert(loadedCampaign.Regional.DiceRolls.Count == RegionalMap.DiceCount, "Saved regional dice must reload.");
    foreach (var coordinate in generatedCampaign.Regional.Cells)
    {
        Assert(loadedCampaign.Regional.GetTerrain(coordinate) == generatedCampaign.Regional.GetTerrain(coordinate), "Saved regional terrain must reload exactly.");
    }

    var loadedLocal = loadedCampaign.GetLocalMap(new RegionalCoord(2, 2));
    Assert(loadedLocal.DensityRoll == generatedLocal.DensityRoll && loadedLocal.DiceCount == generatedLocal.DiceCount, "Saved local density must reload exactly.");
    foreach (var coordinate in generatedLocal.Cells)
    {
        Assert(loadedLocal.GetTerrain(coordinate) == generatedLocal.GetTerrain(coordinate), "Saved local terrain must reload exactly.");
    }

    Assert(loadedLocal.RoamingHazardDay == generatedLocal.RoamingHazardDay, "Saved roaming hazard day must reload exactly.");
    Assert(loadedLocal.RoamingHazards.OrderBy(hazard => hazard.Key.Q).ThenBy(hazard => hazard.Key.R)
        .SequenceEqual(generatedLocal.RoamingHazards.OrderBy(hazard => hazard.Key.Q).ThenBy(hazard => hazard.Key.R)), "Saved roaming hazards must reload exactly.");
    Assert(loadedCampaign.PartyTravel.RegionalCoordinate == generatedCampaign.PartyTravel.RegionalCoordinate &&
           loadedCampaign.PartyTravel.LocalCoordinate == generatedCampaign.PartyTravel.LocalCoordinate &&
           loadedCampaign.PartyTravel.DailyMiles == generatedCampaign.PartyTravel.DailyMiles,
        "Saved party travel state must reload exactly.");
    Assert(loadedCampaign.Party.Members.Select(member => member.Name).SequenceEqual(generatedCampaign.Party.Members.Select(member => member.Name)),
        "Saved party members must reload exactly.");
    var loadedTraveler = loadedCampaign.Party.Members[0];
    Assert(loadedTraveler.Rations == 2 && loadedTraveler.GetSkill("Survival") == 3 && loadedTraveler.GetResource("Water") == 4 && loadedTraveler.Conditions.Contains("Irradiated"),
        "Saved party supplies, skills, resources, and conditions must reload exactly.");
    Assert(loadedCampaign.TravelLog.SequenceEqual(generatedCampaign.TravelLog), "Saved travel log entries must reload exactly.");
}
finally
{
    if (File.Exists(savePath))
    {
        File.Delete(savePath);
    }
}

var dungeon = PrototypeDungeonBuilder.CreateSixLevelDungeon();
Assert(dungeon.Levels.Count == 6, "Prototype dungeon must expose six levels.");
Assert(dungeon.GetLevel(5).Width == 32 && dungeon.GetLevel(5).Height == 24, "Dungeon grid dimensions changed unexpectedly.");
Assert(dungeon.GetLevel(0).GetTile(new GridCoord(20, 8)) == DungeonTile.StairDown, "Level zero must descend.");
Assert(dungeon.GetLevel(5).GetTile(new GridCoord(6, 6)) == DungeonTile.StairUp, "Bottom level must ascend.");

var navigationCampaign = new Campaign(new Random(24680));
var navigationState = navigationCampaign.ToState() with
{
    PartyTravel = new PartyTravelStateState(
        Campaign.DungeonRegionalCoordinate.Column,
        Campaign.DungeonRegionalCoordinate.Row,
        Campaign.DungeonLocalCoordinate.Q,
        Campaign.DungeonLocalCoordinate.R,
        Day: 1,
        DailyMiles: 0,
        Exhaustion: 0,
        ForcedMarchUsed: false),
};
var navigation = new MapNavigationService(new Campaign(navigationState));
navigation.SelectRegional(Campaign.DungeonRegionalCoordinate);
navigation.EnterLocal(Campaign.DungeonRegionalCoordinate);
Assert(navigation.TryEnterDungeon(), "Dungeon entrance must be reachable from its local map.");
navigation.SetDungeonDepth(5);
Assert(navigation.Current is MapLocation.Dungeon { Depth: 5 }, "Dungeon depth selection failed.");
navigation.ReturnToLocal();
Assert(navigation.Current is MapLocation.Local, "Dungeon must return to its local map.");

Console.WriteLine("All VastDark domain checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static bool ThrowsInvalidOperation(Action action)
{
    try { action(); }
    catch (InvalidOperationException) { return true; }
    return false;
}

static bool IsConnected(GeneratedRuin ruin)
{
    var adjacent = ruin.Passages
        .SelectMany(passage => new[] { (From: passage.From, To: passage.To), (From: passage.To, To: passage.From) })
        .GroupBy(edge => edge.From)
        .ToDictionary(group => group.Key, group => group.Select(edge => edge.To));
    var visited = new HashSet<GridCoord> { ruin.Rooms[0].Coordinate };
    var pending = new Queue<GridCoord>(visited);
    while (pending.TryDequeue(out var current))
    {
        if (!adjacent.TryGetValue(current, out var neighbours)) continue;
        foreach (var neighbour in neighbours.Where(visited.Add)) pending.Enqueue(neighbour);
    }

    return visited.Count == ruin.Rooms.Select(room => room.Coordinate).Distinct().Count();
}

static Dictionary<int, WastesEntry> CreateWastesOutcomes(WastesEntry entry) =>
    Enumerable.Range(2, 17).ToDictionary(total => total, _ => entry);

static IReadOnlyList<HexCoord> FindBoundaryPath(LocalMap map, RegionalMap regional, RegionalCoord origin)
{
    var visited = new HashSet<HexCoord> { HexCoord.Zero };
    var previous = new Dictionary<HexCoord, HexCoord>();
    var queue = new Queue<HexCoord>();
    queue.Enqueue(HexCoord.Zero);

    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
        {
            if (!map.VisibleCells.Contains(current.Neighbour(direction)) && regional.GetNeighbour(origin, direction) is not null)
            {
                var path = new List<HexCoord> { current };
                while (path[^1] != HexCoord.Zero)
                {
                    path.Add(previous[path[^1]]);
                }

                path.Reverse();
                return path;
            }
        }

        for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
        {
            var next = current.Neighbour(direction);
            if (map.VisibleCells.Contains(next) && visited.Add(next))
            {
                previous.Add(next, current);
                queue.Enqueue(next);
            }
        }
    }

    throw new InvalidOperationException("No reachable local-map boundary was found.");
}

sealed class ScriptedRandom(params int[] values) : IRandomSource
{
    private readonly Queue<int> _values = new(values);

    public int Next(int minInclusive, int maxExclusive)
    {
        if (_values.Count == 0)
        {
            throw new InvalidOperationException("The test did not provide a scripted random value.");
        }

        var value = _values.Dequeue();
        if (value < minInclusive || value >= maxExclusive)
        {
            throw new InvalidOperationException($"Scripted random value {value} is outside [{minInclusive}, {maxExclusive}).");
        }

        return value;
    }
}

sealed class ScriptedSystemRandom(params int[] values) : Random
{
    private readonly Queue<int> _values = new(values);

    public override int Next(int maxValue) => Next(0, maxValue);

    public override int Next(int minValue, int maxValue)
    {
        if (_values.Count == 0)
        {
            throw new InvalidOperationException("The test did not provide a scripted random value.");
        }

        var value = _values.Dequeue();
        if (value < minValue || value >= maxValue)
        {
            throw new InvalidOperationException($"Scripted random value {value} is outside [{minValue}, {maxValue}).");
        }

        return value;
    }
}
