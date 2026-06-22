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
Assert(LocalMap.GetRoamingHazardName(4) == "Void Lightning", "Hazard names must match their d6 result.");

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
Assert(movementCampaign.TryRestParty().Moved && movementCampaign.Party.TotalExhaustion == exhaustionBeforeUnfedRest + 1, "Rest without a ration must add one exhaustion level.");
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

var navigation = new MapNavigationService(new Campaign());
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
