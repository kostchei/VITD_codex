namespace VastDark.Domain;

public sealed record WastesEncounterRule(int Roll, string Name, string Quantity, string Description, bool RequiresMood = false);
public sealed record WastesMoodRule(int Roll, string Name, string Consequence);
public sealed record WastesCuriosityRule(int Roll, string Name, string Description);

/// <summary>Page 12's Wastes encounter (d12, modified by weather), mood (d6), and curiosity (d20) content.</summary>
public static class WastesEncounterRules
{
    private static readonly IReadOnlyDictionary<int, WastesEncounterRule> Encounters = new Dictionary<int, WastesEncounterRule>
    {
        [1] = new(1, "Nothing", "", "You are alone for now."), [2] = new(2, "Nothing", "", "You are alone for now."), [3] = new(3, "Nothing", "", "You are alone for now."), [4] = new(4, "Nothing", "", "You are alone for now."), [5] = new(5, "Nothing", "", "You are alone for now."),
        [6] = new(6, "Lost Travelers", "1d6", "Desperate for food and shelter; helpful if assisted."),
        [7] = new(7, "Nomads", "1d6", "Braving the Wastes weather.", true),
        [8] = new(8, "Merchants", "1d3", "Heavy-pulk traders willing to buy, sell, or trade; limit 100 coin."),
        [9] = new(9, "Bandits", "1d6", "Prowling for an easy score.", true),
        [10] = new(10, "Pilgrims", "2d6", "Traveling to the nearest ruin or pillar; devoted to a random faction."),
        [11] = new(11, "Lodestone Prospectors", "1d6", "Carry 1d20 raw lodestone on a sleigh; cautious, hostile if harassed."),
        [12] = new(12, "Caravan", "1d6 Merchants and 2d6 Nomads", "Heavy-pulk traders willing to buy, sell, or trade; limit 1000 coin."),
        [13] = new(13, "Cutthroats", "1d6", "Out for blood and plunder.", true),
        [14] = new(14, "Cyclops", "1d6", "Clustered for warmth, smelling the air for unwary mortals."),
        [15] = new(15, "Harpies", "1d3", "Circling above or buried under dust, patiently waiting."),
        [16] = new(16, "Medusa", "1d3", "Hidden in corners and listening for steps."),
        [17] = new(17, "Shade", "1", "Drifting across dust, vibrating with hunger."),
        [18] = new(18, "Griffon", "1", "Resting at the highest point near its latest victim's remains."),
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, WastesMoodRule>> Moods = new Dictionary<string, IReadOnlyDictionary<int, WastesMoodRule>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Nomads"] = new Dictionary<int, WastesMoodRule> { [1] = new(1, "Cautious", "Hostile if disturbed."), [2] = new(2, "Curious", "Peaceful if hailed."), [3] = new(3, "Curious", "Peaceful if hailed."), [4] = new(4, "Curious", "Peaceful if hailed."), [5] = new(5, "Friendly", "Gives directions and warns of nearby dangers."), [6] = new(6, "Friendly", "Gives directions and warns of nearby dangers.") },
        ["Bandits"] = new Dictionary<int, WastesMoodRule> { [1] = new(1, "Crazed", "Attacks to kill and loot."), [2] = new(2, "Crazed", "Attacks to kill and loot."), [3] = new(3, "Tribute", "Demands 100 coins or one ration from each Traveler."), [4] = new(4, "Tribute", "Demands 100 coins or one ration from each Traveler."), [5] = new(5, "Tribute", "Demands 100 coins or one ration from each Traveler."), [6] = new(6, "Curious", "Lets characters join a raid.") },
        ["Cutthroats"] = new Dictionary<int, WastesMoodRule> { [1] = new(1, "Crazed", "Attacks to kill and loot."), [2] = new(2, "Crazed", "Attacks to kill and loot."), [3] = new(3, "Crazed", "Attacks to kill and loot."), [4] = new(4, "Tribute", "Demands 1000 coins or all rations."), [5] = new(5, "Tribute", "Demands 1000 coins or all rations."), [6] = new(6, "Recruit", "Demands characters fight each other; survivor joins the Cutthroats.") },
    };

    private static readonly IReadOnlyDictionary<int, WastesCuriosityRule> Curiosities = new Dictionary<int, WastesCuriosityRule>
    {
        [1] = new(1, "Ruin outcropping", "Provides shelter; 1-in-6 encounter chance."), [2] = new(2, "Abandoned camp", "Signs of violence; 1d3 rations and a random tool."), [3] = new(3, "Stone totem", "Geometric designs and effigy offerings."), [4] = new(4, "Desiccated nomads", "One utters one word before expiring."), [5] = new(5, "Burial cairn", "Random weapon and tool."),
        [6] = new(6, "Lodestone cache", "1d10 × 5 lodestone beneath an illegible note."), [7] = new(7, "Nomad in black", "Silent hand points to the nearest pillar."), [8] = new(8, "Collapsed tower", "Provides shelter."), [9] = new(9, "Lodestone obelisk", "Mine 1d20 raw lodestone."), [10] = new(10, "Tied Traveler", "Helpful if freed; has no memory of before."),
        [11] = new(11, "Unearthed road", "Leads to nearest ruins and counts as a landmark."), [12] = new(12, "Swarm", "Stinging insects; 1d3 rations."), [13] = new(13, "Lonely graves", "Carefully laid bodies obscured by sand."), [14] = new(14, "Nest", "Eyeless worms or small crabs; 1d6 rations."), [15] = new(15, "Crawl corpse", "Rotten and massive, with many mouths feeding."),
        [16] = new(16, "Secret tunnel", "Provides shelter and travels 1d6 miles randomly."), [17] = new(17, "Message", "Illegible stone-slab carving."), [18] = new(18, "Lost caravan", "Buried corpses and goods; 1d20 rations or tools."), [19] = new(19, "Bereft swordsman", "Nearly rusted blade; says only: There is no way out."), [20] = new(20, "Forgotten treasure", "Roll Treasure on page 29."),
    };

    public static WastesEncounterRule GetEncounter(int roll) => Encounters.TryGetValue(roll, out var encounter) ? encounter : throw new ArgumentOutOfRangeException(nameof(roll));
    public static WastesMoodRule GetMood(string encounterName, int roll) => Moods.TryGetValue(encounterName, out var moods) && moods.TryGetValue(roll, out var mood) ? mood : throw new ArgumentOutOfRangeException(nameof(roll), "This encounter has no mood table or the mood roll is invalid.");
    public static WastesCuriosityRule GetCuriosity(int roll) => Curiosities.TryGetValue(roll, out var curiosity) ? curiosity : throw new ArgumentOutOfRangeException(nameof(roll));
}
