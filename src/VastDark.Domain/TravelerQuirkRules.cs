namespace VastDark.Domain;

public sealed record TravelerQuirkDefinition(int Roll, string Name, string RuleText, bool CanBeTakenMultipleTimes = false);

/// <summary>Page 6's character-creation and advancement 1d20 Traveler quirks.</summary>
public static class TravelerQuirkRules
{
    private static readonly IReadOnlyDictionary<int, TravelerQuirkDefinition> Quirks = new Dictionary<int, TravelerQuirkDefinition>
    {
        [1] = new(1, "Ruin Plucker", "Gain one extra inventory slot; loathe leaving things behind.", true),
        [2] = new(2, "Enigmatic Paranoia", "Sense when being followed or tracked, and whisper it aloud without realizing."),
        [3] = new(3, "Hollow Fortitude", "On a 3-in-6 chance, do not suffer exhaustion when you normally would."),
        [4] = new(4, "Labrinthiosis", "In a structure, meditate for 10 minutes to predict the next 1d6 rooms."),
        [5] = new(5, "Magnetoception", "Meditate for one hour to locate and navigate back to that location; a new location replaces the old one."),
        [6] = new(6, "Vacant Amygdala", "Cannot feel supernatural or ordinary fear."),
        [7] = new(7, "Distant Appetite", "Go without food for up to 1d6 days."),
        [8] = new(8, "Vampyr", "Drink 1d6 HP of fresh blood in place of a meal."),
        [9] = new(9, "Vicious Abandon", "Sacrifice a weapon to automatically hit and deal maximum damage to an assailant."),
        [10] = new(10, "Wind Seer", "Predict weather in the Vast perfectly."),
        [11] = new(11, "Dreamless", "Need sleep only every 1d6 days."),
        [12] = new(12, "Unreadable", "Others cannot read motives or emotions."),
        [13] = new(13, "Psychitabolism", "Eat brains to acquire simple memories: people give names and secrets; animals give shelter and food locations."),
        [14] = new(14, "Psionherd", "Hypnotize non-hostile small Vast creatures to hold still, follow, or leave."),
        [15] = new(15, "Long-walker", "Travel 6 extra miles each day with no ill effect."),
        [16] = new(16, "Gentle Presence", "Non-hostile Travelers are friendly or helpful."),
        [17] = new(17, "Candles in the Dark", "See in the dark for 1d6 hours after light is gone."),
        [18] = new(18, "Cold Blood", "Suffer no harm from cold weather and half damage from cold attacks."),
        [19] = new(19, "Dull Psyche", "Have advantage on resisting charms and mental compulsion."),
        [20] = new(20, "Memetic", "Duplicate one observed Traveler ability or quirk after observing them for more than a day."),
    };

    public static TravelerQuirkDefinition Get(int roll) => Quirks.TryGetValue(roll, out var quirk)
        ? quirk
        : throw new ArgumentOutOfRangeException(nameof(roll), "Traveler quirks use a 1d20 roll.");

    public static TravelerQuirkDefinition Roll(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return Get(random.Next(1, 21));
    }
}
