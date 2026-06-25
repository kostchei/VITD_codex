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
    private static readonly Color WastesTexture = new(0.58f, 0.70f, 0.80f, 0.35f);
    private static readonly Color RuinsTexture = new(0.38f, 0.48f, 0.58f, 0.38f);
    private static readonly Color PillarsTexture = new(0.08f, 0.13f, 0.20f, 0.32f);
    private static readonly Color SettlementsTexture = new(0.74f, 0.52f, 0.18f, 0.42f);
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
    private MapLocation? _lastLocation;
    private RegionalCoord? _selectedRegional;
    private IReadOnlyList<RegionalCoord> _previewRegionalPath = [];
    private LocalMapCoord? _selectedLocal;
    private GridCoord? _selectedGrid;
    private IReadOnlyList<LocalMapCoord> _previewLocalPath = [];

    public MapCanvas(MapNavigationService navigation)
    {
        _navigation = navigation;
        MouseFilter = MouseFilterEnum.Stop;
        TooltipText = "Left click: inspect  ·  Middle drag: pan  ·  Wheel: zoom";
    }

    public event Action<string>? CellSelected;
    public event Action<IReadOnlyList<LocalMapCoord>>? PartyPathRequested;
    public event Action<IReadOnlyList<RegionalCoord>>? RegionalPathRequested;

    public LocalMapCoord? SelectedLocalCoordinate => _selectedLocal;
    public GridCoord? SelectedRuinRoom => _selectedGrid;
    public IReadOnlyList<LocalMapCoord> PreviewLocalPath => _previewLocalPath;
    public IReadOnlyList<RegionalCoord> PreviewRegionalPath => _previewRegionalPath;

    public void SetNavigation(MapNavigationService navigation)
    {
        _navigation = navigation;
        _viewKey = string.Empty;
        _lastLocation = null;
        _selectedRegional = null;
        _previewRegionalPath = [];
        _selectedLocal = null;
        _selectedGrid = null;
        _previewLocalPath = [];
        QueueRedraw();
    }

    public void Refresh()
    {
        var nextKey = _navigation.Current.ToString() ?? string.Empty;
        var preserveLocalViewport = _lastLocation is MapLocation.Local && _navigation.Current is MapLocation.Local;
        if (_viewKey != nextKey && !preserveLocalViewport)
        {
            _viewKey = nextKey;
            _pan = GetMapCentre();
            _zoom = 1f;
            _selectedRegional = null;
            _selectedLocal = null;
            _selectedGrid = null;
        }
        else
        {
            _viewKey = nextKey;
        }

        _lastLocation = _navigation.Current;

        QueueRedraw();
    }

    public void RecentreOnParty()
    {
        _pan = _navigation.Current switch
        {
            MapLocation.Local => LocalWorldCentre(
                _navigation.Campaign.PartyTravel.RegionalCoordinate,
                _navigation.Campaign.PartyTravel.LocalCoordinate),
            MapLocation.Regional => RegionalCentre(_navigation.Campaign.PartyTravel.RegionalCoordinate),
            _ => _pan,
        };
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
                DrawLocalArea(local.RegionalCoordinate);
                break;
            case MapLocation.Dungeon dungeon:
                DrawRuinMap(_navigation.Campaign.Ruin);
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
            else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Right && _navigation.Current is MapLocation.Local local)
            {
                SelectLocal(mouseButton.Position, local.RegionalCoordinate);
                if (_previewLocalPath.Count > 1 && _selectedLocal == _previewLocalPath[^1]) PartyPathRequested?.Invoke(_previewLocalPath);
            }
            else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Right && _navigation.Current is MapLocation.Regional)
            {
                SelectRegional(mouseButton.Position);
                if (_previewRegionalPath.Count > 1 && _selectedRegional == _previewRegionalPath[^1]) RegionalPathRequested?.Invoke(_previewRegionalPath);
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

    private void DrawLocalArea(RegionalCoord centre)
    {
        foreach (var regionalCoordinate in _navigation.Campaign.GetLocalArea(centre))
        {
            var map = _navigation.Campaign.GetLocalMap(regionalCoordinate);
            var regionalBoundary = CreateFlatTopHex(ChunkCentre(regionalCoordinate), LocalRegionalRadius);
            foreach (var cell in map.Cells)
            {
                var location = new LocalMapCoord(regionalCoordinate, cell);
                var isEntrance = map.Parent == Campaign.DungeonRegionalCoordinate && cell == Campaign.DungeonLocalCoordinate;
                DrawClippedHex(
                    LocalWorldCentre(regionalCoordinate, cell),
                    LocalHexRadius,
                    regionalBoundary,
                    map.GetTerrain(cell),
                    map.VisibleCells.Contains(cell),
                    _selectedLocal == location,
                    isEntrance ? new Color("d97735") : null);
            }

            foreach (var (coordinate, dieRoll) in map.RoamingHazards)
            {
                if (map.VisibleCells.Contains(coordinate))
                {
                    DrawRoamingHazard(LocalWorldCentre(regionalCoordinate, coordinate), dieRoll);
                }
            }

            DrawPolygonBorder(regionalBoundary, RegionalOutline, Math.Max(2f, _zoom * 2f));
        }

        var partyTravel = _navigation.Campaign.PartyTravel;
        if (_previewLocalPath.Count > 1)
        {
            DrawPolyline(_previewLocalPath.Select(step => ToScreen(LocalWorldCentre(step.RegionalCoordinate, step.LocalCoordinate))).ToArray(), Selection, Math.Max(3f, _zoom * 3f));
        }
        if (_previewRegionalPath.Count > 1) DrawPolyline(_previewRegionalPath.Select(cell => ToScreen(RegionalCentre(cell))).ToArray(), Selection, Math.Max(3f, _zoom * 3f));
        DrawPartyMarker(LocalWorldCentre(partyTravel.RegionalCoordinate, partyTravel.LocalCoordinate), LocalHexRadius);
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

    private void DrawRuinMap(RuinExploration ruin)
    {
        var rooms = ruin.Layout.Rooms.ToDictionary(room => room.Coordinate, room => new Vector2(room.Coordinate.X * 120f, room.Coordinate.Y * 100f));
        foreach (var passage in ruin.Layout.Passages)
        {
            DrawLine(ToScreen(rooms[passage.From]), ToScreen(rooms[passage.To]), RegionalOutline, 3f * _zoom);
        }
        foreach (var room in ruin.Layout.Rooms)
        {
            var centre = ToScreen(rooms[room.Coordinate]);
            var visited = ruin.VisitedRooms.Contains(room.Coordinate);
            var color = room.Coordinate == ruin.CurrentRoom ? new Color("d97735") : visited ? new Color("67c5b9") : RuinsFill;
            DrawCircle(centre, 24f * _zoom, color);
            DrawArc(centre, 24f * _zoom, 0, Mathf.Tau, 24, GridLine, 2f * _zoom);
            DrawString(ThemeDB.FallbackFont, centre + new Vector2(-8, 6) * _zoom, room.SourceFaceIndex.ToString(), HorizontalAlignment.Center, 16 * _zoom, (int)(14 * _zoom), SymbolLine);
            if (_selectedGrid == room.Coordinate) DrawArc(centre, 29f * _zoom, 0, Mathf.Tau, 24, Selection, 3f);
        }
    }

    private void DrawHex(Vector2 worldCentre, float radius, Terrain terrain, bool selected, Color? marker)
    {
        var worldPoints = CreateFlatTopHex(worldCentre, radius);
        var points = worldPoints.Select(ToScreen).ToArray();

        DrawTerrainPolygon(points, terrain, radius);
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

        var screenPoints = clippedPoints.Select(ToScreen).ToArray();
        DrawTerrainPolygon(screenPoints, terrain, radius);
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
        var radius = 14f * _zoom;
        var inner = radius * 0.58f;
        var accent = HazardColor(dieRoll);
        DrawCircle(centre, radius, new Color("0f172a"));
        DrawArc(centre, radius, 0f, Mathf.Tau, 32, accent, Math.Max(2f, 2.4f * _zoom), true);

        switch (dieRoll)
        {
            case 1:
                DrawLine(centre + new Vector2(-inner, -inner), centre + new Vector2(inner, inner), accent, Math.Max(1.5f, 2f * _zoom), true);
                DrawLine(centre + new Vector2(inner, -inner), centre + new Vector2(-inner, inner), accent, Math.Max(1.5f, 2f * _zoom), true);
                DrawLine(centre + new Vector2(-inner * 0.5f, 0f), centre + new Vector2(inner * 0.5f, 0f), accent, Math.Max(1.2f, 1.5f * _zoom), true);
                break;
            case 2:
                var spiral = Enumerable.Range(0, 13)
                    .Select(index =>
                    {
                        var angle = index * 0.75f;
                        var distance = inner * index / 12f;
                        return centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    })
                    .ToArray();
                DrawPolyline(spiral, accent, Math.Max(1.5f, 2f * _zoom), true);
                break;
            case 3:
                DrawCircle(centre + new Vector2(-inner * 0.42f, -inner * 0.15f), Math.Max(1.8f, 2.5f * _zoom), accent);
                DrawCircle(centre + new Vector2(inner * 0.35f, -inner * 0.30f), Math.Max(1.8f, 2.5f * _zoom), accent);
                DrawCircle(centre + new Vector2(-inner * 0.15f, inner * 0.35f), Math.Max(1.8f, 2.5f * _zoom), accent);
                DrawCircle(centre + new Vector2(inner * 0.25f, inner * 0.15f), Math.Max(1.8f, 3f * _zoom), accent);
                break;
            case 4:
                var rubble = new[]
                {
                    centre + new Vector2(0f, -inner),
                    centre + new Vector2(inner * 0.82f, inner * 0.62f),
                    centre + new Vector2(-inner * 0.82f, inner * 0.62f),
                    centre + new Vector2(0f, -inner),
                };
                DrawPolyline(rubble, accent, Math.Max(1.5f, 2f * _zoom), true);
                DrawLine(centre + new Vector2(-inner * 0.40f, inner * 0.22f), centre + new Vector2(inner * 0.40f, inner * 0.22f), accent, Math.Max(1.2f, 1.5f * _zoom), true);
                break;
            case 5:
                var bolt = new[]
                {
                    centre + new Vector2(inner * 0.35f, -inner),
                    centre + new Vector2(-inner * 0.25f, -inner * 0.05f),
                    centre + new Vector2(inner * 0.18f, -inner * 0.05f),
                    centre + new Vector2(-inner * 0.25f, inner),
                };
                DrawPolyline(bolt, accent, Math.Max(1.5f, 2.2f * _zoom), true);
                break;
            case 6:
                for (var row = -1; row <= 1; row++)
                {
                    var y = centre.Y + row * inner * 0.46f;
                    var wave = Enumerable.Range(0, 7)
                        .Select(index =>
                        {
                            var x = centre.X - inner + inner * 2f * index / 6f;
                            var offset = Mathf.Sin(index * 1.3f) * 2.5f * _zoom;
                            return new Vector2(x, y + offset);
                        })
                        .ToArray();
                    DrawPolyline(wave, accent, Math.Max(1.2f, 1.5f * _zoom), true);
                }
                break;
        }

        DrawScreenTextCentered(dieRoll.ToString(), centre, Mathf.RoundToInt(8f * _zoom), Colors.White);
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

    private void DrawMovementOption(Vector2 worldCentre)
    {
        var centre = ToScreen(worldCentre);
        DrawArc(centre, 10f * _zoom, 0f, Mathf.Tau, 20, new Color("d6a928"), Math.Max(1.5f, _zoom * 2f), true);
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
        if (terrain == Terrain.Pillars)
        {
            var pillarSize = hexRadius * 0.58f;
            Vector2 PillarPoint(float x, float y) => ToScreen(worldCentre + new Vector2(x * pillarSize, y * pillarSize));
            void PillarStroke(params Vector2[] points) => DrawPolyline(points, SymbolLine, Math.Max(1.5f, 2.25f * _zoom), true);

            PillarStroke(PillarPoint(-0.34f, 0.42f), PillarPoint(-0.18f, -0.42f), PillarPoint(-0.02f, 0.42f));
            PillarStroke(PillarPoint(0.08f, 0.42f), PillarPoint(0.18f, -0.34f), PillarPoint(0.34f, 0.42f));
            PillarStroke(PillarPoint(-0.44f, 0.42f), PillarPoint(0.44f, 0.42f));
            return;
        }

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

    private void DrawTerrainPolygon(Vector2[] screenPoints, Terrain terrain, float radius)
    {
        DrawColoredPolygon(screenPoints, TerrainFill(terrain));
        DrawTerrainTexture(screenPoints, terrain, radius);
    }

    private void DrawTerrainTexture(Vector2[] points, Terrain terrain, float radius)
    {
        var screenRadius = Math.Max(1f, radius * _zoom);
        var step = Mathf.Clamp(screenRadius / 5.5f, 5f, 14f);
        var dotRadius = Mathf.Clamp(screenRadius / 46f, 0.65f, 1.9f);
        switch (terrain)
        {
            case Terrain.Wastes:
                DrawDitheredPolygon(points, WastesTexture, dotRadius, step * 1.15f, TerrainPattern.Dot);
                break;
            case Terrain.Ruins:
                DrawDitheredPolygon(points, RuinsTexture, dotRadius, step, TerrainPattern.Block);
                break;
            case Terrain.Pillars:
                DrawDitheredPolygon(points, PillarsTexture, dotRadius, step * 0.95f, TerrainPattern.Stroke);
                break;
            case Terrain.Settlements:
                DrawDitheredPolygon(points, SettlementsTexture, dotRadius, step, TerrainPattern.Dot);
                break;
        }
    }

    private void DrawDitheredPolygon(Vector2[] points, Color colour, float radius, float step, TerrainPattern pattern)
    {
        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);

        for (var y = minY; y <= maxY; y += step)
        {
            var rowOffset = Mathf.RoundToInt(y / step) % 2 == 0 ? 0f : step * 0.5f;
            for (var x = minX - rowOffset; x <= maxX; x += step)
            {
                var point = new Vector2(x + rowOffset, y);
                if (!Geometry2D.IsPointInPolygon(point, points))
                {
                    continue;
                }

                switch (pattern)
                {
                    case TerrainPattern.Dot:
                        DrawCircle(point, radius, colour);
                        break;
                    case TerrainPattern.Block:
                        DrawRect(new Rect2(point - Vector2.One * radius, Vector2.One * radius * 2f), colour);
                        break;
                    case TerrainPattern.Stroke:
                        DrawLine(point + new Vector2(0f, -radius * 2f), point + new Vector2(0f, radius * 2f), colour, Math.Max(1f, radius), true);
                        break;
                }
            }
        }
    }

    private static Color HazardColor(int dieRoll) => dieRoll switch
    {
        1 => new Color("ef4444"),
        2 => new Color("a855f7"),
        3 => new Color("22c55e"),
        4 => new Color("f97316"),
        5 => new Color("3b82f6"),
        6 => new Color("eab308"),
        _ => Colors.White,
    };

    private void DrawScreenTextCentered(string text, Vector2 centre, int fontSize, Color colour)
    {
        var font = ThemeDB.FallbackFont;
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1f, fontSize);
        DrawString(font, centre + new Vector2(-size.X / 2f, size.Y / 3f), text, HorizontalAlignment.Left, -1f, fontSize, colour);
    }

    private enum TerrainPattern
    {
        Dot,
        Block,
        Stroke,
    }

    private void SelectAt(Vector2 screenPosition)
    {
        switch (_navigation.Current)
        {
            case MapLocation.Regional:
                SelectRegional(screenPosition);
                break;
            case MapLocation.Local local:
                SelectLocal(screenPosition, local.RegionalCoordinate);
                break;
            case MapLocation.Dungeon dungeon:
                SelectRuin(screenPosition, _navigation.Campaign.Ruin);
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
        _previewRegionalPath = BuildRegionalPath(selected.Value);
        _navigation.SelectRegional(selected.Value);
        var terrain = _navigation.Campaign.Regional.GetTerrain(selected.Value);
        var route = _previewRegionalPath.Count > 1 ? $"\n\nRoute: {_previewRegionalPath.Count - 1} regional hex(es), {(_previewRegionalPath.Count - 1) * 6} miles. Right-click endpoint to travel." : string.Empty;
        CellSelected?.Invoke($"Regional hex {selected.Value}\n\nTerrain: {terrain}\nScale: 6 miles per hex.{route}\n\nThis regional hex owns one local map.");
    }

    private IReadOnlyList<RegionalCoord> BuildRegionalPath(RegionalCoord target)
    {
        var origin = _navigation.Campaign.PartyTravel.RegionalCoordinate;
        var queue = new Queue<RegionalCoord>();
        var previous = new Dictionary<RegionalCoord, RegionalCoord?> { [origin] = null };
        queue.Enqueue(origin);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target) break;
            for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
            {
                if (_navigation.Campaign.Regional.GetNeighbour(current, direction) is { } next && previous.TryAdd(next, current)) queue.Enqueue(next);
            }
        }
        if (!previous.ContainsKey(target)) return [];
        var path = new List<RegionalCoord>();
        for (RegionalCoord? cursor = target; cursor is not null; cursor = previous[cursor.Value]) path.Add(cursor.Value);
        path.Reverse();
        return path;
    }

    private void SelectLocal(Vector2 screenPosition, RegionalCoord centre)
    {
        var selected = _navigation.Campaign.GetLocalArea(centre)
            .SelectMany(regionalCoordinate => _navigation.Campaign.GetLocalMap(regionalCoordinate).VisibleCells
                .Select(cell => new LocalMapCoord(regionalCoordinate, cell)))
            .Select(location => (Location: location, Distance: ToScreen(LocalWorldCentre(location.RegionalCoordinate, location.LocalCoordinate)).DistanceTo(screenPosition)))
            .Where(candidate => candidate.Distance <= LocalHexRadius * _zoom)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => (LocalMapCoord?)candidate.Location)
            .FirstOrDefault();

        if (selected is null)
        {
            return;
        }

        _selectedLocal = selected;
        _previewLocalPath = BuildLocalPath(selected.Value);
        var map = _navigation.Campaign.GetLocalMap(selected.Value.RegionalCoordinate);
        var isEntrance = map.Parent == Campaign.DungeonRegionalCoordinate && selected.Value.LocalCoordinate == Campaign.DungeonLocalCoordinate;
        var terrain = map.GetTerrain(selected.Value.LocalCoordinate);
        var hazardText = map.RoamingHazards.TryGetValue(selected.Value.LocalCoordinate, out var dieRoll)
            ? $"\n\nRoaming hazard: {LocalMap.GetRoamingHazardName(dieRoll)} (d6: {dieRoll})."
            : string.Empty;
        var route = _previewLocalPath.Count > 1 ? $"\n\nRoute: {_previewLocalPath.Count - 1} mile(s). Right-click this endpoint to travel." : string.Empty;
        CellSelected?.Invoke($"Local subhex {selected.Value.LocalCoordinate} in regional hex {selected.Value.RegionalCoordinate}\n\nTerrain: {terrain}\nScale: 1 mile per subhex.{route}{hazardText}{(isEntrance ? "\n\nOrange marker: dungeon entrance." : string.Empty)}");
    }

    private IReadOnlyList<LocalMapCoord> BuildLocalPath(LocalMapCoord target)
    {
        var party = _navigation.Campaign.PartyTravel;
        if (target.RegionalCoordinate != party.RegionalCoordinate || party.RestRequired) return [];
        var origin = party.LocalCoordinate;
        var map = _navigation.Campaign.GetLocalMap(party.RegionalCoordinate);
        var queue = new Queue<HexCoord>();
        var previous = new Dictionary<HexCoord, HexCoord?> { [origin] = null };
        queue.Enqueue(origin);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target.LocalCoordinate) break;
            for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
            {
                var next = current.Neighbour(direction);
                if (map.VisibleCells.Contains(next) && previous.TryAdd(next, current)) queue.Enqueue(next);
            }
        }
        if (!previous.ContainsKey(target.LocalCoordinate)) return [];
        var path = new List<LocalMapCoord>();
        for (HexCoord? cursor = target.LocalCoordinate; cursor is not null; cursor = previous[cursor.Value]) path.Add(new LocalMapCoord(party.RegionalCoordinate, cursor.Value));
        path.Reverse();
        return path;
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

    private void SelectRuin(Vector2 screenPosition, RuinExploration ruin)
    {
        var selected = ruin.Layout.Rooms
            .Select(room => (Room: room, Position: ToScreen(new Vector2(room.Coordinate.X * 120f, room.Coordinate.Y * 100f))))
            .Where(candidate => candidate.Position.DistanceTo(screenPosition) <= 28f * _zoom)
            .OrderBy(candidate => candidate.Position.DistanceTo(screenPosition))
            .FirstOrDefault();
        if (selected.Room is null) return;
        _selectedGrid = selected.Room.Coordinate;
        CellSelected?.Invoke($"Ruin depth {ruin.Depth}, room {selected.Room.Coordinate}\n\n{(ruin.VisitedRooms.Contains(selected.Room.Coordinate) ? "Visited" : "Unexplored")}\nSelect Move in the panel to traverse a connected passage.");
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
        MapLocation.Local local => ChunkCentre(local.RegionalCoordinate),
        MapLocation.Dungeon => new Vector2(240f, 120f),
        _ => Vector2.Zero,
    };

    private static Vector2 RegionalCentre(RegionalCoord coordinate) => new(
        coordinate.Column * RegionalHexRadius * 1.5f,
        (coordinate.Row + (coordinate.Column % 2) * 0.5f) * RegionalHexRadius * Mathf.Sqrt(3f));

    private static Vector2 LocalCentre(HexCoord coordinate) => new(
        coordinate.Q * LocalHexRadius * 1.5f,
        (coordinate.R + coordinate.Q * 0.5f) * LocalHexRadius * Mathf.Sqrt(3f));

    private static Vector2 ChunkCentre(RegionalCoord coordinate) => new(
        coordinate.Column * LocalRegionalRadius * 1.5f,
        (coordinate.Row + (coordinate.Column % 2) * 0.5f) * LocalRegionalRadius * Mathf.Sqrt(3f));

    private static Vector2 LocalWorldCentre(RegionalCoord regionalCoordinate, HexCoord localCoordinate) =>
        ChunkCentre(regionalCoordinate) + LocalCentre(localCoordinate);

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
