using Godot;
using VastDark.Domain;

namespace VastDark.Presentation;

public partial class MapCanvas : Control
{
    // Match the reference map: white paper, pale-blue hexes, soft blue grid
    // lines, and black outlined location symbols.
    private static readonly Color Background = new("fbfdff");
    private static readonly Color HexFill = new("eef6fc");
    private static readonly Color WastesFill = new("eef6fc");
    private static readonly Color RuinsFill = new("e7f1fa");
    private static readonly Color PillarsFill = new("dbe9f6");
    private static readonly Color SettlementsFill = new("e5f0f9");
    private static readonly Color GridLine = new("b7cce2");
    private static readonly Color RegionalOutline = new("86a9cb");
    private static readonly Color SymbolLine = new("07090d");
    private static readonly Color Selection = new("d6a928");
    private const float RegionalHexRadius = 39f;
    private const float LocalHexRadius = 42f;
    private const float DungeonTileSize = 25f;
    // Both map scales use flat-topped hexes.  Starting at the horizontal axis
    // leaves horizontal sides at the top and bottom, matching the axial centre
    // conversion used by RegionalCentre and LocalCentre.
    private const float FlatTopFirstVertexDegrees = 0f;
    // A local map is drawn inside a six-mile regional hex.  The source grid
    // intentionally extends beyond this boundary so edge subhexes can be
    // clipped instead of producing a stepped, eleven-subhex-wide outline.
    private const float LocalRegionalRadius = LocalHexRadius * LocalMap.SideLengthInSubhexes;

    private MapNavigationService _navigation;
    private Vector2 _pan;
    private float _zoom = 1f;
    private bool _dragging;
    private string _viewKey = string.Empty;
    private RegionalCoord? _selectedRegional;
    private HexCoord? _selectedLocal;
    private GridCoord? _selectedGrid;

    public MapCanvas(MapNavigationService navigation)
    {
        _navigation = navigation;
        MouseFilter = MouseFilterEnum.Stop;
        TooltipText = "Left click: inspect  ·  Middle drag: pan  ·  Wheel: zoom";
    }

    public event Action<string>? CellSelected;

    public HexCoord? SelectedLocalCoordinate => _selectedLocal;

    public void SetNavigation(MapNavigationService navigation)
    {
        _navigation = navigation;
        _viewKey = string.Empty;
        _selectedRegional = null;
        _selectedLocal = null;
        _selectedGrid = null;
        QueueRedraw();
    }

