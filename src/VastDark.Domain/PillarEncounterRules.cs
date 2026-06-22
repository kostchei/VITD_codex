namespace VastDark.Domain;

public sealed record PillarEncounterRule(int MinimumRoll, int? MaximumRoll, string Name, string Quantity, string Description, bool RequiresMood = false);
public sealed record PillarMoodRule(int Roll, string Name, string Consequence);

/// <summary>Page 14 Pillar encounter table; rolls of 15 or greater are Griffons.</summary>
public static class PillarEncounterRules
{
    private static readonly IReadOnlyList<PillarEncounterRule> Encounters =
    [
        new(1, 2, "Nothing", "", "You are alone for now."),
        new(3, 3, "Lost Travelers", "1d6", "Desperate for food and shelter; helpful if assisted."),
        new(4, 4, "Lodestone Miners", "1d6", "Gathering and mining.", true),
        new(5, 5, "Merchants", "1d3", "Heavy-pulk traders; limit 100 coin."),
        new(6, 6, "Cyclops", "1d6", "Circling the pillar searching for mortals."),
        new(7, 7, "Bandits", "1d6", "Prowling for an easy score.", true),
        new(8, 8, "Harpies", "1d3", "Circling above, attracted by prey."),
        new(9, 9, "Cutthroats", "1d6", "Out for blood and plunder.", true),
        new(10, 10, "Medusa", "1d3", "Slithering out from the dark, humming."),
        new(11, 11, "Cyclops", "2d6", "Tumbling from cracks, howling violently."),
        new(12, 12, "Ogre", "1", "Lurching out from the Pillar."),
        new(13, 13, "Harpies", "2d6", "Closing in, frenzied with hunger."),
        new(14, 14, "Shade", "1", "Its dark form spills out from the Pillar."),
        new(15, null, "Griffon", "1", "Its shriek echoes as wings stir the dust."),
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, PillarMoodRule>> Moods = new Dictionary<string, IReadOnlyDictionary<int, PillarMoodRule>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Lodestone Miners"] = new Dictionary<int, PillarMoodRule> { [1] = new(1, "Territorial", "Hostile if disturbed."), [2] = new(2, "Curious", "Peaceful if hailed."), [3] = new(3, "Curious", "Peaceful if hailed."), [4] = new(4, "Curious", "Peaceful if hailed."), [5] = new(5, "Friendly", "Gives directions and warns of nearby dangers."), [6] = new(6, "Friendly", "Gives directions and warns of nearby dangers.") },
        ["Bandits"] = new Dictionary<int, PillarMoodRule> { [1] = new(1, "Crazed", "Attacks to kill and loot."), [2] = new(2, "Crazed", "Attacks to kill and loot."), [3] = new(3, "Tribute", "Demands all raw lodestone or one ration per Traveler."), [4] = new(4, "Tribute", "Demands all raw lodestone or one ration per Traveler."), [5] = new(5, "Tribute", "Demands all raw lodestone or one ration per Traveler."), [6] = new(6, "Curious", "Lets characters join a raid.") },
        ["Cutthroats"] = new Dictionary<int, PillarMoodRule> { [1] = new(1, "Crazed", "Attacks to kill and loot."), [2] = new(2, "Crazed", "Attacks to kill and loot."), [3] = new(3, "Crazed", "Attacks to kill and loot."), [4] = new(4, "Tribute", "Demands all lodestone or all rations."), [5] = new(5, "Tribute", "Demands all lodestone or all rations."), [6] = new(6, "Recruit", "Demands characters fight; survivor joins the Cutthroats.") },
    };

    public static PillarEncounterRule Get(int roll) => Encounters.FirstOrDefault(rule => roll >= rule.MinimumRoll && (rule.MaximumRoll is null || roll <= rule.MaximumRoll)) ?? throw new ArgumentOutOfRangeException(nameof(roll));
    public static PillarMoodRule GetMood(string encounterName, int roll) => Moods.TryGetValue(encounterName, out var moods) && moods.TryGetValue(roll, out var mood) ? mood : throw new ArgumentOutOfRangeException(nameof(roll));
}
