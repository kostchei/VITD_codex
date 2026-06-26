namespace VastDark.Domain;

public enum EncounterSource { RoamingHazard, Wastes, Pillar, Ruin, Crawl }

/// <summary>
/// A choice the source rules leave to the players or referee. These are recorded explicitly so
/// callers resolve them deliberately instead of reading intent out of free-form log text.
/// </summary>
public abstract record PendingDecision
{
    public abstract string Prompt { get; }
}

public sealed record SavingThrowDecision(string TravelerName, string SaveType, string FailureConsequence) : PendingDecision
{
    public override string Prompt => $"{TravelerName} must Save versus {SaveType} or {FailureConsequence}.";
}

public sealed record CombatDecision(string EnemyGroup, int? CombatantCount, string? StatBlockName, string Disposition) : PendingDecision
{
    public override string Prompt => $"Combat ({Disposition}): {(CombatantCount is { } count ? count + " " : string.Empty)}{EnemyGroup}.";
}

public sealed record MoodDecision(string EncounterName, string MoodName, string Consequence) : PendingDecision
{
    public override string Prompt => $"{EncounterName} are {MoodName}: {Consequence}";
}

public sealed record TradeDecision(string Partner, int CoinLimit) : PendingDecision
{
    public override string Prompt => $"Trade available with {Partner}; limit {CoinLimit} coin.";
}

public sealed record RefereeChoiceDecision(string ChoicePrompt, IReadOnlyList<string> Options) : PendingDecision
{
    public override string Prompt => Options.Count == 0 ? ChoicePrompt : $"{ChoicePrompt} [{string.Join(" / ", Options)}]";
}

public sealed record WarbandDecision(Warband Warband) : PendingDecision
{
    public override string Prompt =>
        $"Warband: a Demagogue leads {Warband.Cutthroats.Count} Cutthroats. Slaying the Demagogue destroys the hazard.";
}

public sealed record EncounterResolution(
    EncounterSource Source,
    string Title,
    IReadOnlyList<string> Log,
    IReadOnlyList<AppliedDamage> AppliedDamage,
    IReadOnlyList<PendingDecision> PendingDecisions,
    RoamingHazardResolution? RoamingHazard = null)
{
    public string Summary => string.Join(Environment.NewLine, Log.Concat(PendingDecisions.Select(decision => decision.Prompt)));
}

/// <summary>
/// Single entry point that turns every encounter source (roaming hazards, Wastes, Pillar, Ruin, and
/// individual Crawl) into the same structured outcome: applied mechanical damage plus the explicit
/// saves, combats, tributes, trades, moods, and referee choices that remain to be resolved.
/// </summary>
public static class EncounterResolver
{
    public static EncounterResolution ResolveWastes(int encounterRoll, TravelParty party, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(random);
        var rule = WastesEncounterRules.GetEncounter(encounterRoll);
        return ResolveTabledEncounter(
            EncounterSource.Wastes,
            rule.Name,
            rule.Quantity,
            rule.Description,
            rule.RequiresMood,
            roll => { var mood = WastesEncounterRules.GetMood(rule.Name, roll); return (mood.Name, mood.Consequence); },
            party,
            random);
    }

    public static EncounterResolution ResolvePillar(int encounterTotal, TravelParty party, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(random);
        var rule = PillarEncounterRules.Get(encounterTotal);
        return ResolveTabledEncounter(
            EncounterSource.Pillar,
            rule.Name,
            rule.Quantity,
            rule.Description,
            rule.RequiresMood,
            roll => { var mood = PillarEncounterRules.GetMood(rule.Name, roll); return (mood.Name, mood.Consequence); },
            party,
            random);
    }

    public static EncounterResolution ResolveRuin(int encounterTotal, TravelParty party, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(random);
        var rule = RuinEncounterRules.Get(encounterTotal);
        return ResolveTabledEncounter(
            EncounterSource.Ruin,
            rule.Name,
            rule.Quantity,
            rule.Description,
            rule.RequiresMood,
            roll => { var mood = RuinEncounterRules.GetMood(rule.Name, roll); return (mood.Name, mood.Effect); },
            party,
            random);
    }

    public static EncounterResolution ResolveCrawl(CrawlCreature creature, int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        var stats = CrawlCreatureRules.Get(creature);
        return new EncounterResolution(
            EncounterSource.Crawl,
            $"Crawl encounter: {creature}",
            [$"{count} {creature}: {stats.Special}"],
            [],
            [new CombatDecision(creature.ToString(), count, creature.ToString(), "Hostile")]);
    }

