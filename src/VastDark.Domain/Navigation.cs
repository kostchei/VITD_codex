namespace VastDark.Domain;

public abstract record MapLocation
{
    private MapLocation()
    {
    }

    public sealed record Regional(RegionalCoord Coordinate) : MapLocation;
    public sealed record Local(RegionalCoord RegionalCoordinate) : MapLocation;
    public sealed record Dungeon(RegionalCoord RegionalCoordinate, int Depth) : MapLocation;
}

public sealed class Campaign
{
    public static readonly RegionalCoord DungeonRegionalCoordinate = new(4, 3);
    public static readonly HexCoord DungeonLocalCoordinate = HexCoord.Zero;
    public const string CoinResource = "Coins";
    public const string RawLodestoneItem = "Raw Lodestone";
    public const int RationCoinCost = 10;

    private readonly Dictionary<RegionalCoord, LocalMap> _localMaps = new();
    private readonly Dictionary<RegionalCoord, LocalMapOverlayState> _localMapOverlays = new();
    private readonly Random _random;
    private readonly List<TravelLogEntryState> _travelLog;
    private readonly bool _usesDeterministicLocalMaps;
    private PillarDelve? _pillarDelve;

    public Campaign(Random? random = null)
    {
        _random = random ?? Random.Shared;
        Regional = new RegionalMap(_random);
        WorldSeed = _random.Next();
        _usesDeterministicLocalMaps = true;
        Dungeon = PrototypeDungeonBuilder.CreateSixLevelDungeon();
        Ruin = new RuinExploration(RuinGenerationRules.RollAndGenerate(new SystemRandomSource(_random)), new GridCoord(0, 0));
        Party = new TravelParty([new Traveler("Expedition")]);
        PartyTravel = new PartyTravelState(new RegionalCoord(0, 0), HexCoord.Zero);
        _travelLog = [];
    }

    public Campaign(CampaignState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _random = Random.Shared;
        Regional = new RegionalMap(state.RegionalCells ?? throw new InvalidDataException("The campaign save is missing regional cells."));
        var hasLegacyFullLocalMaps = state.WorldSeed is null && state.LocalMaps is { Count: > 0 };
        WorldSeed = hasLegacyFullLocalMaps ? null : state.WorldSeed ?? 0;
        _usesDeterministicLocalMaps = WorldSeed is not null;
        Dungeon = PrototypeDungeonBuilder.CreateSixLevelDungeon();
        Ruin = new RuinExploration(RuinGenerationRules.RollAndGenerate(new SystemRandomSource(_random)), new GridCoord(0, 0));

        foreach (var localState in state.LocalMaps ?? [])
        {
            var localMap = new LocalMap(localState);
            if (!Regional.Contains(localMap.Parent) || Regional.GetTerrain(localMap.Parent) != localMap.ParentTerrain || !_localMaps.TryAdd(localMap.Parent, localMap))
            {
                throw new InvalidDataException("The campaign save contains an invalid local map.");
            }
        }

        foreach (var overlay in state.LocalMapOverlays ?? [])
        {
            var parent = new RegionalCoord(overlay.ParentColumn, overlay.ParentRow);
            if (!Regional.Contains(parent) || !_localMapOverlays.TryAdd(parent, overlay))
            {
                throw new InvalidDataException("The campaign save contains an invalid local map overlay.");
            }

            if (_localMaps.TryGetValue(parent, out var localMap))
            {
                localMap.ApplyOverlay(overlay, CreateLocalRandom(parent));
            }
        }

        var legacyTravelState = state.PartyTravel;
        Party = state.Party?.Members is { Count: > 0 } members
            ? new TravelParty(members.Select(member => new Traveler(member)))
            : new TravelParty([new Traveler(
                "Expedition",
                rations: legacyTravelState?.Rations ?? 0)]);
        if (state.Party is null && legacyTravelState is { Exhaustion: > 0 })
        {
            Party.Members[0].AddExhaustion(legacyTravelState.Exhaustion);
        }

        PartyTravel = legacyTravelState is { } partyState
            ? new PartyTravelState(
                new RegionalCoord(partyState.RegionalColumn, partyState.RegionalRow),
                new HexCoord(partyState.LocalQ, partyState.LocalR),
                partyState.Day,
                partyState.DailyMiles,
                partyState.ForcedMarchUsed)
            : new PartyTravelState(new RegionalCoord(0, 0), HexCoord.Zero);

        if (!Regional.Contains(PartyTravel.RegionalCoordinate) || !GetLocalMap(PartyTravel.RegionalCoordinate).VisibleCells.Contains(PartyTravel.LocalCoordinate))
        {
            throw new InvalidDataException("The campaign save contains an invalid party location.");
        }

        _travelLog = (state.TravelLog ?? [])
            .Where(entry => entry.Day >= 1 && !string.IsNullOrWhiteSpace(entry.Message))
            .TakeLast(100)
            .ToList();
        if (state.PillarDelve is not null)
        {
            _pillarDelve = new PillarDelve(state.PillarDelve);
        }
    }

