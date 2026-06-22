namespace VastDark.Domain;

public interface IRandomSource
{
    int Next(int minInclusive, int maxExclusive);
}

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    public SystemRandomSource(Random? random = null) => _random = random ?? Random.Shared;

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}

public enum ExhaustionSource { Unspecified, LostSleep, SevereWound, Hunger, ForcedMarch }

public sealed class Traveler
{
    private readonly Dictionary<string, int> _skills = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _resources = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _conditions = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ExhaustionSource> _exhaustionSources = [];

    public Traveler(string name, int health = 10, int rations = 0, AbilityScores? abilityScores = null, int level = 1, Vitality? vitality = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (health < 0 || rations < 0 || level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(health), "Health and rations cannot be negative.");
        }

        Name = name;
        Health = health;
        Rations = rations;
        AbilityScores = abilityScores ?? AbilityScores.Average;
        Level = level;
        Vitality = vitality;
    }

    public Traveler(TravelerState state)
        : this(
            state?.Name ?? throw new ArgumentNullException(nameof(state)),
            state.Health,
            state.Rations,
            state.AbilityScores,
            state.Level,
            state.Vitality)
    {
        if (state.ExhaustionSources is { Count: > 0 })
        {
            foreach (var source in state.ExhaustionSources.Take(state.Exhaustion))
            {
                AddExhaustion(1, source);
            }
        }

        AddExhaustion(state.Exhaustion - Exhaustion);
        foreach (var skill in state.Skills ?? [])
        {
            SetSkill(skill.Name, skill.Value);
        }

        foreach (var resource in state.Resources ?? [])
        {
            SetResource(resource.Name, resource.Value);
        }

        foreach (var condition in state.Conditions ?? [])
        {
            AddCondition(condition);
        }
    }

    public string Name { get; }
    public int Health { get; private set; }
    public int Rations { get; private set; }
    public int Exhaustion { get; private set; }
    public int Level { get; }
    public Vitality? Vitality { get; private set; }
    public IReadOnlyList<ExhaustionSource> ExhaustionSources => _exhaustionSources;
    public AbilityScores AbilityScores { get; }
    public IReadOnlyCollection<string> Conditions => _conditions;
    public IReadOnlyDictionary<string, int> Skills => _skills;
    public IReadOnlyDictionary<string, int> Resources => _resources;

    public int GetAbilityScore(Ability ability) => AbilityScores[ability];

    public int GetAbilityModifier(Ability ability) => AbilityScores.Modifier(ability);

    public static Traveler CreateWithVitality(string name, AbilityScores scores, int level, IEnumerable<int> d8Rolls, int rations = 0) =>
        new(name, rations: rations, abilityScores: scores, level: level, vitality: VitalityRules.CreateStartingVitality(level, scores, d8Rolls));

    public void SetSkill(string skill, int value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skill);
        _skills[skill] = value;
    }

    public int GetSkill(string skill) => _skills.TryGetValue(skill, out var value) ? value : 0;

    public void SetResource(string resource, int amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        _resources[resource] = amount;
    }

    public int GetResource(string resource) => _resources.TryGetValue(resource, out var amount) ? amount : 0;

    public bool ConsumeRation()
    {
        if (Rations == 0)
        {
            return false;
        }

        Rations--;
        return true;
    }

    public void AddRations(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Rations += amount;
    }

    public void AddExhaustion(int levels, ExhaustionSource source = ExhaustionSource.Unspecified)
    {
        if (levels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(levels));
        }

        Exhaustion += levels;
        _exhaustionSources.AddRange(Enumerable.Repeat(source, levels));
    }

    public bool RecoverExhaustionFromFullRest()
    {
        if (Exhaustion == 0) return false;
        Exhaustion--;
        _exhaustionSources.RemoveAt(_exhaustionSources.Count - 1);
        return true;
    }

    public void DealDamage(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Health = Math.Max(0, Health - amount);
    }

    public DamageResolution? TakeDamage(int amount, IRandomSource random)
    {
        if (Vitality is null)
        {
            DealDamage(amount);
            return null;
        }

        var resolution = VitalityRules.ApplyDamage(Vitality, amount, random);
        Vitality = resolution.Vitality;
        if (resolution.InjuryRequired) AddExhaustion(1, ExhaustionSource.SevereWound);
        return resolution;
    }

    public void RecoverGritAfterRest(bool fullDayOfRest, IRandomSource random)
    {
        if (Vitality is not null) Vitality = VitalityRules.RecoverGritAfterRest(Vitality, fullDayOfRest, random);
    }

    public void RecoverFleshAtSettlement()
    {
        if (Vitality is not null) Vitality = VitalityRules.RecoverFleshAtSettlement(Vitality);
    }

    public bool TryLoseResource(string resource, int amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        var current = GetResource(resource);
        if (current < amount)
        {
            return false;
        }

        _resources[resource] = current - amount;
        return true;
    }

    public void AddCondition(string condition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(condition);
        _conditions.Add(condition);
    }

    public TravelerState ToState() => new(
        Name,
        Health,
        Rations,
        Exhaustion,
        _skills.OrderBy(skill => skill.Key).Select(skill => new NamedValueState(skill.Key, skill.Value)).ToList(),
        _resources.OrderBy(resource => resource.Key).Select(resource => new NamedValueState(resource.Key, resource.Value)).ToList(),
        _conditions.Order().ToList(),
        AbilityScores,
        _exhaustionSources.ToList(),
        Level,
        Vitality);
}

