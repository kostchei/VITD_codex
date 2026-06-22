namespace VastDark.Domain;

public enum SettlementDenizen { Nod, Masque, FlayedDervish, Sindr, OldTune, Hool, Skitter, Dive }
public enum SettlementFaction { PartisansOfFlame, SeekerKeepers, BlackHelms, Grafters }
public sealed record DenizenRule(SettlementDenizen Denizen, string Name, string Offer, string Obligation);
public sealed record SettlementFactionAbility(SettlementFaction Faction, string Name, string Ability, string TrainingRequirement);

public static class SettlementDenizenRules
{
    private static readonly IReadOnlyDictionary<SettlementDenizen, DenizenRule> Denizens = new Dictionary<SettlementDenizen, DenizenRule>
    {
        [SettlementDenizen.Nod] = new(SettlementDenizen.Nod, "Nod", "Find anyone in the Vast and bring them back.", "Pay raw lodestone and Crawl teeth."),
        [SettlementDenizen.Masque] = new(SettlementDenizen.Masque, "Masque", "Trade equally potent dangerous information.", "Tell a secret that should not be known or is dangerous."),
        [SettlementDenizen.FlayedDervish] = new(SettlementDenizen.FlayedDervish, "Flayed Dervish", "Fights beside the party.", "Help hunt seven notorious bandit Cutthroats."),
        [SettlementDenizen.Sindr] = new(SettlementDenizen.Sindr, "Sindr", "Pursues the five Holds of Fire.", "Find the hidden fire workshops."),
        [SettlementDenizen.OldTune] = new(SettlementDenizen.OldTune, "Old Tune", "Gifts a comforting flute.", "Return a song from the depths sung by Crawl and dark echoes."),
        [SettlementDenizen.Hool] = new(SettlementDenizen.Hool, "Hool", "Lodging, food, and small gifts.", "Protect them between settlements."),
        [SettlementDenizen.Skitter] = new(SettlementDenizen.Skitter, "Skitter", "Begins a collective-psyche escape work.", "Bring ancient lab formulas and reagents."),
        [SettlementDenizen.Dive] = new(SettlementDenizen.Dive, "Dive", "Pays increasingly for each safe ruin depth reached.", "Grant safe passage into the depths."),
    };
    private static readonly IReadOnlyDictionary<SettlementFaction, SettlementFactionAbility> Factions = new Dictionary<SettlementFaction, SettlementFactionAbility>
    {
        [SettlementFaction.PartisansOfFlame] = new(SettlementFaction.PartisansOfFlame, "Partisans of Flame", "Craft Jarred Fire in 1d3 hours with materials.", "Gift an artifact to a pyromancer."),
        [SettlementFaction.SeekerKeepers] = new(SettlementFaction.SeekerKeepers, "Seeker Keepers", "Once per day produce one poor common tool or item; it breaks after use.", "Gift something important from before arriving."),
        [SettlementFaction.BlackHelms] = new(SettlementFaction.BlackHelms, "Black Helms", "For each memory lost gain 1d6 Grit and an additional combat attack.", "Defeat a Black Helm in ritual combat."),
        [SettlementFaction.Grafters] = new(SettlementFaction.Grafters, "Grafters", "Take or sacrifice 1d6 Grit from a willing host to heal 1 Flesh.", "Assist their arts on three occasions."),
    };

    public static DenizenRule GetDenizen(SettlementDenizen denizen) => Denizens[denizen];
    public static SettlementFactionAbility GetFaction(SettlementFaction faction) => Factions[faction];
}

public sealed class SettlementFactionUses
{
    private bool _seekerKeeperUsed;
    public string ProduceSeekerKeeperItem(string itemName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemName);
        if (_seekerKeeperUsed) throw new InvalidOperationException("Inscrutable Pockets is usable once per day.");
        _seekerKeeperUsed = true;
        return $"Poor {itemName} (breaks after serving its purpose)";
    }
    public void ResetDay() => _seekerKeeperUsed = false;
}

public static class SettlementFactionService
{
    public static int CraftJarredFireHours(bool hasMaterials, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return hasMaterials ? random.Next(1, 4) : throw new InvalidOperationException("Jarred Fire requires its materials.");
    }
    public static (int Grit, int AdditionalAttacks) BlackHelmMemoryLossBenefit(int memoriesLost, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (memoriesLost < 0) throw new ArgumentOutOfRangeException(nameof(memoriesLost));
        return (Enumerable.Range(0, memoriesLost).Sum(_ => random.Next(1, 7)), memoriesLost);
    }
    public static (int GritSacrificed, int FleshHealed) GraftFromWillingHost(bool willing, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (!willing) throw new InvalidOperationException("Grafters require a willing host.");
        return (random.Next(1, 7), 1);
    }
}