    public RegionalMap Regional { get; }
    public Dungeon Dungeon { get; }
    public RuinExploration Ruin { get; }
    public TravelParty Party { get; }
    public PartyTravelState PartyTravel { get; }
    public int? WorldSeed { get; }
    public IReadOnlyList<TravelLogEntryState> TravelLog => _travelLog;
    public bool IsInPillarDelve => _pillarDelve is not null;
    public Terrain PartyTerrain => GetLocalMap(PartyTravel.RegionalCoordinate).GetTerrain(PartyTravel.LocalCoordinate);
    public bool IsPartyOnSettlement => !IsInPillarDelve && PartyTerrain == Terrain.Settlements;
    public bool IsPartyOnPillar => PartyTerrain == Terrain.Pillars;
    public int PartyCoins => Party.Members.Sum(member => member.GetResource(CoinResource));
    public int PartyRawLodestone => Party.Members.Sum(member => CountInventoryItems(member, RawLodestoneItem));
    public bool PartyHasMiningTools => Party.Members.Any(HasMiningTools);

    public CampaignState ToState()
    {
        var localMaps = _usesDeterministicLocalMaps
            ? []
            : _localMaps.Values.Select(map => map.ToState()).ToList();
        var overlays = _usesDeterministicLocalMaps
            ? CreateLocalMapOverlays()
            : [];

        return new CampaignState(
            Regional.ToState().ToList(),
            localMaps,
            PartyTravel.ToState(),
            Party.ToState(),
            _travelLog.ToList(),
            CampaignFile.CurrentVersion,
            WorldSeed,
            overlays,
            _pillarDelve?.ToState());
    }

