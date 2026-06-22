namespace VastDark.Domain;

public sealed record PillarTunnelShape(int Roll, string Name, string RuleText);
public sealed record PillarTunnel(int Depth, int ShapeRoll, PillarTunnelShape Shape, int? SplitMarker = null);
public sealed record PillarEventRule(int MinimumRoll, int? MaximumRoll, string Name, string RuleText);
public sealed record PillarLootRule(int MinimumRoll, int? MaximumRoll, string Name, string RuleText);

public sealed class PillarDelve
{
    private readonly List<PillarTunnel> _tunnels = [];
    public const int MinutesToTravelTunnel = 10;
    public const int MinutesToSearchTunnel = 30;
    public IReadOnlyList<PillarTunnel> Tunnels => _tunnels;

    public PillarTunnel EnterTunnel(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var shapeRoll = random.Next(1, 7);
        int? splitMarker = null;
        if (_tunnels.Any(tunnel => tunnel.ShapeRoll == shapeRoll)) splitMarker = random.Next(1, 7);
        var tunnel = new PillarTunnel(_tunnels.Count + 1, shapeRoll, PillarDelvingRules.GetShape(shapeRoll), splitMarker);
        _tunnels.Add(tunnel);
        return tunnel;
    }

    public PillarEventRule RollEvent(IRandomSource random) => PillarDelvingRules.GetEvent(random.Next(1, 7) + _tunnels.Count - 1);
    public PillarLootRule RollLoot(IRandomSource random) => PillarDelvingRules.GetLoot(random.Next(1, 7) + _tunnels.Count);
}

/// <summary>Page 15 tunnel shape, escalating event, and depth-based loot tables.</summary>
public static class PillarDelvingRules
{
    private static readonly IReadOnlyDictionary<int, PillarTunnelShape> Shapes = new Dictionary<int, PillarTunnelShape>
    {
        [1] = new(1, "Constricting Squeeze", "Single file; Travelers are prone."), [2] = new(2, "Sheer Drop", "Must be climbed; tools improve safety."), [3] = new(3, "Tight Halls", "Single-file travel."), [4] = new(4, "Winding Tunnel", "Loops back and forth; easy to get lost."), [5] = new(5, "Jagged Ascent", "Must be climbed; tools improve safety."), [6] = new(6, "Cavernous", "Massive and dark."),
    };
    private static readonly IReadOnlyList<PillarEventRule> Events =
    [
        new(1, 3, "Chill Fog", "Save v. Poison or 1d6 cold damage; heavy clothes or fire prevents it."), new(4, 4, "Wind Blast", "Save v. Breath or use a tool, or be tossed for 2d6 damage."), new(5, 5, "Cyclops", "2d6 emerge from the dark."), new(6, 6, "Decay", "Lose a random item or erase an unused inventory slot."), new(7, 7, "Medusa", "1d6 creep forward."), new(8, 8, "Harpies", "2d6 cling to walls."), new(9, 9, "Collapse", "Requires tools and one hour to clear; excavating rolls an event."), new(10, 10, "Hallucination", "Save v. Madness each round or lash out at companions."), new(11, 11, "Harmonics", "Save v. Poison or gain one exhaustion."), new(12, 12, "Ogre", "A hungry Ogre squeezes forward."), new(13, 13, "Ego Sink", "Save v. Charm or gain 1d3 exhaustion."), new(14, 14, "Shade", "A violent Shade swarm."), new(15, null, "Call of the Dark", "Gain one exhaustion; if unable, lose a memory."),
    ];
    private static readonly IReadOnlyList<PillarLootRule> Loot =
    [
        new(1, 3, "Forgotten Corpse", "1d3 random tools and 1d20 coins."), new(4, 6, "Raw Lodestone", "1d10 raw lodestone."), new(7, 7, "Lodestone Idols", "1d10 idols worth 100 coins each."), new(8, 8, "Abandoned Supplies", "1d6 random tools, 1d10 rations, 1d6 × 10 coins."), new(9, 9, "Raw Lodestone Pile", "2d10 raw lodestone."), new(10, 10, "Lone Survivor", "Whispers a dangerous secret before expiring."), new(11, 11, "Lodestone Mural", "Geometric carvings allude to obscure truth."), new(12, 12, "Corpse Pile", "Tools of choice, 1d20 rations, 1d6 × 50 coins."), new(13, 13, "Artifact", "Roll Artifact on page 29."), new(14, null, "Hoard", "2d20 raw lodestone."),
    ];

    public static PillarTunnelShape GetShape(int roll) => Shapes.TryGetValue(roll, out var shape) ? shape : throw new ArgumentOutOfRangeException(nameof(roll));
    public static PillarEventRule GetEvent(int roll) => Events.First(rule => roll >= rule.MinimumRoll && (rule.MaximumRoll is null || roll <= rule.MaximumRoll));
    public static PillarLootRule GetLoot(int roll) => Loot.First(rule => roll >= rule.MinimumRoll && (rule.MaximumRoll is null || roll <= rule.MaximumRoll));
}
