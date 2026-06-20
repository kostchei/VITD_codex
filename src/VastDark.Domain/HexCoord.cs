namespace VastDark.Domain;

/// <summary>Axial coordinate for a flat-topped hex grid.</summary>
public readonly record struct HexCoord(int Q, int R)
{
    public static readonly HexCoord Zero = new(0, 0);

    public static readonly IReadOnlyList<HexCoord> Directions =
    [new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)];

    public int S => -Q - R;

    public HexCoord Neighbour(int direction)
    {
        if (direction is < 0 or >= 6)
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        var offset = Directions[direction];
        return new HexCoord(Q + offset.Q, R + offset.R);
    }

    public int DistanceTo(HexCoord other) =>
        (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
}