    public static EncounterResolution ResolveRoamingHazard(int dieRoll, TravelParty party, RoamingHazardContext context, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(random);

        var hazard = RoamingHazardService.Resolve(dieRoll, party, context, random);
        var log = new List<string> { hazard.Rule.Procedure };
        var applied = new List<AppliedDamage>();
        var decisions = new List<PendingDecision>();
        var travelersByName = party.Members.ToDictionary(traveler => traveler.Name, StringComparer.OrdinalIgnoreCase);

        if (hazard.CombatantCount is { } combatants)
        {
            // The Warband fields its actual roster (a Demagogue + the rolled 5d6 Cutthroats); other
            // combat hazards (e.g. the Crawlherd) remain a generic count for now.
            decisions.Add(string.Equals(hazard.Rule.Name, "Warband", StringComparison.Ordinal)
                ? new WarbandDecision(WarbandRules.Compose(combatants))
                : new CombatDecision(hazard.Rule.Name, combatants, null, "Hostile"));
        }

        foreach (var displacement in hazard.Displacements)
        {
            log.Add($"{displacement.TravelerName} is displaced 1 mile in direction {displacement.Direction}.");
        }

        foreach (var hit in hazard.Damage)
        {
            if (!travelersByName.TryGetValue(hit.TravelerName, out var traveler))
            {
                throw new InvalidOperationException($"Hazard damage references unknown Traveler '{hit.TravelerName}'.");
            }

            var vitality = traveler.TakeDamage(hit.Amount, random);
            applied.Add(new AppliedDamage(hit.TravelerName, hit.Amount, vitality));
            log.Add($"{hit.TravelerName} takes {hit.Amount} damage.");
        }

        foreach (var traveler in hazard.ExhaustedTravelers)
        {
            log.Add($"{traveler} gains 1 exhaustion.");
        }

        foreach (var traveler in hazard.CrushedTravelers)
        {
            decisions.Add(new RefereeChoiceDecision($"{traveler} is caught by the Collapse — resolve death or rescue", ["Crushed", "Rescued"]));
        }

        foreach (var traveler in hazard.BreathSaveTravelers)
        {
            decisions.Add(new SavingThrowDecision(traveler, "Breath", "disappear into the ground"));
        }

        if (hazard.TerrainReducedToWastes)
        {
            log.Add("The terrain may be reduced to Wastes.");
        }

        return new EncounterResolution(EncounterSource.RoamingHazard, hazard.Rule.Name, log, applied, decisions, hazard);
    }

    private static EncounterResolution ResolveTabledEncounter(
        EncounterSource source,
        string name,
        string quantity,
        string description,
        bool requiresMood,
        Func<int, (string Name, string Consequence)> moodLookup,
        TravelParty party,
        IRandomSource random)
    {
        var count = TryRollQuantity(quantity, random);
        var quantityLabel = count is { } value ? value.ToString() : quantity;
        var log = new List<string>
        {
            string.IsNullOrWhiteSpace(quantity) ? $"{name}: {description}" : $"{quantityLabel} {name}: {description}",
        };
        var decisions = new List<PendingDecision>();

        if (!string.Equals(name, "Nothing", StringComparison.OrdinalIgnoreCase))
        {
            if (TryMapCrawl(name, out var creature))
            {
                decisions.Add(new CombatDecision(name, count, creature.ToString(), "Hostile"));
                log.Add($"{creature} special: {CrawlCreatureRules.Get(creature).Special}");
            }
            else if (requiresMood)
            {
                var mood = moodLookup(random.Next(1, 7));
                decisions.Add(new MoodDecision(name, mood.Name, mood.Consequence));
            }
            else if (TryParseCoinLimit(description) is { } coinLimit)
            {
                decisions.Add(new TradeDecision(name, coinLimit));
            }
        }

        return new EncounterResolution(source, $"{source} encounter: {name}", log, [], decisions);
    }

    private static bool TryMapCrawl(string name, out CrawlCreature creature)
    {
        creature = name.Trim().ToLowerInvariant() switch
        {
            "cyclops" => CrawlCreature.Cyclops,
            "medusa" => CrawlCreature.Medusa,
            "harpy" or "harpies" => CrawlCreature.Harpy,
            "griffon" => CrawlCreature.Griffon,
            "siren" or "sirens" => CrawlCreature.Siren,
            "centaur" => CrawlCreature.Centaur,
            "hydra" => CrawlCreature.Hydra,
            "shade" or "shades" => CrawlCreature.Shade,
            "ogre" => CrawlCreature.Ogre,
            "wyrm" => CrawlCreature.Wyrm,
            _ => (CrawlCreature)(-1),
        };
        return (int)creature >= 0;
    }

    private static int? TryRollQuantity(string quantity, IRandomSource random)
    {
        if (string.IsNullOrWhiteSpace(quantity)) return null;
        var text = quantity.Trim();
        if (int.TryParse(text, out var fixedCount)) return fixedCount;

        var dieIndex = text.IndexOf('d');
        if (dieIndex <= 0) return null;
        if (!int.TryParse(text[..dieIndex], out var dice) ||
            !int.TryParse(text[(dieIndex + 1)..], out var sides) ||
            dice < 1 || sides < 1)
        {
            return null;
        }

        return Enumerable.Range(0, dice).Sum(_ => random.Next(1, sides + 1));
    }

    private static int? TryParseCoinLimit(string description)
    {
        const string marker = "limit ";
        var index = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;
        var rest = description[(index + marker.Length)..].TrimStart();
        var digits = new string(rest.TakeWhile(char.IsDigit).ToArray());
        return digits.Length > 0 && int.TryParse(digits, out var limit) ? limit : null;
    }
}
