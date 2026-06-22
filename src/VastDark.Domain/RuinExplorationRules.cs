namespace VastDark.Domain;

/// <summary>Page 21 cautious Ruin movement, search time, and depth transitions.</summary>
public sealed class RuinExploration
{
    private GeneratedRuin _layout;
    private readonly HashSet<GridCoord> _visited = [];
    private readonly HashSet<GridCoord> _searched = [];

    public const int MinutesPerHallwayPoint = 10;
    public const int MinutesPerRoomTransition = 10;
    public const int MinutesToSearchRoom = 30;

    public RuinExploration(GeneratedRuin layout, GridCoord entrance)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!layout.Rooms.Any(room => room.Coordinate == entrance)) throw new ArgumentException("The entrance must be a room in the layout.", nameof(entrance));
        _layout = layout;
        CurrentRoom = entrance;
        _visited.Add(entrance);
    }

    public int Depth { get; private set; } = 1;
    public int ElapsedMinutes { get; private set; }
    public GridCoord CurrentRoom { get; private set; }
    public GeneratedRuin Layout => _layout;
    public IReadOnlySet<GridCoord> VisitedRooms => _visited;
    public IReadOnlySet<GridCoord> SearchedRooms => _searched;

    public bool TryMoveToRoom(GridCoord target)
    {
        if (!_layout.Passages.Any(passage => (passage.From == CurrentRoom && passage.To == target) || (passage.To == CurrentRoom && passage.From == target))) return false;
        CurrentRoom = target;
        _visited.Add(target);
        ElapsedMinutes += MinutesPerRoomTransition;
        return true;
    }

    public void TravelHallwayPoint() => ElapsedMinutes += MinutesPerHallwayPoint;
    public void SearchCurrentRoom() { _searched.Add(CurrentRoom); ElapsedMinutes += MinutesToSearchRoom; }

    public void Descend(GeneratedRuin nextLayout, GridCoord descentRoom)
    {
        ArgumentNullException.ThrowIfNull(nextLayout);
        if (!nextLayout.Rooms.Any(room => room.Coordinate == descentRoom)) throw new ArgumentException("The descent must lead to a room in the next layout.", nameof(descentRoom));
        Depth++;
        _layout = nextLayout;
        CurrentRoom = descentRoom;
        _visited.Clear();
        _searched.Clear();
        _visited.Add(descentRoom);
    }
}
