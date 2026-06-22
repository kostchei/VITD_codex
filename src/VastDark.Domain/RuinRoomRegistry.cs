namespace VastDark.Domain;

public sealed record RuinRoomDefinition(int Code, string Name, string Summary);

/// <summary>Source-faithful registry for pages 22-27. The printed table has no 32 and two 45 entries, so callers receive all source choices for a code.</summary>
public static class RuinRoomRegistry
{
    private static readonly IReadOnlyDictionary<int, IReadOnlyList<RuinRoomDefinition>> Rooms = new Dictionary<int, IReadOnlyList<RuinRoomDefinition>>
    {
        [11] = [new(11, "Plaza", "Open tiled expanse; possible sculptures and ambushes.")], [12] = [new(12, "Graveyard", "Ordered plinths like a cemetery.")], [13] = [new(13, "Archive", "Towering library-like shelves.")], [14] = [new(14, "Kennel", "Claustrophobic wall rooms attracting life.")], [15] = [new(15, "Oubliette", "Slick funnel pit.")], [16] = [new(16, "Temple", "Vaulted sanctum.")],
        [21] = [new(21, "Pit", "Bottomless-looking vertical descent.")], [22] = [new(22, "Vault", "Rusted iron gate and treasure.")], [23] = [new(23, "Atrium", "Grand transition space with Travelers.")], [24] = [new(24, "Tower", "Vertical chimney that may reach the surface.")], [25] = [new(25, "Ossuary", "Winding tunnel of small holes.")], [26] = [new(26, "Great Hall", "Titanic columned hall; roll encounters twice.")],
        [31] = [new(31, "Maze", "Fractal maze requiring three consecutive Intelligence checks.")], [33] = [new(33, "Bathhouse", "Water basins and sediment.")], [34] = [new(34, "Amphitheater", "Concentric steps and a stage.")], [35] = [new(35, "Cellar", "Cold rooms where contents do not age.")], [36] = [new(36, "Planetarium", "Cosmic dome affecting dreams and meditation.")],
        [41] = [new(41, "Dormitory", "Stasis sleep slabs.")], [42] = [new(42, "Dump", "Detritus mounds with hourly search risks.")], [43] = [new(43, "Pyramid", "Single-stone pyramid with three possibilities.")], [44] = [new(44, "Nave", "Eldritch architecture for risky meditation.")], [45] = [new(45, "Reliquary", "Cracked alcove with an ominous object."), new(45, "Fountain", "Basin fed by water, blood, or dust.")], [46] = [new(46, "Colonnade", "Endless identical columns; roll encounters twice.")],
        [51] = [new(51, "Canyon", "Rift with bridge, path, fishery, and deep descent.")], [52] = [new(52, "Kiln", "Half-molten stone; fires persist until snuffed.")], [53] = [new(53, "Well", "Trawling may snag six possible results.")], [54] = [new(54, "Corridor", "Clean passage with squeeze, hollow, or twisted variants.")], [55] = [new(55, "Mosaic", "Study for Wastes maps; harvest lodestone noisily.")], [56] = [new(56, "Garden", "Sculpture garden guarded by Delvers.")],
        [61] = [new(61, "Scriptorium", "Hypnotic floor etchings.")], [62] = [new(62, "Oasis", "Safe, encounter-free water pool.")], [63] = [new(63, "Quarry", "Collapsing cliff crossing.")], [64] = [new(64, "Coliseum", "Maximum-damage attacks and Grit on kills.")], [65] = [new(65, "Lighthouse", "Memory-linked sleep return costs items.")], [66] = [new(66, "Observatory", "Pipes reveal sounds; predict encounters.")],
    };

    public static IReadOnlyList<RuinRoomDefinition> Get(int firstD6, int secondD6)
    {
        if (firstD6 is < 1 or > 6 || secondD6 is < 1 or > 6) throw new ArgumentOutOfRangeException();
        var code = firstD6 * 10 + secondD6;
        return Rooms.TryGetValue(code, out var results) ? results : [];
    }
}
