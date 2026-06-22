namespace VastDark.Domain;

public sealed record RuinDiscoveryRule(int Roll, string Name, string Effect);
public sealed record RuinDiscoveryResult(bool Discovered, int CheckTotal, RuinDiscoveryRule? Discovery);

/// <summary>Page 29 one-per-room Ruing Remnants discovery procedure.</summary>
public sealed class RuinDiscoveryTracker
{
    private readonly HashSet<(int Depth, GridCoord Room)> _resolvedRooms = [];
    public RuinDiscoveryResult EnterRoom(int depth, GridCoord room, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth));
        if (!_resolvedRooms.Add((depth, room))) return new RuinDiscoveryResult(false, 0, null);
        var check = random.Next(1, 21) + depth;
        return check < 20 ? new RuinDiscoveryResult(false, check, null) : new RuinDiscoveryResult(true, check, RuinDiscoveryRules.Get(random.Next(1, 7)));
    }
}

public static class RuinDiscoveryRules
{
    private static readonly IReadOnlyDictionary<int, RuinDiscoveryRule> Discoveries = new Dictionary<int, RuinDiscoveryRule>
    {
        [1] = new(1, "Inscrutable Art", "Recover 1d3 exhaustion."), [2] = new(2, "Esoteric Records", "Re-roll the next encounter and take the lower result."), [3] = new(3, "Curious Currency", "1d100 coins."), [4] = new(4, "Lost Architecture", "Choose the next room feature."), [5] = new(5, "Lost Habitation", "Rest safely and undisturbed for 1d3 days."), [6] = new(6, "Dangerous Artifact", "As Treasure: Something Great and Terrible."),
    };
    public static RuinDiscoveryRule Get(int roll) => Discoveries.TryGetValue(roll, out var discovery) ? discovery : throw new ArgumentOutOfRangeException(nameof(roll));
}
