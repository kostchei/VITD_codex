namespace VastDark.Domain;

/// <summary>DCC-style ability scores: 3d6, with floor((score - 10) / 2) modifiers.</summary>
public static class AbilityScoreRules
{
    public const int MinimumScore = 3;
    public const int MaximumScore = 18;

    public static int Roll3d6(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return random.Next(1, 7) + random.Next(1, 7) + random.Next(1, 7);
    }

    public static int Modifier(int score)
    {
        if (score is < MinimumScore or > MaximumScore)
        {
            throw new ArgumentOutOfRangeException(nameof(score));
        }

        return (int)Math.Floor((score - 10) / 2d);
    }
}

/// <summary>The six DCC ability scores carried by a traveler.</summary>
public sealed record AbilityScores
{
    public static AbilityScores Average { get; } = new(10, 10, 10, 10, 10, 10);

    public AbilityScores(int strength, int dexterity, int constitution, int wisdom, int intelligence, int charisma)
    {
        foreach (var score in new[] { strength, dexterity, constitution, wisdom, intelligence, charisma })
        {
            if (score is < AbilityScoreRules.MinimumScore or > AbilityScoreRules.MaximumScore)
            {
                throw new ArgumentOutOfRangeException(nameof(strength), "Ability scores must be between 3 and 18.");
            }
        }

        Strength = strength;
        Dexterity = dexterity;
        Constitution = constitution;
        Wisdom = wisdom;
        Intelligence = intelligence;
        Charisma = charisma;
    }

    public int Strength { get; }
    public int Dexterity { get; }
    public int Constitution { get; }
    public int Wisdom { get; }
    public int Intelligence { get; }
    public int Charisma { get; }

    public int this[Ability ability] => ability switch
    {
        Ability.Strength => Strength,
        Ability.Dexterity => Dexterity,
        Ability.Constitution => Constitution,
        Ability.Wisdom => Wisdom,
        Ability.Intelligence => Intelligence,
        Ability.Charisma => Charisma,
        _ => throw new ArgumentOutOfRangeException(nameof(ability)),
    };

    public int Modifier(Ability ability) => AbilityScoreRules.Modifier(this[ability]);

    public int Get(Ability ability) => this[ability];

    public int HighestModifier => Enum.GetValues<Ability>().Max(Modifier);

    public static AbilityScores RollDcc(IRandomSource random) => new(
        AbilityScoreRules.Roll3d6(random),
        AbilityScoreRules.Roll3d6(random),
        AbilityScoreRules.Roll3d6(random),
        AbilityScoreRules.Roll3d6(random),
        AbilityScoreRules.Roll3d6(random),
        AbilityScoreRules.Roll3d6(random));
}
