namespace VastDark.Domain;

public enum Terrain
{
    Wastes,
    Ruins,
    Pillars,
    Settlements,
}

public readonly record struct RegionalCoord(int Column, int Row)
{
    public override string ToString() => $"({Column}, {Row})";
}

public sealed class RegionalMap
{
    public const int Width = 10;
    public const int Height = 8;
    public const int DiceCount = 8;

    private readonly HashSet<RegionalCoord> _cells;
    private readonly Dictionary<RegionalCoord, Terrain> _terrain;
    private readonly Dictionary<RegionalCoord, int> _diceRolls;

    public RegionalMap(Random? random = null)
    {
        random ??= Random.Shared;
        _cells = new HashSet<RegionalCoord>();
        _terrain = new Dictionary<RegionalCoord, Terrain>();
        _diceRolls = new Dictionary<RegionalCoord, int>();
        for (var row = 0; row < Height; row++)
        {
            for (var column = 0; column < Width; column++)
            {
                var coordinate = new RegionalCoord(column, row);
                _cells.Add(coordinate);
                _terrain.Add(coordinate, Terrain.Wastes);
            }
        }

        foreach (var coordinate in ChooseDistinct(_cells, DiceCount, random))
        {
            var roll = RollD6(random);
            _diceRolls.Add(coordinate, roll);
            _terrain[coordinate] = roll switch
            {
                1 => Terrain.Wastes,
                <= 4 => Terrain.Ruins,
                _ => Terrain.Pillars,
            };
        }
    }

    public RegionalMap(IEnumerable<RegionalCellState> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        _cells = new HashSet<RegionalCoord>();
        _terrain = new Dictionary<RegionalCoord, Terrain>();
        _diceRolls = new Dictionary<RegionalCoord, int>();

        foreach (var state in cells)
        {
            var coordinate = new RegionalCoord(state.Column, state.Row);
            if (!ContainsGridCoordinate(coordinate) || !_cells.Add(coordinate))
            {
                throw new InvalidDataException("The campaign save contains an invalid regional cell.");
            }

            _terrain.Add(coordinate, state.Terrain);
            if (state.DieRoll is { } roll)
            {
                ValidateDieRoll(roll);
                _diceRolls.Add(coordinate, roll);
            }
        }

        if (_cells.Count != Width * Height || _diceRolls.Count != DiceCount)
        {
            throw new InvalidDataException("The campaign save does not contain a complete regional map.");
        }
    }

    public IReadOnlyCollection<RegionalCoord> Cells => _cells;
    public IReadOnlyDictionary<RegionalCoord, Terrain> TerrainByCell => _terrain;
    public IReadOnlyDictionary<RegionalCoord, int> DiceRolls => _diceRolls;

    public bool Contains(RegionalCoord coordinate) => _cells.Contains(coordinate);

    public Terrain GetTerrain(RegionalCoord coordinate) => _terrain[coordinate];

    public RegionalCellState[] ToState() => _cells
        .Select(coordinate => new RegionalCellState(
            coordinate.Column,
            coordinate.Row,
            _terrain[coordinate],
            _diceRolls.TryGetValue(coordinate, out var roll) ? roll : null))
        .ToArray();

    internal static int RollD6(Random random) => random.Next(1, 7);

    private static bool ContainsGridCoordinate(RegionalCoord coordinate) =>
        coordinate.Column >= 0 && coordinate.Column < Width && coordinate.Row >= 0 && coordinate.Row < Height;

    internal static void ValidateDieRoll(int roll)
    {
        if (roll is < 1 or > 6)
        {
            throw new InvalidDataException("Dice rolls must be between 1 and 6.");
        }
    }

    internal static IEnumerable<TCoordinate> ChooseDistinct<TCoordinate>(IReadOnlyCollection<TCoordinate> cells, int count, Random random)
        where TCoordinate : notnull
    {
        if (count < 0 || count > cells.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var shuffled = cells.ToList();
        for (var index = shuffled.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
        }

        return shuffled.Take(count);
    }
}

public sealed class LocalMap
{
    // The regional hex spans six one-mile local-hex widths. Its outline clips
    // the surrounding tessellation, so edge subhexes may be partial.
    public const int SideLengthInSubhexes = 6;
    public const int MilesPerSubhex = 1;
    public const int FlatToFlatSubhexes = SideLengthInSubhexes;

