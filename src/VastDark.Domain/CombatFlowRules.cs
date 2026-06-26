namespace VastDark.Domain;

public sealed record InitiativeEntry(string Name, int DexterityModifier, int NaturalRoll, int Total);

/// <summary>
/// Shadowdark initiative: each combatant rolls 1d20 + DEX modifier; the monster side rolls once using
/// its best DEX. Play proceeds from the highest total down (ties broken by DEX, then name).
/// </summary>
public static class InitiativeRules
{
    public static int Roll(int dexterityModifier, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return random.Next(1, 21) + dexterityModifier;
    }

    public static IReadOnlyList<InitiativeEntry> Order(IEnumerable<(string Name, int DexterityModifier)> combatants, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(combatants);
        ArgumentNullException.ThrowIfNull(random);

        var entries = combatants
            .Select(combatant =>
            {
                var natural = random.Next(1, 21);
                return new InitiativeEntry(combatant.Name, combatant.DexterityModifier, natural, natural + combatant.DexterityModifier);
            })
            .ToList();

        return entries
            .OrderByDescending(entry => entry.Total)
            .ThenByDescending(entry => entry.DexterityModifier)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)
            .ToList();
    }
}

/// <summary>
/// Shadowdark stealth and surprise: a hidden side surprises its target by winning a stealth (DEX)
/// versus awareness (WIS) contest; attacks against a surprised target are made with advantage.
/// </summary>
public static class SurpriseRules
{
    public static bool Surprises(int hiderDexterityModifier, int watcherWisdomModifier, IRandomSource random) =>
        CheckResolver.Contested(hiderDexterityModifier, watcherWisdomModifier, random);

    public static RollMode AttackMode(bool targetSurprised) => targetSurprised ? RollMode.Advantage : RollMode.Normal;
}
