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
        var message = PartyTravel.RestRequired
            ? $"Moved 1 mile. {PartyTravel.DailyMiles} / {PartyTravel.DailyMileLimit} miles; rest is required."
            : crossedRegionalBoundary
                ? $"Crossed into regional hex {localMapCoordinate}. Moved 1 mile; {PartyTravel.DailyMiles} / {PartyTravel.DailyMileLimit} miles."
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
