namespace VastDark.Domain;

/// <summary>Shadowdark's standard difficulty ladder (SD_rules_ref.md section C).</summary>
public enum CheckDifficulty
{
    Easy = 9,
    Normal = 12,
    Hard = 15,
    Extreme = 18,
}

public enum RollMode { Normal, Advantage, Disadvantage }

public static class RollModeRules
{
    /// <summary>
    /// Shadowdark: advantage and disadvantage do not stack; if both apply (e.g. surprise plus cover)
    /// they cancel and you roll a single d20.
    /// </summary>
    public static RollMode Combine(bool advantage, bool disadvantage) =>
        advantage == disadvantage ? RollMode.Normal : advantage ? RollMode.Advantage : RollMode.Disadvantage;
}

public sealed record CheckResult(int NaturalRoll, int Total, int DifficultyClass, bool Success, bool CriticalSuccess, bool CriticalFailure);

/// <summary>
/// The Shadowdark base resolution the Vast text leaves unstated: a d20 ability check against a DC,
/// with advantage/disadvantage and natural-20/natural-1 auto-results. Vast keeps its own Grit/Flesh
/// vitality and ability-mod math (which already equals Shadowdark's table), so only the roll-vs-DC
/// mechanic is supplied here.
/// </summary>
public static class CheckResolver
{
    public static CheckResult Resolve(int abilityModifier, int difficultyClass, IRandomSource random, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(random);
        var natural = RollD20(mode, random);
        var total = natural + abilityModifier;
        return natural switch
        {
            20 => new CheckResult(20, total, difficultyClass, Success: true, CriticalSuccess: true, CriticalFailure: false),
            1 => new CheckResult(1, total, difficultyClass, Success: false, CriticalSuccess: false, CriticalFailure: true),
            _ => new CheckResult(natural, total, difficultyClass, Success: total >= difficultyClass, CriticalSuccess: false, CriticalFailure: false),
        };
    }

    public static CheckResult Resolve(int abilityModifier, CheckDifficulty difficulty, IRandomSource random, RollMode mode = RollMode.Normal) =>
        Resolve(abilityModifier, (int)difficulty, random, mode);

    /// <summary>A contested check: each side rolls 1d20 + modifier; the higher wins and ties are rerolled.</summary>
    public static bool Contested(int challengerModifier, int defenderModifier, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        while (true)
        {
            var challenger = random.Next(1, 21) + challengerModifier;
            var defender = random.Next(1, 21) + defenderModifier;
            if (challenger != defender) return challenger > defender;
        }
    }

    private static int RollD20(RollMode mode, IRandomSource random) => mode switch
    {
        RollMode.Normal => random.Next(1, 21),
        RollMode.Advantage => Math.Max(random.Next(1, 21), random.Next(1, 21)),
        RollMode.Disadvantage => Math.Min(random.Next(1, 21), random.Next(1, 21)),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
}

/// <summary>
/// Maps the Vast's named saves onto Shadowdark ability checks (user-chosen CON/STR/WIS scheme) and
/// resolves a Traveler's save. The Vast text never states a save DC, so callers default to
/// Shadowdark's Normal DC and may override per the situation.
/// </summary>
public static class SaveRules
{
    public static Ability AbilityFor(string saveType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveType);
        return saveType.Trim().ToLowerInvariant() switch
        {
            "breath" => Ability.Constitution,
            "poison" => Ability.Constitution,
            "hold" => Ability.Strength,
            "charm" => Ability.Wisdom,
            "magic" => Ability.Wisdom,
            _ => throw new ArgumentOutOfRangeException(nameof(saveType), $"No ability mapping is defined for save type '{saveType}'."),
        };
    }

    public static CheckResult Resolve(Traveler traveler, string saveType, IRandomSource random, CheckDifficulty difficulty = CheckDifficulty.Normal, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(traveler);
        return CheckResolver.Resolve(traveler.GetAbilityModifier(AbilityFor(saveType)), difficulty, random, mode);
    }

    /// <summary>Resolves a deferred <see cref="SavingThrowDecision"/> against the party member it names.</summary>
    public static CheckResult Resolve(SavingThrowDecision decision, TravelParty party, IRandomSource random, CheckDifficulty difficulty = CheckDifficulty.Normal, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(party);
        var traveler = party.Members.FirstOrDefault(member => string.Equals(member.Name, decision.TravelerName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The save names unknown Traveler '{decision.TravelerName}'.");
        return Resolve(traveler, decision.SaveType, random, difficulty, mode);
    }
}