    public LocalMap GetLocalMap(RegionalCoord coordinate)
    {
        if (!Regional.Contains(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        if (!_localMaps.TryGetValue(coordinate, out var localMap))
        {
            localMap = new LocalMap(coordinate, Regional.GetTerrain(coordinate), CreateLocalRandom(coordinate));
            if (_localMapOverlays.TryGetValue(coordinate, out var overlay))
            {
                localMap.ApplyOverlay(overlay, CreateLocalRandom(coordinate));
            }

            _localMaps.Add(coordinate, localMap);
        }

        return localMap;
    }

    private Random CreateLocalRandom(RegionalCoord coordinate)
    {
        if (!_usesDeterministicLocalMaps || WorldSeed is not { } seed)
        {
            return _random;
        }

        unchecked
        {
            var hash = seed;
            hash = (hash * 397) ^ coordinate.Column;
            hash = (hash * 397) ^ coordinate.Row;
            return new Random(hash);
        }
    }

    private List<LocalMapOverlayState> CreateLocalMapOverlays()
    {
        var overlays = _localMapOverlays.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var map in _localMaps.Values)
        {
            overlays[map.Parent] = map.ToOverlayState();
        }

        return overlays
            .OrderBy(entry => entry.Key.Column)
            .ThenBy(entry => entry.Key.Row)
            .Select(entry => entry.Value)
            .ToList();
    }

    public bool HasDungeonEntrance(RegionalCoord regionalCoordinate) =>
        regionalCoordinate == DungeonRegionalCoordinate;

    public bool IsPartyAtDungeonEntrance =>
        PartyTravel.RegionalCoordinate == DungeonRegionalCoordinate && PartyTravel.LocalCoordinate == DungeonLocalCoordinate;

    public bool TryMoveRuinRoom(GridCoord target) => Ruin.TryMoveToRoom(target);

    public void SearchRuinRoom() => Ruin.SearchCurrentRoom();

    public void DescendRuin()
    {
        var next = RuinGenerationRules.RollAndGenerate(new SystemRandomSource(_random));
        Ruin.Descend(next, new GridCoord(0, 0));
    }

    public IReadOnlyList<RegionalCoord> GetLocalArea(RegionalCoord centre)
    {
        if (!Regional.Contains(centre))
        {
            throw new ArgumentOutOfRangeException(nameof(centre));
        }

        var area = new List<RegionalCoord> { centre };
        for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
        {
            if (Regional.GetNeighbour(centre, direction) is { } neighbour)
            {
                area.Add(neighbour);
            }
        }

        return area;
    }

    public IReadOnlyList<LocalMapCoord> GetAvailablePartyMoves()
    {
        if (PartyTravel.RestRequired)
        {
            return [];
        }

        var originRegion = PartyTravel.RegionalCoordinate;
        var originMap = GetLocalMap(originRegion);
        var moves = new List<LocalMapCoord>();
        for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
        {
            var adjacent = PartyTravel.LocalCoordinate.Neighbour(direction);
            if (originMap.VisibleCells.Contains(adjacent))
            {
                moves.Add(new LocalMapCoord(originRegion, adjacent));
                continue;
            }

            if (Regional.GetNeighbour(originRegion, direction) is { } destinationRegion)
            {
                moves.Add(new LocalMapCoord(destinationRegion, FindBoundaryEntry(originRegion, PartyTravel.LocalCoordinate, direction, destinationRegion)));
            }
        }

        return moves;
    }

    public PartyMoveResult TryMoveParty(RegionalCoord localMapCoordinate, HexCoord target)
    {
        if (PartyTravel.RestRequired)
        {
            return MoveFailed("Rest is required before the party can move again.");
        }

        var destination = new LocalMapCoord(localMapCoordinate, target);
        if (!GetAvailablePartyMoves().Contains(destination))
        {
            return MoveFailed("The selected hex is not reachable in one local-mile move.");
        }

        var crossedRegionalBoundary = PartyTravel.RegionalCoordinate != localMapCoordinate;
        PartyTravel.MoveTo(localMapCoordinate, target);
        var interruption = GetTravelInterruption(localMapCoordinate, target);
        var message = PartyTravel.RestRequired
            ? $"Moved 1 mile. {PartyTravel.DailyMiles} / {PartyTravel.DailyMileLimit} miles; rest is required."
            : crossedRegionalBoundary
                ? $"Crossed into regional hex {localMapCoordinate}. Moved 1 mile; {PartyTravel.DailyMiles} / {PartyTravel.DailyMileLimit} miles."
                : $"Moved 1 mile. {PartyTravel.DailyMiles} / {PartyTravel.DailyMileLimit} miles.";
        return MoveSucceeded(message, interruption);
    }

    public PartyMoveResult TryTravelRegionalStep(RegionalCoord destination)
    {
        if (PartyTravel.RestRequired) return MoveFailed("Rest is required before regional travel.");
        if (!Enumerable.Range(0, HexCoord.Directions.Count).Select(direction => Regional.GetNeighbour(PartyTravel.RegionalCoordinate, direction)).Any(neighbour => neighbour == destination))
        {
            return MoveFailed("The selected regional hex is not adjacent to the party.");
        }
        for (var mile = 0; mile < LocalMap.SideLengthInSubhexes && !PartyTravel.RestRequired; mile++)
        {
            PartyTravel.MoveTo(destination, HexCoord.Zero);
        }
        return MoveSucceeded($"Crossed into regional hex {destination}. {PartyTravel.DailyMiles} / {PartyTravel.DailyMileLimit} miles travelled.", GetTravelInterruption(destination, HexCoord.Zero));
    }

    public PartyMoveResult TryBeginForcedMarch()
    {
        if (!PartyTravel.CanForcedMarch)
        {
            return MoveFailed("Forced march is available after 18 miles and before resting.");
        }

        PartyTravel.BeginForcedMarch(Party);
        return MoveSucceeded($"Forced march started: each party member gains 1 exhaustion. Travel allowance is now {PartyTravel.DailyMileLimit} miles.");
    }

    public PartyMoveResult TryRestParty()
    {
        var localMap = GetLocalMap(PartyTravel.RegionalCoordinate);
        var terrainEvent = TerrainDayEventService.ResolveDay(localMap.GetTerrain(PartyTravel.LocalCoordinate), Party, new SystemRandomSource(_random));
        var restResult = PartyTravel.Rest(Party);
        foreach (var traveler in Party.Members)
        {
            traveler.RecoverGritAfterRest(fullDayOfRest: true, new SystemRandomSource(_random));
        }
        localMap.AdvanceRoamingHazards(_random);
        var rationMessage = restResult.UnfedTravelers == 0
            ? $"The party rests and consumes {restResult.FedTravelers} ration(s)."
            : $"The party rests: {restResult.FedTravelers} ration(s) consumed; {restResult.UnfedTravelers} member(s) gain 1 exhaustion from hunger.";
        var terrainMessage = terrainEvent.Weather is null ? string.Empty : $" Wastes day: {terrainEvent.Weather.Rule.Name}; encounter: {terrainEvent.Encounter!.Name}.";
        return MoveSucceeded($"{rationMessage} Roaming hazards advance to local day {localMap.RoamingHazardDay}.{terrainMessage} Day {PartyTravel.Day} begins with 18 miles available.");
    }

    private PartyMoveResult MoveFailed(string message) => new(
        false,
        message,
        PartyTravel.DailyMiles,
        PartyTravel.DailyMileLimit,
        PartyTravel.RestRequired);

    public TravelInterruption? GetTravelInterruption(RegionalCoord regionalCoordinate, HexCoord localCoordinate)
    {
        var map = GetLocalMap(regionalCoordinate);
        var terrain = map.GetTerrain(localCoordinate);
        if (map.RoamingHazards.TryGetValue(localCoordinate, out var hazard)) return new TravelInterruption(TravelInterruptionKind.RoamingHazard, terrain, hazard);
        return terrain switch
        {
            Terrain.Ruins => new TravelInterruption(TravelInterruptionKind.Ruins, terrain),
            Terrain.Settlements => new TravelInterruption(TravelInterruptionKind.Settlement, terrain),
            _ => null,
        };
    }

    public TravelInterruptionResolution ResolveTravelInterruption(TravelInterruption interruption, TravelResolutionOptions? options = null)
    {
        var resolution = TravelInterruptionResolver.Resolve(interruption, Party, new SystemRandomSource(_random), options);
        AppendTravelLog(string.Join(" ", resolution.Log));
        return resolution;
    }

    public CampaignActionResult TryBuyRationsAtSettlement(int quantity = 1)
    {
        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (!IsPartyOnSettlement)
        {
            return CampaignActionResult.Blocked("Settlement shop", "The party must be standing on a Settlement subhex to buy supplies.");
        }

        var market = new SettlementMarket(SettlementScarcity.Middling);
        var purchase = market.Purchase(RationCoinCost, quantity, supplies: true, offersBarterItem: false);
        if (!purchase.Purchased)
        {
            return CampaignActionResult.Blocked("Settlement shop", purchase.Failure ?? "The settlement rejects the purchase.");
        }

        if (!TrySpendPartyCoins(purchase.CoinCost))
        {
            return CampaignActionResult.Blocked("Settlement shop", $"Buying {purchase.QuantityReceived} ration(s) costs {purchase.CoinCost} coins; the party has {PartyCoins}.");
        }

        GrantPartyRations(purchase.QuantityReceived);
        return LogAction(
            "Settlement shop",
            $"Bought {purchase.QuantityReceived} ration(s) for {purchase.CoinCost} coins.",
            $"Party rations: {Party.TotalRations}; coins: {PartyCoins}.");
    }

    public CampaignActionResult TryRefineRawLodestoneAtSettlement()
    {
        if (!IsPartyOnSettlement)
        {
            return CampaignActionResult.Blocked("Settlement shop", "Raw Lodestone can be refined only at a Settlement.");
        }

        var rawAvailable = PartyRawLodestone;
        if (rawAvailable == 0)
        {
            return CampaignActionResult.Blocked("Settlement shop", "The party has no recorded Raw Lodestone.");
        }

        var random = new SystemRandomSource(_random);
        var coinsGained = 0;
        var rawRefined = 0;
        foreach (var traveler in Party.Members)
        {
            var travelerRaw = CountInventoryItems(traveler, RawLodestoneItem);
            if (travelerRaw == 0)
            {
                continue;
            }

            coinsGained += PillarMiningService.RefineAtSettlement(traveler.Inventory, travelerRaw, random);
            rawRefined += travelerRaw;
        }

        AddPartyCoins(coinsGained);
        return LogAction(
            "Settlement shop",
            $"Refined {rawRefined} Raw Lodestone into {coinsGained} coins.",
            $"Party Raw Lodestone: {PartyRawLodestone}; coins: {PartyCoins}.");
    }

    public CampaignActionResult TryWorkPillar(PillarWork work)
    {
        if (!IsPartyOnPillar || IsInPillarDelve)
        {
            return CampaignActionResult.Blocked("Pillar work", "The party must be at a Pillar and outside its tunnels.");
        }

        var worker = Party.Members
            .OrderByDescending(member => member.Inventory.AvailableSlots)
            .ThenBy(member => member.Name, StringComparer.Ordinal)
            .First();
        try
        {
            var result = PillarMiningService.WorkHour(work, PartyHasMiningTools, worker.Inventory, new SystemRandomSource(_random));
            return LogAction(
                "Pillar work",
                $"{worker.Name} spends 1 hour {work.ToString().ToLowerInvariant()} at the Pillar.",
                $"Raw Lodestone rolled: {result.RawLodestoneRolled}; collected: {result.RawLodestoneCollected}; encounter roll modifier: +{result.EncounterRollModifier}.",
                $"Party Raw Lodestone: {PartyRawLodestone}.");
        }
        catch (InvalidOperationException exception)
        {
            return CampaignActionResult.Blocked("Pillar work", exception.Message);
        }
    }

    public CampaignActionResult TryEnterPillarDelve()
    {
        if (!IsPartyOnPillar)
        {
            return CampaignActionResult.Blocked("Pillar delve", "The party must be standing on Pillars to delve into their tunnels.");
        }

        _pillarDelve ??= new PillarDelve();
        return EnterNextPillarTunnel("Pillar delve");
    }

    public CampaignActionResult TryGoDeeperInPillar()
    {
        if (_pillarDelve is null)
        {
            return CampaignActionResult.Blocked("Pillar delve", "Enter a Pillar delve before going deeper.");
        }

        return EnterNextPillarTunnel("Pillar delve");
    }

    public CampaignActionResult TrySearchPillarTunnel()
    {
        if (_pillarDelve is null)
        {
            return CampaignActionResult.Blocked("Pillar delve", "Enter a Pillar delve before searching its tunnels.");
        }

        var random = new SystemRandomSource(_random);
        var loot = _pillarDelve.RollLoot(random);
        var eventRule = _pillarDelve.RollEvent(random);
        return LogAction(
            "Pillar delve",
            $"Search takes {PillarDelve.MinutesToSearchTunnel} minutes at tunnel depth {_pillarDelve.Tunnels.Count}.",
            $"Loot: {loot.Name}. {loot.RuleText}",
            $"Pressure: {eventRule.Name}. {eventRule.RuleText}");
    }

    public CampaignActionResult TryExitPillarDelve()
    {
        if (_pillarDelve is null)
        {
            return CampaignActionResult.Blocked("Pillar delve", "The party is not currently inside a Pillar delve.");
        }

        var tunnelCount = _pillarDelve.Tunnels.Count;
        _pillarDelve = null;
        return LogAction("Pillar delve", $"Exited the Pillar after exploring {tunnelCount} tunnel(s).");
    }

    private PartyMoveResult MoveSucceeded(string message, TravelInterruption? interruption = null)
    {
        AppendTravelLog(message);
        return new PartyMoveResult(true, message, PartyTravel.DailyMiles, PartyTravel.DailyMileLimit, PartyTravel.RestRequired, interruption);
    }

    private void AppendTravelLog(string message)
    {
        _travelLog.Add(new TravelLogEntryState(PartyTravel.Day, message));
        if (_travelLog.Count > 100)
        {
            _travelLog.RemoveRange(0, _travelLog.Count - 100);
        }
    }

    private HexCoord FindBoundaryEntry(RegionalCoord originRegion, HexCoord originLocal, int direction, RegionalCoord destinationRegion)
    {
        var step = Subtract(LocalCentre(originLocal.Neighbour(direction)), LocalCentre(originLocal));
        var target = Add(Add(ChunkCentre(originRegion), LocalCentre(originLocal)), step);
        return GetLocalMap(destinationRegion).VisibleCells
            .OrderBy(candidate => DistanceSquared(Add(ChunkCentre(destinationRegion), LocalCentre(candidate)), target))
            .ThenBy(candidate => candidate.Q)
            .ThenBy(candidate => candidate.R)
            .First();
    }

    private static (double X, double Y) ChunkCentre(RegionalCoord coordinate) =>
        (coordinate.Column * 9d, (coordinate.Row + (coordinate.Column % 2) * 0.5d) * 6d * Math.Sqrt(3d));

    private static (double X, double Y) LocalCentre(HexCoord coordinate) =>
        (coordinate.Q * 1.5d, (coordinate.R + coordinate.Q * 0.5d) * Math.Sqrt(3d));

    private static (double X, double Y) Subtract((double X, double Y) left, (double X, double Y) right) =>
        (left.X - right.X, left.Y - right.Y);

    private static (double X, double Y) Add((double X, double Y) left, (double X, double Y) right) =>
        (left.X + right.X, left.Y + right.Y);

    private static double DistanceSquared((double X, double Y) left, (double X, double Y) right) =>
        (left.X - right.X) * (left.X - right.X) + (left.Y - right.Y) * (left.Y - right.Y);

    private CampaignActionResult EnterNextPillarTunnel(string title)
    {
        _pillarDelve ??= new PillarDelve();
        var tunnel = _pillarDelve.EnterTunnel(new SystemRandomSource(_random));
        var splitText = tunnel.SplitMarker is null ? string.Empty : $" Split marker: {tunnel.SplitMarker}.";
        return LogAction(
            title,
            $"Moved through a Pillar tunnel for {PillarDelve.MinutesToTravelTunnel} minutes.",
            $"Depth {tunnel.Depth}: {tunnel.Shape.Name}. {tunnel.Shape.RuleText}{splitText}");
    }

    private CampaignActionResult LogAction(string title, params string[] messages)
    {
        AppendTravelLog(string.Join(" ", messages));
        return new CampaignActionResult(true, title, messages);
    }

    private void GrantPartyRations(int amount)
    {
        for (var ration = 0; ration < amount; ration++)
        {
            var recipient = Party.Members
                .OrderBy(member => member.Rations)
                .ThenBy(member => member.Name, StringComparer.Ordinal)
                .First();
            recipient.AddRations(1);
        }
    }

    private bool TrySpendPartyCoins(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (PartyCoins < amount)
        {
            return false;
        }

        var remaining = amount;
        foreach (var member in Party.Members.OrderByDescending(member => member.GetResource(CoinResource)).ThenBy(member => member.Name, StringComparer.Ordinal))
        {
            var spend = Math.Min(member.GetResource(CoinResource), remaining);
            if (spend == 0)
            {
                continue;
            }

            member.TryLoseResource(CoinResource, spend);
            remaining -= spend;
            if (remaining == 0)
            {
                return true;
            }
        }

        return false;
    }

    private void AddPartyCoins(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        var purse = Party.Members[0];
        purse.SetResource(CoinResource, purse.GetResource(CoinResource) + amount);
    }

    private static int CountInventoryItems(Traveler traveler, string itemName) =>
        traveler.Inventory.Items.Count(item => string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase));

