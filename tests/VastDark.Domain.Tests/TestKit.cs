using VastDark.Domain;

internal static class TestKit
{
    internal static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
    
    internal static void AssertThrows(Action action, string message)
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
    
    internal static bool ThrowsInvalidOperation(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return true; }
        return false;
    }
    
    internal static bool IsConnected(GeneratedRuin ruin)
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
    
    internal static Dictionary<int, WastesEntry> CreateWastesOutcomes(WastesEntry entry) =>
        Enumerable.Range(2, 17).ToDictionary(total => total, _ => entry);
    
    internal static IReadOnlyList<HexCoord> FindBoundaryPath(LocalMap map, RegionalMap regional, RegionalCoord origin)
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
}

internal sealed class ScriptedRandom(params int[] values) : IRandomSource
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

internal sealed class ScriptedSystemRandom(params int[] values) : Random
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