    public void Refresh()
    {
        var nextKey = _navigation.Current.ToString() ?? string.Empty;
        if (_viewKey != nextKey)
        {
            _viewKey = nextKey;
            _pan = GetMapCentre();
            _zoom = 1f;
            _selectedRegional = null;
            _selectedLocal = null;
            _selectedGrid = null;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Background);
        if (Size.X <= 0 || Size.Y <= 0)
        {
            return;
        }

        switch (_navigation.Current)
        {
            case MapLocation.Regional:
                DrawRegionalMap();
                break;
            case MapLocation.Local local:
                DrawLocalMap(_navigation.Campaign.GetLocalMap(local.RegionalCoordinate));
                break;
            case MapLocation.Dungeon dungeon:
                DrawDungeonMap(_navigation.Campaign.Dungeon.GetLevel(dungeon.Depth));
                break;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Middle)
            {
                _dragging = mouseButton.Pressed;
                return;
            }

            if (mouseButton.Pressed && mouseButton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                ZoomAround(mouseButton.Position, mouseButton.ButtonIndex == MouseButton.WheelUp ? 1.15f : 1f / 1.15f);
                return;
            }

            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                SelectAt(mouseButton.Position);
            }
        }
        else if (@event is InputEventMouseMotion motion && _dragging)
        {
            _pan -= motion.Relative / _zoom;
            QueueRedraw();
        }
    }

    private void DrawRegionalMap()
    {
        foreach (var cell in _navigation.Campaign.Regional.Cells)
        {
            var isDungeon = cell == Campaign.DungeonRegionalCoordinate;
            DrawHex(RegionalCentre(cell), RegionalHexRadius, _navigation.Campaign.Regional.GetTerrain(cell), _selectedRegional == cell, isDungeon ? new Color("d97735") : null);
            if (cell == _navigation.Campaign.PartyTravel.RegionalCoordinate)
            {
                DrawPartyMarker(RegionalCentre(cell), RegionalHexRadius);
            }
        }
    }

    private void DrawLocalMap(LocalMap map)
    {
        var regionalBoundary = CreateFlatTopHex(Vector2.Zero, LocalRegionalRadius);
        foreach (var cell in map.Cells)
        {
            var isEntrance = map.Parent == Campaign.DungeonRegionalCoordinate && cell == Campaign.DungeonLocalCoordinate;
            DrawClippedHex(
                LocalCentre(cell),
                LocalHexRadius,
                regionalBoundary,
                map.GetTerrain(cell),
                map.VisibleCells.Contains(cell),
                _selectedLocal == cell,
                isEntrance ? new Color("d97735") : null);
        }

        foreach (var (coordinate, dieRoll) in map.RoamingHazards)
        {
            if (map.VisibleCells.Contains(coordinate))
            {
                DrawRoamingHazard(LocalCentre(coordinate), dieRoll);
            }
        }

        var partyTravel = _navigation.Campaign.PartyTravel;
        if (map.Parent == partyTravel.RegionalCoordinate && map.VisibleCells.Contains(partyTravel.LocalCoordinate))
        {
            DrawPartyMarker(LocalCentre(partyTravel.LocalCoordinate), LocalHexRadius);
        }

        DrawPolygonBorder(regionalBoundary, RegionalOutline, Math.Max(2f, _zoom * 2f));
    }

    private void DrawDungeonMap(DungeonLevel level)
    {
        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var coordinate = new GridCoord(x, y);
                var colour = level.GetTile(coordinate) switch
                {
                    DungeonTile.Wall => new Color("24303d"),
                    DungeonTile.Floor => new Color("6d6860"),
                    DungeonTile.StairUp => new Color("67c5b9"),
                    DungeonTile.StairDown => new Color("d97735"),
                    _ => Colors.Magenta,
                };
                var topLeft = ToScreen(new Vector2(x * DungeonTileSize, y * DungeonTileSize));
                var tileSize = new Vector2(DungeonTileSize, DungeonTileSize) * _zoom;
                var rectangle = new Rect2(topLeft, tileSize);
                DrawRect(rectangle, colour);
                DrawRect(rectangle, GridLine, false, Math.Max(1f, _zoom));
                if (_selectedGrid == coordinate)
                {
                    DrawRect(rectangle.Grow(2f), Selection, false, 3f);
                }
            }
        }
    }

    private void DrawHex(Vector2 worldCentre, float radius, Terrain terrain, bool selected, Color? marker)
    {
        var worldPoints = CreateFlatTopHex(worldCentre, radius);
        var points = worldPoints.Select(ToScreen).ToArray();

        DrawColoredPolygon(points, TerrainFill(terrain));
        DrawPolygonBorder(worldPoints, GridLine, Math.Max(1f, _zoom));
        DrawTerrainMarker(worldCentre, radius, terrain);
        if (marker is not null)
        {
            DrawCircle(ToScreen(worldCentre), 7f * _zoom, marker.Value);
        }

        if (selected)
        {
            DrawPolygonBorder(worldPoints, Selection, 3f);
        }
    }

    private void DrawClippedHex(
        Vector2 worldCentre,
        float radius,
        IReadOnlyList<Vector2> boundary,
        Terrain terrain,
        bool showTerrainMarker,
        bool selected,
        Color? marker)
    {
        var clippedPoints = ClipConvexPolygon(CreateFlatTopHex(worldCentre, radius), boundary);
        if (clippedPoints.Count < 3)
        {
            return;
        }

        DrawColoredPolygon(clippedPoints.Select(ToScreen).ToArray(), TerrainFill(terrain));
        DrawPolygonBorder(clippedPoints, GridLine, Math.Max(1f, _zoom));
        var visibleCentre = VisiblePolygonCentre(clippedPoints);
        if (showTerrainMarker)
        {
            DrawTerrainMarker(visibleCentre, radius, terrain);
        }
        if (marker is not null)
        {
            DrawCircle(ToScreen(visibleCentre), 7f * _zoom, marker.Value);
        }

        if (selected)
        {
            DrawPolygonBorder(clippedPoints, Selection, 3f);
        }
    }

    private void DrawRoamingHazard(Vector2 worldCentre, int dieRoll)
    {
        var centre = ToScreen(worldCentre);
        var radius = 13f * _zoom;
        DrawCircle(centre, radius, SymbolLine);
        DrawArc(centre, radius, 0f, Mathf.Tau, 24, Colors.White, Math.Max(1f, _zoom), true);
        DrawString(
            ThemeDB.FallbackFont,
            centre + new Vector2(-5f * _zoom, 5.5f * _zoom),
            dieRoll.ToString(),
            HorizontalAlignment.Left,
            -1f,
            Mathf.RoundToInt(15f * _zoom),
            Colors.White);
    }

    private void DrawPartyMarker(Vector2 worldCentre, float hexRadius)
    {
        var centre = ToScreen(worldCentre);
        var radius = Math.Max(8f, hexRadius * 0.22f) * _zoom;
        var fill = new Color("218c74");
        DrawCircle(centre, radius, fill);
        DrawArc(centre, radius, 0f, Mathf.Tau, 24, Colors.White, Math.Max(1.5f, _zoom * 2f), true);
        DrawString(
            ThemeDB.FallbackFont,
            centre + new Vector2(-4.5f * _zoom, 5.5f * _zoom),
            "P",
            HorizontalAlignment.Left,
            -1f,
            Mathf.RoundToInt(14f * _zoom),
            Colors.White);
    }

    private static Vector2[] CreateFlatTopHex(Vector2 centre, float radius)
    {
        var points = new Vector2[6];
        for (var point = 0; point < points.Length; point++)
        {
            var angle = Mathf.DegToRad(FlatTopFirstVertexDegrees + point * 60f);
            points[point] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        return points;
    }

    private static List<Vector2> ClipConvexPolygon(
        IReadOnlyList<Vector2> subject,
        IReadOnlyList<Vector2> boundary)
    {
        var output = subject.ToList();
        for (var edge = 0; edge < boundary.Count && output.Count > 0; edge++)
        {
            var edgeStart = boundary[edge];
            var edgeEnd = boundary[(edge + 1) % boundary.Count];
            var input = output;
            output = [];

            var previous = input[^1];
            var previousInside = IsInsideBoundaryEdge(previous, edgeStart, edgeEnd);
            foreach (var current in input)
            {
                var currentInside = IsInsideBoundaryEdge(current, edgeStart, edgeEnd);
                if (currentInside != previousInside)
                {
                    output.Add(IntersectBoundaryEdge(previous, current, edgeStart, edgeEnd));
                }

                if (currentInside)
                {
                    output.Add(current);
                }

                previous = current;
                previousInside = currentInside;
            }
        }

        return output;
    }

    private static Vector2 VisiblePolygonCentre(IReadOnlyList<Vector2> points)
    {
        var total = Vector2.Zero;
        foreach (var point in points)
        {
            total += point;
        }

        return total / points.Count;
    }

    private static bool IsInsideBoundaryEdge(Vector2 point, Vector2 edgeStart, Vector2 edgeEnd) =>
        Cross(edgeEnd - edgeStart, point - edgeStart) >= -0.001f;

    private static Vector2 IntersectBoundaryEdge(Vector2 start, Vector2 end, Vector2 edgeStart, Vector2 edgeEnd)
    {
        var startCross = Cross(edgeEnd - edgeStart, start - edgeStart);
        var endCross = Cross(edgeEnd - edgeStart, end - edgeStart);
        var ratio = startCross / (startCross - endCross);
        return start + (end - start) * ratio;
    }

    private void DrawPolygonBorder(IReadOnlyList<Vector2> worldPoints, Color colour, float width)
    {
        var border = worldPoints.Select(ToScreen).Append(ToScreen(worldPoints[0])).ToArray();
        DrawPolyline(border, colour, width);
    }

    private void DrawTerrainMarker(Vector2 worldCentre, float hexRadius, Terrain terrain)
    {
        if (terrain is not (Terrain.Ruins or Terrain.Settlements))
        {
            return;
        }

        var size = hexRadius * 0.64f;
        Vector2 Point(float x, float y) => ToScreen(worldCentre + new Vector2(x * size, y * size));
        void Stroke(params Vector2[] points) => DrawPolyline(points, SymbolLine, Math.Max(1.5f, 2.25f * _zoom), true);

        // Three uneven outlined buildings reproduce the compact ruin glyph in
        // the reference.  Settlements use the same glyph plus a left flag.
        Stroke(Point(-0.39f, 0.30f), Point(-0.39f, -0.04f), Point(-0.22f, -0.04f), Point(-0.22f, 0.30f));
        Stroke(Point(-0.14f, 0.30f), Point(-0.14f, -0.35f), Point(0.06f, -0.35f), Point(0.06f, 0.30f));
        Stroke(Point(0.14f, 0.30f), Point(0.14f, -0.17f), Point(0.34f, -0.17f), Point(0.34f, 0.30f));
        Stroke(Point(-0.44f, 0.30f), Point(0.39f, 0.30f));

        if (terrain == Terrain.Settlements)
        {
            Stroke(Point(-0.46f, 0.30f), Point(-0.46f, -0.37f));
            Stroke(Point(-0.46f, -0.37f), Point(-0.70f, -0.28f), Point(-0.46f, -0.16f));
        }
    }

    private static float Cross(Vector2 left, Vector2 right) => left.X * right.Y - left.Y * right.X;

    private void SelectAt(Vector2 screenPosition)
    {
        switch (_navigation.Current)
        {
            case MapLocation.Regional:
                SelectRegional(screenPosition);
                break;
            case MapLocation.Local local:
                SelectLocal(screenPosition, _navigation.Campaign.GetLocalMap(local.RegionalCoordinate));
                break;
            case MapLocation.Dungeon dungeon:
                SelectDungeon(screenPosition, _navigation.Campaign.Dungeon.GetLevel(dungeon.Depth));
                break;
        }

        QueueRedraw();
    }

    private void SelectRegional(Vector2 screenPosition)
    {
        var selected = _navigation.Campaign.Regional.Cells
            .Select(cell => (Cell: cell, Distance: ToScreen(RegionalCentre(cell)).DistanceTo(screenPosition)))
            .Where(candidate => candidate.Distance <= RegionalHexRadius * _zoom)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => (RegionalCoord?)candidate.Cell)
            .FirstOrDefault();

        if (selected is null)
        {
            return;
        }

        _selectedRegional = selected;
        _navigation.SelectRegional(selected.Value);
        var terrain = _navigation.Campaign.Regional.GetTerrain(selected.Value);
        CellSelected?.Invoke($"Regional hex {selected.Value}\n\nTerrain: {terrain}\nScale: 6 miles per hex.\n\nThis regional hex owns one local map.");
    }

    private void SelectLocal(Vector2 screenPosition, LocalMap map)
    {
        var selected = map.Cells
            .Select(cell => (Cell: cell, Distance: ToScreen(LocalCentre(cell)).DistanceTo(screenPosition)))
            .Where(candidate => candidate.Distance <= LocalHexRadius * _zoom)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => (HexCoord?)candidate.Cell)
            .FirstOrDefault();

        if (selected is null)
        {
            return;
        }

        _selectedLocal = selected;
        var isEntrance = map.Parent == Campaign.DungeonRegionalCoordinate && selected == Campaign.DungeonLocalCoordinate;
        var terrain = map.GetTerrain(selected.Value);
        var hazardText = map.RoamingHazards.TryGetValue(selected.Value, out var dieRoll)
            ? $"\n\nRoaming hazard: {LocalMap.GetRoamingHazardName(dieRoll)} (d6: {dieRoll})."
            : string.Empty;
        CellSelected?.Invoke($"Local subhex {selected.Value}\n\nTerrain: {terrain}\nScale: 1 mile per subhex.\nLocal footprint: 6 subhexes along each regional-hex edge (12 flat-to-flat).{hazardText}{(isEntrance ? "\n\nOrange marker: dungeon entrance." : string.Empty)}");
    }

    private void SelectDungeon(Vector2 screenPosition, DungeonLevel level)
    {
        var worldPosition = ToWorld(screenPosition);
        var coordinate = new GridCoord(
            Mathf.FloorToInt(worldPosition.X / DungeonTileSize),
            Mathf.FloorToInt(worldPosition.Y / DungeonTileSize));
        if (!level.Contains(coordinate))
        {
            return;
        }

        _selectedGrid = coordinate;
        CellSelected?.Invoke($"Dungeon depth {level.Depth}, grid cell {coordinate}\n\nTile: {level.GetTile(coordinate)}");
    }

    private void ZoomAround(Vector2 screenPosition, float factor)
    {
        var anchor = ToWorld(screenPosition);
        _zoom = Mathf.Clamp(_zoom * factor, 0.35f, 3f);
        _pan += anchor - ToWorld(screenPosition);
        QueueRedraw();
    }

    private Vector2 GetMapCentre() => _navigation.Current switch
    {
        MapLocation.Regional => RegionalCentre(new RegionalCoord(RegionalMap.Width / 2, RegionalMap.Height / 2)),
        MapLocation.Local => Vector2.Zero,
        MapLocation.Dungeon dungeon => new Vector2(
            _navigation.Campaign.Dungeon.GetLevel(dungeon.Depth).Width * DungeonTileSize / 2f,
            _navigation.Campaign.Dungeon.GetLevel(dungeon.Depth).Height * DungeonTileSize / 2f),
        _ => Vector2.Zero,
    };

    private static Vector2 RegionalCentre(RegionalCoord coordinate) => new(
        coordinate.Column * RegionalHexRadius * 1.5f,
        (coordinate.Row + (coordinate.Column % 2) * 0.5f) * RegionalHexRadius * Mathf.Sqrt(3f));

    private static Vector2 LocalCentre(HexCoord coordinate) => new(
        coordinate.Q * LocalHexRadius * 1.5f,
        (coordinate.R + coordinate.Q * 0.5f) * LocalHexRadius * Mathf.Sqrt(3f));

    private static Color TerrainFill(Terrain terrain) => terrain switch
    {
        Terrain.Wastes => WastesFill,
        Terrain.Ruins => RuinsFill,
        Terrain.Pillars => PillarsFill,
        Terrain.Settlements => SettlementsFill,
        _ => HexFill,
    };

    private Vector2 ToScreen(Vector2 worldPosition) => (worldPosition - _pan) * _zoom + Size / 2f;
    private Vector2 ToWorld(Vector2 screenPosition) => (screenPosition - Size / 2f) / _zoom + _pan;
}
