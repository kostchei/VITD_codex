namespace VastDark.Domain;

public sealed record RuinRoom(GridCoord Coordinate, int SourceFaceIndex);
public sealed record RuinPassage(GridCoord From, GridCoord To);
public sealed record GeneratedRuin(IReadOnlyList<int> VisibleFaces, IReadOnlyList<RuinRoom> Rooms, IReadOnlyList<RuinPassage> Passages);

/// <summary>Page 20 five-visible-face ruin graph construction. Face 0 is the central cluster; north/south/east/west attach in that order.</summary>
public static class RuinGenerationRules
{
    public static GeneratedRuin Generate(IReadOnlyList<int> visibleFaces)
    {
        ArgumentNullException.ThrowIfNull(visibleFaces);
        if (visibleFaces.Count != 5 || visibleFaces.Any(face => face is < 1 or > 6)) throw new ArgumentException("A ruin needs five visible d6 faces.", nameof(visibleFaces));
        var rooms = new List<RuinRoom>();
        var passages = new List<RuinPassage>();
        var central = Enumerable.Range(0, visibleFaces[0]).Select(index => new GridCoord(index, 0)).ToList();
        AddLine(central, 0, connectTo: null);
        AddLine(Enumerable.Range(0, visibleFaces[1]).Select(index => new GridCoord(index, -1)).ToList(), 1, central);
        AddLine(Enumerable.Range(0, visibleFaces[2]).Select(index => new GridCoord(index, 1)).ToList(), 2, central);
        AddLine(Enumerable.Range(0, visibleFaces[3]).Select(index => new GridCoord(-1, index)).ToList(), 3, central);
        AddLine(Enumerable.Range(0, visibleFaces[4]).Select(index => new GridCoord(visibleFaces[0], index)).ToList(), 4, central);
        return new GeneratedRuin(visibleFaces.ToArray(), rooms, passages);

        void AddLine(IReadOnlyList<GridCoord> coordinates, int faceIndex, IReadOnlyList<GridCoord>? connectTo)
        {
            for (var index = 0; index < coordinates.Count; index++)
            {
                rooms.Add(new RuinRoom(coordinates[index], faceIndex));
                if (index > 0) passages.Add(new RuinPassage(coordinates[index - 1], coordinates[index]));
                if (connectTo is not null) passages.Add(new RuinPassage(connectTo[Math.Min(index, connectTo.Count - 1)], coordinates[index]));
            }
        }
    }

    public static GeneratedRuin RollAndGenerate(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return Generate(Enumerable.Range(0, 5).Select(_ => random.Next(1, 7)).ToArray());
    }
}
