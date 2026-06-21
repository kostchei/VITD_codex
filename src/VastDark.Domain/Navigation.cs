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

    private readonly Dictionary<RegionalCoord, LocalMap> _localMaps = new();
    private readonly Random _random;

    public Campaign(Random? random = null)
    {
        _random = random ?? Random.Shared;
        Regional = new RegionalMap(_random);
        Dungeon = PrototypeDungeonBuilder.CreateSixLevelDungeon();
        PartyTravel = new PartyTravelState(new RegionalCoord(0, 0), HexCoord.Zero);
    }

    public Campaign(CampaignState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _random = Random.Shared;
        Regional = new RegionalMap(state.RegionalCells ?? throw new InvalidDataException("The campaign save is missing regional cells."));
        Dungeon = PrototypeDungeonBuilder.CreateSixLevelDungeon();

        foreach (var localState in state.LocalMaps ?? throw new InvalidDataException("The campaign save is missing local maps."))
        {
            var localMap = new LocalMap(localState);
            if (!Regional.Contains(localMap.Parent) || Regional.GetTerrain(localMap.Parent) != localMap.ParentTerrain || !_localMaps.TryAdd(localMap.Parent, localMap))
            {
                throw new InvalidDataException("The campaign save contains an invalid local map.");
            }
        }

        PartyTravel = state.PartyTravel is { } partyState
            ? new PartyTravelState(
                new RegionalCoord(partyState.RegionalColumn, partyState.RegionalRow),
                new HexCoord(partyState.LocalQ, partyState.LocalR),
                partyState.Day,
                partyState.DailyMiles,
                partyState.Exhaustion,
                partyState.ForcedMarchUsed,
                partyState.Rations)
            : new PartyTravelState(new RegionalCoord(0, 0), HexCoord.Zero);

        if (!Regional.Contains(PartyTravel.RegionalCoordinate) || !GetLocalMap(PartyTravel.RegionalCoordinate).VisibleCells.Contains(PartyTravel.LocalCoordinate))
        {
            throw new InvalidDataException("The campaign save contains an invalid party location.");
        }
    }

    public RegionalMap Regional { get; }
    public Dungeon Dungeon { get; }
    public PartyTravelState PartyTravel { get; }

    public CampaignState ToState() => new(
        Regional.ToState().ToList(),
        _localMaps.Values.Select(map => map.ToState()).ToList(),
        PartyTravel.ToState());

    public LocalMap GetLocalMap(RegionalCoord coordinate)
    {
        if (!Regional.Contains(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        if (!_localMaps.TryGetValue(coordinate, out var localMap))
        {
            localMap = new LocalMap(coordinate, Regional.GetTerrain(coordinate), _random);
            _localMaps.Add(coordinate, localMap);
        }

        return localMap;
    }

    public bool HasDungeonEntrance(RegionalCoord regionalCoordinate) =>
        regionalCoordinate == DungeonRegionalCoordinate;

    public PartyMoveResult TryMoveParty(RegionalCoord localMapCoordinate, HexCoord target)
    {
        if (PartyTravel.RegionalCoordinate != localMapCoordinate)
        {
            return MoveFailed("The party is in a different regional hex.");
        }

        var map = GetLocalMap(localMapCoordinate);
        if (!map.VisibleCells.Contains(target))
        {
            return MoveFailed("The selected local hex cannot be entered.");
        }

        if (PartyTravel.RestRequired)
        {
            return MoveFailed("Rest is required before the party can move again.");
        }

        if (PartyTravel.LocalCoordinate.DistanceTo(target) != 1)
        {
            return MoveFailed("The party can move only to an adjacent local hex.");
        }

        PartyTravel.MoveTo(target);
        var message = PartyTravel.RestRequired
            ? $"Moved 1 mile. {PartyTravel.DailyMiles} / {PartyTravel.DailyMileLimit} miles; rest is required."
            : $"Moved 1 mile. {PartyTravel.DailyMiles} / {PartyTravel.DailyMileLimit} miles.";
        return new PartyMoveResult(true, message, PartyTravel.DailyMiles, PartyTravel.DailyMileLimit, PartyTravel.RestRequired);
    }

    public PartyMoveResult TryBeginForcedMarch()
    {
        if (!PartyTravel.CanForcedMarch)
        {
            return MoveFailed("Forced march is available after 18 miles and before resting.");
        }

        PartyTravel.BeginForcedMarch();
        return new PartyMoveResult(
            true,
            $"Forced march started: 1 exhaustion gained. Travel allowance is now {PartyTravel.DailyMileLimit} miles.",
            PartyTravel.DailyMiles,
            PartyTravel.DailyMileLimit,
            PartyTravel.RestRequired);
    }

    public PartyMoveResult TryRestParty()
    {
        var consumedRation = PartyTravel.Rest();
        return new PartyMoveResult(
            true,
            consumedRation
                ? $"The party rests and consumes 1 ration. Day {PartyTravel.Day} begins with 18 miles available."
                : $"The party rests without a ration and gains 1 exhaustion. Day {PartyTravel.Day} begins with 18 miles available.",
            PartyTravel.DailyMiles,
            PartyTravel.DailyMileLimit,
            PartyTravel.RestRequired);
    }

    private PartyMoveResult MoveFailed(string message) => new(
        false,
        message,
        PartyTravel.DailyMiles,
        PartyTravel.DailyMileLimit,
        PartyTravel.RestRequired);
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
        if (Current is not MapLocation.Local local || !Campaign.HasDungeonEntrance(local.RegionalCoordinate))
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