public sealed class TravelParty
{
    private readonly List<Traveler> _members;

    public TravelParty(IEnumerable<Traveler> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        _members = members.ToList();
        if (_members.Count == 0)
        {
            throw new ArgumentException("A travel party requires at least one traveler.", nameof(members));
        }
    }

    public IReadOnlyList<Traveler> Members => _members;
    public bool MustStopTraveling { get; private set; }
    public int TotalRations => _members.Sum(member => member.Rations);
    public int TotalExhaustion => _members.Sum(member => member.Exhaustion);

    public int BestSkill(string skill) => _members.Max(member => member.GetSkill(skill));

    public void StopTraveling() => MustStopTraveling = true;

    public void RequireRest() => MustStopTraveling = true;

    public void CompleteRest() => MustStopTraveling = false;

    public PartyState ToState() => new(_members.Select(member => member.ToState()).ToList());
}

public sealed record TravelSegment
{
    public TravelSegment(Terrain terrain, int miles)
    {
        if (miles < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(miles), "Travel segments must be at least one mile.");
        }

        Terrain = terrain;
        Miles = miles;
    }

    public Terrain Terrain { get; }
    public int Miles { get; }
}

public enum TravelEventKind
{
    Weather,
    Encounter,
}

public enum TravelEventCadence
{
    PerSegment,
    PerTravelPeriod,
}

public sealed class TravelEventDefinition
{
    public TravelEventDefinition(string name, IEnumerable<ITravelEffect>? effects = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Effects = (effects ?? []).ToArray();
    }

    public string Name { get; }
    public IReadOnlyList<ITravelEffect> Effects { get; }
}

public sealed class TravelEventTable
{
    private readonly List<TravelEventDefinition> _outcomes;

    public TravelEventTable(IEnumerable<TravelEventDefinition> outcomes)
    {
        ArgumentNullException.ThrowIfNull(outcomes);
        _outcomes = outcomes.ToList();
        if (_outcomes.Count == 0)
        {
            throw new ArgumentException("An event table requires at least one outcome.", nameof(outcomes));
        }
    }

    public TravelEventDefinition Draw(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return _outcomes[random.Next(0, _outcomes.Count)];
    }
}

public sealed record TerrainTravelProfile(TravelEventTable WeatherTable, TravelEventTable EncounterTable);

public sealed class TravelWorld
{
    private readonly Dictionary<Terrain, TerrainTravelProfile> _terrainProfiles;

    public TravelWorld(
        WastesDeck wastesDeck,
        IReadOnlyDictionary<Terrain, TerrainTravelProfile> terrainProfiles,
        TravelEventCadence travelEventCadence = TravelEventCadence.PerSegment)
    {
        ArgumentNullException.ThrowIfNull(wastesDeck);
        ArgumentNullException.ThrowIfNull(terrainProfiles);
        WastesDeck = wastesDeck;
        _terrainProfiles = new Dictionary<Terrain, TerrainTravelProfile>(terrainProfiles);
        TravelEventCadence = travelEventCadence;
    }

    public WastesDeck WastesDeck { get; }
    public TravelEventCadence TravelEventCadence { get; }

    public bool TryGetTerrainProfile(Terrain terrain, out TerrainTravelProfile profile)
    {
        if (_terrainProfiles.TryGetValue(terrain, out var configuredProfile))
        {
            profile = configuredProfile;
            return true;
        }

        profile = null!;
        return false;
    }
}

public sealed class ResolutionContext
{
    public ResolutionContext(TravelParty party, TravelWorld world, IRandomSource random, List<string> log)
    {
        Party = party;
        World = world;
        Random = random;
        Log = log;
    }

