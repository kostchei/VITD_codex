namespace VastDark.Domain;

public enum WastesFaction { LodestoneBrokers, Candlekeepers, DustAnglers, PillarWorms }
public sealed record FactionRule(WastesFaction Faction, string Name, string AbilityName, string TrainingRequirement, string Description);

public static class WastesFactionRules
{
    private static readonly IReadOnlyDictionary<WastesFaction, FactionRule> Rules = new Dictionary<WastesFaction, FactionRule>
    {
        [WastesFaction.LodestoneBrokers] = new(WastesFaction.LodestoneBrokers, "Lodestone Brokers", "What's Fair is Fair", "Assist a caravan on a full trade route.", "Freely barter common-for-common or magic-for-magic items at no cost regardless of value difference."),
        [WastesFaction.Candlekeepers] = new(WastesFaction.Candlekeepers, "Candlekeepers", "A Burden Shared", "Join a call to action.", "Take one exhaustion instead when an ally in arm's reach would gain exhaustion or lose a memory."),
        [WastesFaction.DustAnglers] = new(WastesFaction.DustAnglers, "Dust Anglers", "Plenty From Nothing", "Hunt and survive a week with the Dust Anglers.", "In the Wastes with appropriate tools, spend a day to hunt and trap 1d6 rations."),
        [WastesFaction.PillarWorms] = new(WastesFaction.PillarWorms, "Pillar Worms", "Grit and Bear It", "Delve three separate Pillars.", "Gain one exhaustion to perform a task as if equipped with a required or useful tool."),
    };

    public static FactionRule Get(WastesFaction faction) => Rules[faction];
}

public static class WastesFactionService
{
    public static bool CanBarterWithoutCost(bool offeredIsMagic, bool requestedIsMagic) => offeredIsMagic == requestedIsMagic;

    public static bool TryShareBurden(Traveler bearer, Traveler ally, bool withinArmsReach, bool allyWouldGainExhaustion, bool allyWouldLoseMemory)
    {
        ArgumentNullException.ThrowIfNull(bearer);
        ArgumentNullException.ThrowIfNull(ally);
        if (!withinArmsReach || (!allyWouldGainExhaustion && !allyWouldLoseMemory)) return false;
        bearer.AddExhaustion(1);
        return true;
    }

    public static int HuntInWastes(bool inWastes, bool hasAppropriateTools, bool spendsDay, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (!inWastes || !hasAppropriateTools || !spendsDay) return 0;
        return random.Next(1, 7);
    }

    public static void SubstituteForTool(Traveler traveler)
    {
        ArgumentNullException.ThrowIfNull(traveler);
        traveler.AddExhaustion(1);
    }
}
