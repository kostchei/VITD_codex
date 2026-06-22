namespace VastDark.Domain;

public enum SettlementLocation { Storyteller, ScrapSmithy, Apothecary, PyromancerFoundry, MagusSanctum, Reservoir, Bazaar, CartographerRoost, LodestoneCarver, MemorialShrine, Paddock, NomadHold }
public sealed record SettlementLocationRule(SettlementLocation Location, int Roll, string Name, IReadOnlyList<string> Services);
public sealed record ApothecaryResult(string Name, string Effect, string Requirement);

/// <summary>Page 17 settlement-location services. Costs not numerically specified remain barter/trade requirements.</summary>
public static class SettlementServiceRules
{
    private static readonly IReadOnlyDictionary<int, SettlementLocationRule> Locations = new Dictionary<int, SettlementLocationRule>
    {
        [1] = new(SettlementLocation.Storyteller, 1, "Storyteller", ["Stories: each day resting here has a 1-in-6 chance to recover an additional exhaustion."]),
        [2] = new(SettlementLocation.ScrapSmithy, 2, "Scrap Smithy", ["Repair: trade two metal tools, arms, or armor for one new item of equal weight; broken items count.", "Renew: trade metal ingots for items of equal weight."]),
        [3] = new(SettlementLocation.Apothecary, 3, "Apothecary", ["Medicine: buy alchemical tools with specific material components.", "Hellfire: Griffon blood + Cyclops hollow head; thrown, sticks, 3d6 damage, Crawl flee.", "Hearthfire: Crawl fat and bones; 6 hours warmth and torchlight.", "Remedy: skinned Harpy; recover 1d6 Grit or 1d3 Flesh.", "Malady: Hydra glands; 1d3 contact: blinded, 2d6 damage, or 1 hour paralysis."]),
        [4] = new(SettlementLocation.PyromancerFoundry, 4, "Pyromancer Foundry", ["Jarred Fire: buy special tools with specific material components."]),
        [5] = new(SettlementLocation.MagusSanctum, 5, "Magus Sanctum", ["Magic Scrolls: trade raw lodestone for a single-spell scroll that dissolves after use."]),
        [6] = new(SettlementLocation.Reservoir, 6, "Reservoir", ["Add +1 to the Scarcity roll."]),
        [7] = new(SettlementLocation.Bazaar, 7, "Bazaar", ["Barter: trade common items instead of coin or lodestone purchases."]),
        [8] = new(SettlementLocation.CartographerRoost, 8, "Cartographer Roost", ["Wayfinder: buy directions and maps as an item."]),
        [9] = new(SettlementLocation.LodestoneCarver, 9, "Lodestone Carver", ["Expertise: exchange each raw lodestone for 100 coins."]),
        [10] = new(SettlementLocation.MemorialShrine, 10, "Memorial Shrine", ["Remember: when a companion dies, write their name and replace a lost memory with memory of them."]),
        [11] = new(SettlementLocation.Paddock, 11, "Paddock", ["Add +1 to the Scarcity roll."]),
        [12] = new(SettlementLocation.NomadHold, 12, "Nomad Hold", ["Companions: recruit or pay hirelings, Travelers, and companions to join the party."]),
    };

    public static SettlementLocationRule Get(int roll) => Locations.TryGetValue(roll, out var location) ? location : throw new ArgumentOutOfRangeException(nameof(roll));
    public static int LodestoneCarverCoins(int rawLodestone) => rawLodestone < 0 ? throw new ArgumentOutOfRangeException(nameof(rawLodestone)) : rawLodestone * 100;
    public static ApothecaryResult ResolveRemedy(bool restoreFlesh, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return restoreFlesh
            ? new ApothecaryResult("Remedy", $"Recover {random.Next(1, 4)} Flesh.", "Skinned Harpy")
            : new ApothecaryResult("Remedy", $"Recover {random.Next(1, 7)} Grit.", "Skinned Harpy");
    }
    public static ApothecaryResult ResolveMalady(IRandomSource random) => random.Next(1, 4) switch
    {
        1 => new ApothecaryResult("Malady", "Blinded.", "Hydra glands"),
        2 => new ApothecaryResult("Malady", "2d6 damage.", "Hydra glands"),
        _ => new ApothecaryResult("Malady", "Paralysis for 1 hour.", "Hydra glands"),
    };
}
