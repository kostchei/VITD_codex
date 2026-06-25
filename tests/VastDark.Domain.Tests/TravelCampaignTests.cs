using VastDark.Domain;
using static TestKit;

internal static class TravelCampaignTests
{
    public static void Run()
    {
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

        var damageTraveler = new Traveler("Hazard A", health: 100);
        var damageParty = new TravelParty([damageTraveler]);
        var hazardResolution = TravelInterruptionResolver.Resolve(
            new TravelInterruption(TravelInterruptionKind.RoamingHazard, Terrain.Wastes, HazardDieRoll: 2),
            damageParty,
            new ScriptedRandom(0, 20, 20, 20));
        Assert(damageTraveler.Health == 40 && hazardResolution.AppliedDamage!.Single().Amount == 60, "The interruption shell must apply deterministic roaming-hazard damage to party state.");

        var movementCampaign = new Campaign(new Random(76543));
        var interruptionCampaign = new Campaign(new Random(24680));
        var interruptionRegion = interruptionCampaign.PartyTravel.RegionalCoordinate;
        var interruptionMap = interruptionCampaign.GetLocalMap(interruptionRegion);
        var hazardCoordinate = interruptionMap.RoamingHazards.Keys.First();
        Assert(interruptionCampaign.GetTravelInterruption(interruptionRegion, hazardCoordinate) is { Kind: TravelInterruptionKind.RoamingHazard }, "Entering a hex with a roaming hazard must interrupt travel before terrain resolution.");
        var resolvedLogCount = interruptionCampaign.TravelLog.Count;
        var settlementResolution = interruptionCampaign.ResolveTravelInterruption(new TravelInterruption(TravelInterruptionKind.Settlement, Terrain.Settlements));
        Assert(settlementResolution.Log.Single().Contains("Settlement", StringComparison.Ordinal) && interruptionCampaign.TravelLog.Count == resolvedLogCount + 1, "Campaign interruption resolution must log player-facing resolution shell output.");
        var settlementActionCampaign = CreateCampaignAtTerrain(
            Terrain.Settlements,
            new Traveler("Trader", abilityScores: new AbilityScores(10, 10, 14, 10, 10, 10)));
        settlementActionCampaign.Party.Members[0].SetResource(Campaign.CoinResource, 30);
        var rationPurchase = settlementActionCampaign.TryBuyRationsAtSettlement(2);
        Assert(rationPurchase.Applied && settlementActionCampaign.Party.TotalRations == 2 && settlementActionCampaign.PartyCoins == 10, "Settlement ration purchase must spend party coins and add rations through the campaign action shell.");
        settlementActionCampaign.Party.Members[0].Inventory.RecordOwnItem(new InventoryItem(Campaign.RawLodestoneItem, 1));
        var refined = settlementActionCampaign.TryRefineRawLodestoneAtSettlement();
        Assert(refined.Applied && settlementActionCampaign.PartyRawLodestone == 0 && settlementActionCampaign.PartyCoins > 10, "Settlement refinement must remove Raw Lodestone inventory and add coins.");

        var pillarActionCampaign = CreateCampaignAtTerrain(
            Terrain.Pillars,
            new Traveler(
                "Miner",
                abilityScores: new AbilityScores(10, 10, 14, 10, 10, 10),
                rules: new TravelerRulesState(Items: [new InventoryItemState("Mining Tools", 1, false)])));
        var mining = pillarActionCampaign.TryWorkPillar(PillarWork.Mining);
        Assert(mining.Applied && pillarActionCampaign.PartyRawLodestone == 1, "Pillar mining must use the existing PillarMiningService and record collected Raw Lodestone.");
        var enterPillar = pillarActionCampaign.TryEnterPillarDelve();
        var restoredPillarDelve = new Campaign(pillarActionCampaign.ToState());
        var restoredInPillarDelve = restoredPillarDelve.IsInPillarDelve;
        var searchPillar = restoredPillarDelve.TrySearchPillarTunnel();
        var exitPillar = restoredPillarDelve.TryExitPillarDelve();
        Assert(enterPillar.Applied && restoredInPillarDelve && searchPillar.Applied && exitPillar.Applied && !restoredPillarDelve.IsInPillarDelve, "Pillar delve controls must persist active delve state, drive the existing PillarDelve shell, and exit cleanly.");
        var terrainStop = interruptionMap.VisibleCells.FirstOrDefault(cell => interruptionMap.GetTerrain(cell) is Terrain.Ruins or Terrain.Settlements);
        if (interruptionMap.VisibleCells.Contains(terrainStop) && !interruptionMap.RoamingHazards.ContainsKey(terrainStop))
        {
            Assert(interruptionCampaign.GetTravelInterruption(interruptionRegion, terrainStop) is { Kind: TravelInterruptionKind.Ruins or TravelInterruptionKind.Settlement }, "Entering Ruins or a Settlement must interrupt travel.");
        }
        var regionalTravelCampaign = new Campaign(new Random(8675309));
        var regionalOrigin = regionalTravelCampaign.PartyTravel.RegionalCoordinate;
        var regionalDestination = regionalTravelCampaign.Regional.GetNeighbour(regionalOrigin, 0)!.Value;
        Assert(regionalTravelCampaign.TryTravelRegionalStep(regionalDestination) is { Moved: true, DailyMiles: 6 } && regionalTravelCampaign.PartyTravel.RegionalCoordinate == regionalDestination, "Regional path steps must move the party one adjacent regional hex and consume six local miles.");
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
        var hungerBeforeUnfedRest = movementCampaign.Party.Members[0].ExhaustionSources.Count(source => source == ExhaustionSource.Hunger);
        Assert(movementCampaign.TryRestParty().Moved && movementCampaign.Party.Members[0].ExhaustionSources.Count(source => source == ExhaustionSource.Hunger) == hungerBeforeUnfedRest + 1, "A hungry full-day rest must add hunger exhaustion for the missed ration.");
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

    }

    private static Campaign CreateCampaignAtTerrain(Terrain terrain, Traveler traveler)
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var campaign = new Campaign(new Random(seed));
            foreach (var region in campaign.Regional.Cells)
            {
                var localMap = campaign.GetLocalMap(region);
                foreach (var cell in localMap.VisibleCells)
                {
                    if (localMap.GetTerrain(cell) != terrain)
                    {
                        continue;
                    }

                    // Settlements now carry generated scarcity; the purchase-shell test needs a buyable (Middling) market.
                    if (terrain == Terrain.Settlements && campaign.GetSettlement(region, cell).Scarcity != SettlementScarcity.Middling)
                    {
                        continue;
                    }

                    var state = campaign.ToState() with
                    {
                        Party = new PartyState([traveler.ToState()]),
                        PartyTravel = new PartyTravelStateState(
                            region.Column,
                            region.Row,
                            cell.Q,
                            cell.R,
                            Day: 1,
                            DailyMiles: 0,
                            Exhaustion: 0,
                            ForcedMarchUsed: false),
                    };
                    return new Campaign(state);
                }
            }
        }

        throw new InvalidOperationException($"Could not find a generated {terrain} local cell for the campaign action tests.");
    }
}
