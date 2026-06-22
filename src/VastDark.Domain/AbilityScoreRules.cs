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