    public TravelParty Party { get; }
    public TravelWorld World { get; }
    public IRandomSource Random { get; }
    public List<string> Log { get; }
}

public interface ITravelEffect
{
    void Apply(ResolutionContext context);
}

public sealed record AddExhaustionEffect(int Levels) : ITravelEffect
{
    public void Apply(ResolutionContext context)
    {
        foreach (var traveler in context.Party.Members)
        {
            traveler.AddExhaustion(Levels);
        }

        context.Log.Add($"Party gains {Levels} exhaustion.");
    }
}

public sealed record DamageEffect(int Amount) : ITravelEffect
{
    public void Apply(ResolutionContext context)
    {
        foreach (var traveler in context.Party.Members)
        {
            traveler.TakeDamage(Amount, context.Random);
        }

        context.Log.Add($"Party takes {Amount} damage.");
    }
}

public sealed record AddConditionEffect(string Condition) : ITravelEffect
{
    public void Apply(ResolutionContext context)
    {
        foreach (var traveler in context.Party.Members)
        {
            traveler.AddCondition(Condition);
        }

        context.Log.Add($"Party gains condition: {Condition}.");
    }
}

public sealed record LoseResourceEffect(string Resource, int Amount, ITravelEffect? Fallback = null) : ITravelEffect
{
    public void Apply(ResolutionContext context)
    {
        var paid = context.Party.Members.All(traveler => traveler.GetResource(Resource) >= Amount);
        if (paid)
        {
            foreach (var traveler in context.Party.Members)
            {
                traveler.TryLoseResource(Resource, Amount);
            }

            context.Log.Add($"Party loses {Amount} {Resource}.");
            return;
        }

        context.Log.Add($"Party cannot lose {Amount} {Resource}.");
        Fallback?.Apply(context);
    }
}

public sealed class StopTravelEffect : ITravelEffect
{
    public void Apply(ResolutionContext context)
    {
        context.Party.StopTraveling();
        context.Log.Add("Travel must stop.");
    }
}

public sealed record TravelEventResult(TravelEventKind Kind, Terrain Terrain, string Name);

public sealed record TravelDayResult(
    int MilesTravelled,
    int ForcedMarchLevels,
    IReadOnlyList<TravelEventResult> Events,
    IReadOnlyList<string> Log,
    bool RestRequired);

public sealed class TravelService
{
    public const int NormalDailyMiles = 18;
    public const int ExtraMilesPerExhaustionLevel = 6;

    public TravelDayResult TravelDay(
        TravelParty party,
        IEnumerable<TravelSegment> route,
        TravelWorld world,
        int forcedMarchLevels,
        IRandomSource? random = null)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(world);
        if (forcedMarchLevels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(forcedMarchLevels));
        }

        random ??= new SystemRandomSource();
        var routeSegments = route.ToArray();
        var log = new List<string>();
        var context = new ResolutionContext(party, world, random, log);
        var events = new List<TravelEventResult>();
        var milesTravelled = 0;
        var segmentIndex = 0;
        var milesIntoSegment = 0;

        foreach (var traveler in party.Members)
        {
            if (traveler.ConsumeRation())
            {
                log.Add($"{traveler.Name} consumes 1 ration.");
            }
            else
            {
                traveler.AddExhaustion(1, ExhaustionSource.Hunger);
                log.Add($"{traveler.Name} gains 1 exhaustion because no ration was available.");
            }
        }

        MoveForMiles(NormalDailyMiles, isForcedMarch: false);
        var usedForcedMarchLevels = 0;
        while (!party.MustStopTraveling && usedForcedMarchLevels < forcedMarchLevels && segmentIndex < routeSegments.Length)
        {
            foreach (var traveler in party.Members)
            {
                traveler.AddExhaustion(1, ExhaustionSource.ForcedMarch);
            }

            usedForcedMarchLevels++;
            log.Add("Party gains 1 exhaustion to travel 6 additional miles.");
            MoveForMiles(ExtraMilesPerExhaustionLevel, isForcedMarch: true);
        }

        party.RequireRest();
        return new TravelDayResult(milesTravelled, usedForcedMarchLevels, events, log, RestRequired: true);

        void MoveForMiles(int capacity, bool isForcedMarch)
        {
            Terrain? lastTerrain = null;
            while (capacity > 0 && segmentIndex < routeSegments.Length && !party.MustStopTraveling)
            {
                var segment = routeSegments[segmentIndex];
                var milesRemainingInSegment = segment.Miles - milesIntoSegment;
                var milesMoved = Math.Min(capacity, milesRemainingInSegment);
                capacity -= milesMoved;
                milesIntoSegment += milesMoved;
                milesTravelled += milesMoved;
                lastTerrain = segment.Terrain;
                log.Add($"Travel {milesMoved} mile(s) through {segment.Terrain}{(isForcedMarch ? " on a forced march" : string.Empty)}.");

                if (world.TravelEventCadence == TravelEventCadence.PerSegment)
                {
                    ResolveTerrainEvents(segment.Terrain);
                }

                if (milesIntoSegment == segment.Miles)
                {
                    segmentIndex++;
                    milesIntoSegment = 0;
                }
            }

            if (world.TravelEventCadence == TravelEventCadence.PerTravelPeriod && lastTerrain is { } terrain && !party.MustStopTraveling)
            {
                ResolveTerrainEvents(terrain);
            }
        }

        void ResolveTerrainEvents(Terrain terrain)
        {
            if (!world.TryGetTerrainProfile(terrain, out var profile))
            {
                log.Add($"No travel-event profile is configured for {terrain}.");
                return;
            }

            ResolveEvent(TravelEventKind.Weather, terrain, profile.WeatherTable.Draw(random));
            if (!party.MustStopTraveling)
            {
                ResolveEvent(TravelEventKind.Encounter, terrain, profile.EncounterTable.Draw(random));
            }
        }

        void ResolveEvent(TravelEventKind kind, Terrain terrain, TravelEventDefinition definition)
        {
            events.Add(new TravelEventResult(kind, terrain, definition.Name));
            log.Add($"{kind} in {terrain}: {definition.Name}.");
            foreach (var effect in definition.Effects)
            {
                effect.Apply(context);
            }
        }
    }
}