    private readonly HashSet<HexCoord> _cells;
    private readonly Dictionary<HexCoord, Terrain> _terrain;
    private readonly Dictionary<HexCoord, int> _diceRolls;

    public LocalMap(RegionalCoord parent, Terrain parentTerrain = Terrain.Wastes, Random? random = null)
    {
        random ??= Random.Shared;
        Parent = parent;
        ParentTerrain = parentTerrain;
        _cells = CreateHexagonalCells(SideLengthInSubhexes);
        _terrain = _cells.ToDictionary(cell => cell, _ => parentTerrain == Terrain.Pillars ? Terrain.Pillars : Terrain.Wastes);
        _diceRolls = new Dictionary<HexCoord, int>();
        DensityRoll = RegionalMap.RollD6(random);
        DiceCount = DensityRoll switch
        {
            <= 3 => 6,
            <= 5 => 12,
            _ => 32,
        };

        foreach (var coordinate in RegionalMap.ChooseDistinct(_cells, DiceCount, random))
        {
            var roll = RegionalMap.RollD6(random);
            _diceRolls.Add(coordinate, roll);
            _terrain[coordinate] = parentTerrain switch
            {
                Terrain.Ruins => roll switch
                {
                    1 => Terrain.Wastes,
                    <= 4 => Terrain.Ruins,
                    _ => Terrain.Settlements,
                },
                Terrain.Wastes => roll == 6 ? Terrain.Ruins : Terrain.Wastes,
                Terrain.Pillars => Terrain.Pillars,
                _ => throw new InvalidOperationException("A local map must have a regional terrain parent."),
            };
        }
    }

    public LocalMap(LocalMapState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Parent = new RegionalCoord(state.ParentColumn, state.ParentRow);
        ParentTerrain = state.ParentTerrain;
        DensityRoll = state.DensityRoll;
        DiceCount = state.DiceCount;
        RegionalMap.ValidateDieRoll(DensityRoll);
        if (DiceCount is not (6 or 12 or 32))
        {
            throw new InvalidDataException("A local map must contain 6, 12, or 32 dice.");
        }

        _cells = CreateHexagonalCells(SideLengthInSubhexes);
        _terrain = new Dictionary<HexCoord, Terrain>();
        _diceRolls = new Dictionary<HexCoord, int>();
        foreach (var cellState in state.Cells ?? throw new InvalidDataException("The local map is missing cells."))
        {
            var coordinate = new HexCoord(cellState.Q, cellState.R);
            if (!_cells.Contains(coordinate) || !_terrain.TryAdd(coordinate, cellState.Terrain))
            {
                throw new InvalidDataException("The campaign save contains an invalid local cell.");
            }

            if (cellState.DieRoll is { } roll)
            {
                RegionalMap.ValidateDieRoll(roll);
                _diceRolls.Add(coordinate, roll);
            }
        }

        if (_terrain.Count != _cells.Count || _diceRolls.Count != DiceCount)
        {
            throw new InvalidDataException("The campaign save does not contain a complete local map.");
        }
    }

    public RegionalCoord Parent { get; }
    public Terrain ParentTerrain { get; }
    public int DensityRoll { get; }
    public int DiceCount { get; }
    public IReadOnlyCollection<HexCoord> Cells => _cells;
    public IReadOnlyDictionary<HexCoord, Terrain> TerrainByCell => _terrain;
    public IReadOnlyDictionary<HexCoord, int> DiceRolls => _diceRolls;

    public bool Contains(HexCoord coordinate) => _cells.Contains(coordinate);

    public Terrain GetTerrain(HexCoord coordinate) => _terrain[coordinate];

    public LocalMapState ToState() => new(
        Parent.Column,
        Parent.Row,
        ParentTerrain,
        DensityRoll,
        DiceCount,
        _cells.Select(coordinate => new LocalCellState(
            coordinate.Q,
            coordinate.R,
            _terrain[coordinate],
            _diceRolls.TryGetValue(coordinate, out var roll) ? roll : null))
            .ToList());

    private static HashSet<HexCoord> CreateHexagonalCells(int sideLength)
    {
        if (sideLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sideLength));
        }

        var radius = sideLength - 1;
        var cells = new HashSet<HexCoord>();
        for (var q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                cells.Add(new HexCoord(q, r));
            }
        }

        return cells;
    }
}
