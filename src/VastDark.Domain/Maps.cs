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
            _terrain[coordinate] = GetTerrainForRoll(roll);
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

    public RegionalCoord? GetNeighbour(RegionalCoord coordinate, int direction)
    {
        if (!Contains(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        if (direction < 0 || direction >= HexCoord.Directions.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        var axialRow = coordinate.Row - FloorDivide(coordinate.Column, 2);
        var offset = HexCoord.Directions[direction];
        var column = coordinate.Column + offset.Q;
        var row = axialRow + offset.R + FloorDivide(column, 2);
        var neighbour = new RegionalCoord(column, row);
        return Contains(neighbour) ? neighbour : null;
    }

    public Terrain GetTerrain(RegionalCoord coordinate) => _terrain[coordinate];

    public static Terrain GetTerrainForRoll(int roll)
    {
        ValidateDieRoll(roll);
        return roll switch
        {
            1 => Terrain.Wastes,
            <= 4 => Terrain.Ruins,
            _ => Terrain.Pillars,
        };
    }

    public RegionalCellState[] ToState() => _cells
        .Select(coordinate => new RegionalCellState(
            coordinate.Column,
            coordinate.Row,
            _terrain[coordinate],
            _diceRolls.TryGetValue(coordinate, out var roll) ? roll : null))
        .ToArray();

    internal static int RollD6(Random random) => random.Next(1, 7);

    private static int FloorDivide(int value, int divisor) =>
        value >= 0 ? value / divisor : -(((-value) + divisor - 1) / divisor);

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
    private readonly HashSet<HexCoord> _visibleCells;
    private readonly Dictionary<HexCoord, Terrain> _terrain;
    private readonly Dictionary<HexCoord, int> _diceRolls;
    private readonly Dictionary<HexCoord, int> _roamingHazards;

    public LocalMap(RegionalCoord parent, Terrain parentTerrain = Terrain.Wastes, Random? random = null)
    {
        random ??= Random.Shared;
        Parent = parent;
        ParentTerrain = parentTerrain;
        _cells = CreateHexagonalCells(SideLengthInSubhexes);
        _visibleCells = _cells.Where(IsFullyVisibleCell).ToHashSet();
        _terrain = _cells.ToDictionary(cell => cell, _ => parentTerrain == Terrain.Pillars ? Terrain.Pillars : Terrain.Wastes);
        _diceRolls = new Dictionary<HexCoord, int>();
        _roamingHazards = new Dictionary<HexCoord, int>();
        DensityRoll = RegionalMap.RollD6(random);
        DiceCount = GetVisibleDiceCount(DensityRoll, _visibleCells.Count);

        foreach (var coordinate in RegionalMap.ChooseDistinct(_visibleCells, DiceCount, random))
        {
            var roll = RegionalMap.RollD6(random);
            _diceRolls.Add(coordinate, roll);
            ApplyTerrainRoll(coordinate, roll);
        }

        CreateRoamingHazards(random);
    }

    public LocalMap(LocalMapState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Parent = new RegionalCoord(state.ParentColumn, state.ParentRow);
        ParentTerrain = state.ParentTerrain;
        DensityRoll = state.DensityRoll;
        RegionalMap.ValidateDieRoll(DensityRoll);
        if (state.DiceCount is not (6 or 12 or 32))
        {
            throw new InvalidDataException("A local map contains an invalid number of terrain dice.");
        }

        _cells = CreateHexagonalCells(SideLengthInSubhexes);
        _visibleCells = _cells.Where(IsFullyVisibleCell).ToHashSet();
        _terrain = new Dictionary<HexCoord, Terrain>();
        _diceRolls = new Dictionary<HexCoord, int>();
        _roamingHazards = new Dictionary<HexCoord, int>();
        var savedDiceRolls = new Dictionary<HexCoord, int>();
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
                savedDiceRolls.Add(coordinate, roll);
            }
        }

        DiceCount = GetVisibleDiceCount(DensityRoll, _visibleCells.Count);
        if (_terrain.Count != _cells.Count || savedDiceRolls.Count < DiceCount)
        {
            throw new InvalidDataException("The campaign save does not contain a complete local map.");
        }

        // Older saves could place terrain dice in clipped edge cells. Rebuild
        // their terrain rolls into fully visible cells before displaying them.
        foreach (var coordinate in _cells)
        {
            _terrain[coordinate] = BaseTerrain;
        }

        foreach (var (coordinate, roll) in savedDiceRolls.Where(die => _visibleCells.Contains(die.Key)).OrderBy(die => die.Key.Q).ThenBy(die => die.Key.R))
        {
            if (_diceRolls.Count == DiceCount)
            {
                break;
            }

            _diceRolls.Add(coordinate, roll);
            ApplyTerrainRoll(coordinate, roll);
        }

        foreach (var roll in savedDiceRolls.Where(die => !_visibleCells.Contains(die.Key)).OrderBy(die => die.Key.Q).ThenBy(die => die.Key.R).Select(die => die.Value))
        {
            if (_diceRolls.Count == DiceCount)
            {
                break;
            }

            var coordinate = _visibleCells.Where(cell => !_diceRolls.ContainsKey(cell)).OrderBy(cell => cell.Q).ThenBy(cell => cell.R).First();
            _diceRolls.Add(coordinate, roll);
            ApplyTerrainRoll(coordinate, roll);
        }

        RoamingHazardDay = state.RoamingHazardDay;
        if (RoamingHazardDay < 0)
        {
            throw new InvalidDataException("The roaming hazard day cannot be negative.");
        }

        if (state.RoamingHazards is null)
        {
            CreateRoamingHazards(Random.Shared);
        }
        else
        {
            foreach (var hazardState in state.RoamingHazards)
            {
                var coordinate = new HexCoord(hazardState.Q, hazardState.R);
                if (!_cells.Contains(coordinate))
                {
                    throw new InvalidDataException("The campaign save contains an invalid roaming hazard.");
                }

                RegionalMap.ValidateDieRoll(hazardState.DieRoll);
                var target = _visibleCells.Contains(coordinate) && !_roamingHazards.ContainsKey(coordinate)
                    ? coordinate
                    : ChooseUnoccupiedCell(_roamingHazards.Keys.ToHashSet(), Random.Shared);
                _roamingHazards.Add(target, hazardState.DieRoll);
            }

            if (_roamingHazards.Count is < 1 or > 6)
            {
                throw new InvalidDataException("A local map must contain between one and six roaming hazards.");
            }
        }
    }

    public RegionalCoord Parent { get; }
    public Terrain ParentTerrain { get; }
    public int DensityRoll { get; }
    public int DiceCount { get; }
    public int RoamingHazardDay { get; private set; }
    public IReadOnlyCollection<HexCoord> Cells => _cells;
    public IReadOnlyCollection<HexCoord> VisibleCells => _visibleCells;
    public IReadOnlyDictionary<HexCoord, Terrain> TerrainByCell => _terrain;
    public IReadOnlyDictionary<HexCoord, int> DiceRolls => _diceRolls;
    public IReadOnlyDictionary<HexCoord, int> RoamingHazards => _roamingHazards;

    public bool Contains(HexCoord coordinate) => _cells.Contains(coordinate);

    public Terrain GetTerrain(HexCoord coordinate) => _terrain[coordinate];

    private Terrain BaseTerrain => ParentTerrain == Terrain.Pillars ? Terrain.Pillars : Terrain.Wastes;

    public void AdvanceRoamingHazards(Random? random = null)
    {
        random ??= Random.Shared;
        var occupied = new HashSet<HexCoord>(_roamingHazards.Keys);
        var movedHazards = new Dictionary<HexCoord, int>();

        foreach (var (origin, dieRoll) in _roamingHazards.OrderBy(hazard => hazard.Key.Q).ThenBy(hazard => hazard.Key.R))
        {
            occupied.Remove(origin);
            var target = origin.Neighbour(random.Next(0, HexCoord.Directions.Count));
            if (!_visibleCells.Contains(target) || occupied.Contains(target))
            {
                target = ChooseUnoccupiedCell(occupied, random);
            }

            occupied.Add(target);
            movedHazards.Add(target, dieRoll);
        }

        _roamingHazards.Clear();
        foreach (var (coordinate, dieRoll) in movedHazards)
        {
            _roamingHazards.Add(coordinate, dieRoll);
        }

        RoamingHazardDay++;
    }

    public static string GetRoamingHazardName(int dieRoll)
    {
        RegionalMap.ValidateDieRoll(dieRoll);
        return dieRoll switch
        {
            1 => "Warband",
            2 => "Maelstrom",
            3 => "Crawlherd",
            4 => "Collapse",
            5 => "Void Lightning",
            6 => "Singing Sand",
            _ => throw new InvalidOperationException(),
        };
    }

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
            .ToList(),
        _roamingHazards.Select(hazard => new RoamingHazardState(hazard.Key.Q, hazard.Key.R, hazard.Value)).ToList(),
        RoamingHazardDay);

    private void CreateRoamingHazards(Random random)
    {
        var hazardCount = RegionalMap.RollD6(random);
        foreach (var coordinate in RegionalMap.ChooseDistinct(_visibleCells, hazardCount, random))
        {
            _roamingHazards.Add(coordinate, RegionalMap.RollD6(random));
        }
    }

    private HexCoord ChooseUnoccupiedCell(IReadOnlySet<HexCoord> occupied, Random random)
    {
        var candidates = _visibleCells.Where(cell => !occupied.Contains(cell)).ToArray();
        if (candidates.Length == 0)
        {
            throw new InvalidOperationException("No local cell is available to re-drop a roaming hazard.");
        }

        return candidates[random.Next(candidates.Length)];
    }

    private void ApplyTerrainRoll(HexCoord coordinate, int roll)
    {
        _terrain[coordinate] = GetTerrainForRoll(ParentTerrain, roll);
    }

    public static Terrain GetTerrainForRoll(Terrain parentTerrain, int roll)
    {
        RegionalMap.ValidateDieRoll(roll);
        return parentTerrain switch
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

    public static int GetRequestedDiceCountForDensityRoll(int densityRoll)
    {
        RegionalMap.ValidateDieRoll(densityRoll);
        return densityRoll switch
        {
            <= 3 => 6,
            <= 5 => 12,
            _ => 32,
        };
    }

    private static int GetVisibleDiceCount(int densityRoll, int visibleCellCount)
    {
        var requestedDiceCount = GetRequestedDiceCountForDensityRoll(densityRoll);

        return Math.Min(requestedDiceCount, visibleCellCount);
    }

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

    private static bool IsFullyVisibleCell(HexCoord coordinate)
    {
        const double subhexRadius = 1d;
        var centreX = coordinate.Q * 1.5d * subhexRadius;
        var centreY = (coordinate.R + coordinate.Q * 0.5d) * Math.Sqrt(3d) * subhexRadius;
        var mapRadius = SideLengthInSubhexes * subhexRadius;

        for (var vertex = 0; vertex < 6; vertex++)
        {
            var angle = vertex * Math.PI / 3d;
            var x = centreX + Math.Cos(angle) * subhexRadius;
            var y = centreY + Math.Sin(angle) * subhexRadius;
            if (Math.Abs(x) + Math.Abs(y) / Math.Sqrt(3d) > mapRadius + 0.0001d)
            {
                return false;
            }
        }

        return true;
    }
}