public sealed class WastesDeck
{
    private readonly List<WastesCard> _cards;

    public WastesDeck(IEnumerable<WastesCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        _cards = cards.ToList();
        if (_cards.Count == 0)
        {
            throw new ArgumentException("A Wastes deck requires at least one card.", nameof(cards));
        }
    }

    public WastesCard Draw(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return _cards[random.Next(0, _cards.Count)];
    }
}

public sealed class WastesCard
{
    private readonly IReadOnlyDictionary<int, WastesEntry> _outcomes;

    public WastesCard(string name, IReadOnlyDictionary<int, WastesEntry> outcomes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(outcomes);
        if (outcomes.Count != 17 || Enumerable.Range(2, 17).Any(total => !outcomes.ContainsKey(total)))
        {
            throw new ArgumentException("Wastes cards must provide one outcome for every total from 2 through 18.", nameof(outcomes));
        }

        Name = name;
        _outcomes = new Dictionary<int, WastesEntry>(outcomes);
    }

    public string Name { get; }

    public WastesEntry GetOutcome(int total) =>
        _outcomes.TryGetValue(total, out var entry)
            ? entry
            : throw new InvalidOperationException($"Wastes card '{Name}' has no outcome for total {total}.");
}

public sealed record WastesEntry(string Title, IReadOnlyList<WastesStep> Steps);

public sealed record WastesStepResult(
    IReadOnlyList<ITravelEffect> Effects,
    bool EndsEncounter = false,
    string? CombatEnemyGroup = null);

public interface IWastesDecisionProvider
{
    string Choose(string prompt, IReadOnlyList<string> options);
}

public sealed class FirstOptionDecisionProvider : IWastesDecisionProvider
{
    public string Choose(string prompt, IReadOnlyList<string> options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        if (options.Count == 0)
        {
            throw new ArgumentException("A choice requires at least one option.", nameof(options));
        }

        return options[0];
    }
}

public abstract class WastesStep
{
    public abstract WastesStepResult Resolve(ResolutionContext context, IWastesDecisionProvider decisions);
}

public sealed class EffectsStep : WastesStep
{
    private readonly WastesStepResult _result;

    public EffectsStep(IEnumerable<ITravelEffect> effects, bool endsEncounter = false)
    {
        ArgumentNullException.ThrowIfNull(effects);
        _result = new WastesStepResult(effects.ToArray(), endsEncounter);
    }

    public override WastesStepResult Resolve(ResolutionContext context, IWastesDecisionProvider decisions) => _result;
}

public sealed class RollStep : WastesStep
{
    private readonly IReadOnlyDictionary<int, WastesStepResult> _outcomes;

    public RollStep(int modifier, IReadOnlyDictionary<int, WastesStepResult> outcomes)
    {
        ArgumentNullException.ThrowIfNull(outcomes);
        Modifier = modifier;
        _outcomes = new Dictionary<int, WastesStepResult>(outcomes);
    }