    private static bool HasMiningTools(Traveler traveler) =>
        traveler.Inventory.Items.Any(IsTool) ||
        traveler.Inventory.Loadouts.Any(loadout => ContainsToolText(loadout.Purpose));

    private static bool IsTool(InventoryItem item) =>
        ContainsToolText(item.Name);

    private static bool ContainsToolText(string value) =>
        value.Contains("tool", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("mining", StringComparison.OrdinalIgnoreCase);
}

public sealed record CampaignActionResult(bool Applied, string Title, IReadOnlyList<string> Log)
{
    public string Summary => string.Join("\n", Log);

    public static CampaignActionResult Blocked(string title, string message) => new(false, title, [message]);
}

public sealed class MapNavigationService
{
    public MapNavigationService(Campaign campaign)
    {
        Campaign = campaign;
        Current = new MapLocation.Regional(new RegionalCoord(0, 0));
    }

    public Campaign Campaign { get; }
    public MapLocation Current { get; private set; }

    public void SelectRegional(RegionalCoord coordinate)
    {
        if (!Campaign.Regional.Contains(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        Current = new MapLocation.Regional(coordinate);
    }

    public void EnterLocal(RegionalCoord coordinate)
    {
        Campaign.GetLocalMap(coordinate);
        Current = new MapLocation.Local(coordinate);
    }

    public bool TryEnterDungeon()
    {
        if (Current is not MapLocation.Local local || !Campaign.HasDungeonEntrance(local.RegionalCoordinate) || !Campaign.IsPartyAtDungeonEntrance)
        {
            return false;
        }

        Current = new MapLocation.Dungeon(local.RegionalCoordinate, Depth: 0);
        return true;
    }

    public void SetDungeonDepth(int depth)
    {
        if (Current is not MapLocation.Dungeon dungeon || !Campaign.Dungeon.HasDepth(depth))
        {
            throw new InvalidOperationException("A valid dungeon level must be active before changing depth.");
        }

        Current = dungeon with { Depth = depth };
    }

    public void ReturnToRegional()
    {
        var regionalCoordinate = Current switch
        {
            MapLocation.Regional regional => regional.Coordinate,
            MapLocation.Local local => local.RegionalCoordinate,
            MapLocation.Dungeon dungeon => dungeon.RegionalCoordinate,
            _ => throw new InvalidOperationException(),
        };
        Current = new MapLocation.Regional(regionalCoordinate);
    }

    public void ReturnToLocal()
    {
        var regionalCoordinate = Current switch
        {
            MapLocation.Local local => local.RegionalCoordinate,
            MapLocation.Dungeon dungeon => dungeon.RegionalCoordinate,
            _ => throw new InvalidOperationException("Only a dungeon has a local parent."),
        };
        Current = new MapLocation.Local(regionalCoordinate);
    }
}
