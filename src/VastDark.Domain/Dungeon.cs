namespace VastDark.Domain;

public enum DungeonTile
{
    Wall,
    Floor,
    StairUp,
    StairDown,
}

public readonly record struct GridCoord(int X, int Y)
{
    public override string ToString() => $"({X}, {Y})";
}

public sealed class DungeonLevel
{
    private readonly DungeonTile[,] _tiles;

    public DungeonLevel(int depth, int width, int height)
    {
        if (width < 3 || height < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Dungeon levels must be at least 3×3.");
        }

        Depth = depth;
        Width = width;
        Height = height;
        _tiles = new DungeonTile[width, height];
    }

    public int Depth { get; }
    public int Width { get; }
    public int Height { get; }

    public bool Contains(GridCoord coordinate) =>
        coordinate.X >= 0 && coordinate.X < Width && coordinate.Y >= 0 && coordinate.Y < Height;

    public DungeonTile GetTile(GridCoord coordinate) =>
        Contains(coordinate)
            ? _tiles[coordinate.X, coordinate.Y]
            : throw new ArgumentOutOfRangeException(nameof(coordinate));

    public void SetTile(GridCoord coordinate, DungeonTile tile)
    {
        if (!Contains(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        _tiles[coordinate.X, coordinate.Y] = tile;
    }
}

public sealed class Dungeon
{
    public const int MaximumLevels = 6;

    private readonly IReadOnlyDictionary<int, DungeonLevel> _levels;

    public Dungeon(IReadOnlyDictionary<int, DungeonLevel> levels)
    {
        if (levels.Count is < 1 or > MaximumLevels)
        {
            throw new ArgumentOutOfRangeException(nameof(levels), $"A dungeon has one to {MaximumLevels} levels.");
        }

        _levels = levels;
    }

    public IReadOnlyDictionary<int, DungeonLevel> Levels => _levels;
    public bool HasDepth(int depth) => _levels.ContainsKey(depth);
    public DungeonLevel GetLevel(int depth) =>
        _levels.TryGetValue(depth, out var level)
            ? level
            : throw new ArgumentOutOfRangeException(nameof(depth));
}

/// <summary>
/// Visual test fixture only. Final room and corridor generation will replace
/// this builder without changing the dungeon-level contract.
/// </summary>
public static class PrototypeDungeonBuilder
{
    public static Dungeon CreateSixLevelDungeon()
    {
        var levels = new Dictionary<int, DungeonLevel>();
        for (var depth = 0; depth < Dungeon.MaximumLevels; depth++)
        {
            levels.Add(depth, CreateLevel(depth));
        }

        return new Dungeon(levels);
    }

    private static DungeonLevel CreateLevel(int depth)
    {
        var level = new DungeonLevel(depth, width: 32, height: 24);
        CarveRoom(level, 3, 3, 10, 8);
        CarveRoom(level, 18, 5, 10, 7);
        CarveRoom(level, 10, 15, 12, 5);
        CarveHorizontalCorridor(level, 12, 7, 18);
        CarveVerticalCorridor(level, 20, 11, 15);
        CarveHorizontalCorridor(level, 16, 15, 20);

        if (depth > 0)
        {
            level.SetTile(new GridCoord(6, 6), DungeonTile.StairUp);
        }

        if (depth < Dungeon.MaximumLevels - 1)
        {
            level.SetTile(new GridCoord(20, 8), DungeonTile.StairDown);
        }

        return level;
    }

    private static void CarveRoom(DungeonLevel level, int left, int top, int width, int height)
    {
        for (var x = left; x < left + width; x++)
        {
            for (var y = top; y < top + height; y++)
            {
                level.SetTile(new GridCoord(x, y), DungeonTile.Floor);
            }
        }
    }

    private static void CarveHorizontalCorridor(DungeonLevel level, int y, int fromX, int toX)
    {
        for (var x = Math.Min(fromX, toX); x <= Math.Max(fromX, toX); x++)
        {
            level.SetTile(new GridCoord(x, y), DungeonTile.Floor);
        }
    }

    private static void CarveVerticalCorridor(DungeonLevel level, int x, int fromY, int toY)
    {
        for (var y = Math.Min(fromY, toY); y <= Math.Max(fromY, toY); y++)
        {
            level.SetTile(new GridCoord(x, y), DungeonTile.Floor);
        }
    }
}