    public int Modifier { get; }

    public override WastesStepResult Resolve(ResolutionContext context, IWastesDecisionProvider decisions)
    {
        var total = context.Random.Next(1, 13) + context.Random.Next(1, 7) + Modifier;
        context.Log.Add($"Roll step total: {total}.");
        return _outcomes.TryGetValue(total, out var outcome)
            ? outcome
            : throw new InvalidOperationException($"Roll step has no outcome for total {total}.");
    }
}

public sealed class TestStep : WastesStep
{
    public TestStep(string skill, int difficulty, int modifier, WastesStepResult onPass, WastesStepResult onFail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skill);
        Skill = skill;
        Difficulty = difficulty;
        Modifier = modifier;
        OnPass = onPass;
        OnFail = onFail;
    }

    public string Skill { get; }
    public int Difficulty { get; }
    public int Modifier { get; }
    public WastesStepResult OnPass { get; }
    public WastesStepResult OnFail { get; }

    public override WastesStepResult Resolve(ResolutionContext context, IWastesDecisionProvider decisions)
    {
        var score = context.Random.Next(1, 13) + context.Party.BestSkill(Skill) + Modifier;
        var passed = score >= Difficulty;
        context.Log.Add($"{Skill} test: {score} vs {Difficulty} ({(passed ? "pass" : "fail")}).");
        return passed ? OnPass : OnFail;
    }
}

public sealed class ChoiceStep : WastesStep
{
    private readonly IReadOnlyDictionary<string, WastesStepResult> _outcomes;

    public ChoiceStep(string prompt, IReadOnlyDictionary<string, WastesStepResult> outcomes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(outcomes);
        if (outcomes.Count == 0)
        {
            throw new ArgumentException("A choice requires at least one outcome.", nameof(outcomes));
        }

        Prompt = prompt;
        _outcomes = new Dictionary<string, WastesStepResult>(outcomes, StringComparer.OrdinalIgnoreCase);
    }

    public string Prompt { get; }

    public override WastesStepResult Resolve(ResolutionContext context, IWastesDecisionProvider decisions)
    {
        var choice = decisions.Choose(Prompt, _outcomes.Keys.ToArray());
        context.Log.Add($"Choice: {choice}.");
        return _outcomes.TryGetValue(choice, out var outcome)
            ? outcome
            : throw new InvalidOperationException($"Choice '{choice}' is not available.");
    }
}

public sealed class CombatStep : WastesStep
{
    private readonly WastesStepResult _result;

    public CombatStep(string enemyGroup, IEnumerable<ITravelEffect>? effects = null, bool endsEncounter = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enemyGroup);
        EnemyGroup = enemyGroup;
        _result = new WastesStepResult((effects ?? []).ToArray(), endsEncounter, enemyGroup);
    }

    public string EnemyGroup { get; }

    public override WastesStepResult Resolve(ResolutionContext context, IWastesDecisionProvider decisions)
    {
        context.Log.Add($"Combat starts: {EnemyGroup}.");
        return _result;
    }
}

public sealed record WastesResolutionResult(
    WastesCard Card,
    int RollTotal,
    WastesEntry Entry,
    IReadOnlyList<string> Log,
    string? CombatEnemyGroup);

public sealed class WastesService
{
    public WastesResolutionResult ResolveWastesEncounter(
        TravelParty party,
        TravelWorld world,
        IRandomSource? random = null,
        IWastesDecisionProvider? decisions = null)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(world);
        random ??= new SystemRandomSource();
        decisions ??= new FirstOptionDecisionProvider();

        var log = new List<string>();
        var context = new ResolutionContext(party, world, random, log);
        var card = world.WastesDeck.Draw(random);
        var rollTotal = random.Next(1, 13) + random.Next(1, 7);
        var entry = card.GetOutcome(rollTotal);
        var pendingEffects = new List<ITravelEffect>();
        string? combatEnemyGroup = null;
        log.Add($"Wastes: {card.Name} / {entry.Title} (roll {rollTotal}).");

        foreach (var step in entry.Steps)
        {
            var result = step.Resolve(context, decisions);
            pendingEffects.AddRange(result.Effects);
            combatEnemyGroup ??= result.CombatEnemyGroup;
            if (result.EndsEncounter)
            {
                break;
            }
        }

        foreach (var effect in pendingEffects)
        {
            effect.Apply(context);
        }

        return new WastesResolutionResult(card, rollTotal, entry, log, combatEnemyGroup);
    }
}
