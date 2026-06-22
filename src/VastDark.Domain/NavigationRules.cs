namespace VastDark.Domain;

/// <summary>Navigation assets listed on page 9 of The Vast in the Dark.</summary>
public enum NavigationAsset
{
    Landmark,
    Directions,
    Tool,
    Light,
    DeadReckoning,
}

public enum LostEffect
{
    Late,
    OffCourse,
    DangerouslyOffCourse,
    UtterlyLost,
}

public sealed record NavigationResult(
    int Roll,
    int LostChanceInSix,
    IReadOnlyCollection<NavigationAsset> Assets,
    bool IsLost,
    LostEffect? Effect,
    int DistanceMiles,
    bool RequiresRepeatedNavigation);

public static class DailyNavigationService
{
    public const int BaseLostChanceInSix = 5;

    /// <summary>
    /// Resolves the daily d6 navigation roll. Every distinct source-listed asset
    /// reduces the chance of becoming lost by one, down to zero.
    /// </summary>
    public static NavigationResult Resolve(IEnumerable<NavigationAsset> assets, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(random);

        var preparedAssets = assets.Distinct().Order().ToArray();
        var lostChance = Math.Max(0, BaseLostChanceInSix - preparedAssets.Length);
        var roll = random.Next(1, 7);
        if (roll > lostChance)
        {
            return new NavigationResult(roll, lostChance, preparedAssets, false, null, 0, false);
        }

        // The source's example shows four assets turning its remaining failed
        // result (a roll of 1) into the least severe, "Late", effect. Combining
        // the rolled failure band with prepared assets preserves that behavior
        // across the complete d6 range.
        var severity = Math.Max(0, BaseLostChanceInSix - (roll + preparedAssets.Length));
        var effect = severity switch
        {
            >= 4 => LostEffect.UtterlyLost,
            3 => LostEffect.DangerouslyOffCourse,
            2 => LostEffect.OffCourse,
            _ => LostEffect.Late,
        };

        return effect switch
        {
            LostEffect.Late => new NavigationResult(roll, lostChance, preparedAssets, true, effect, 6, false),
            LostEffect.OffCourse => new NavigationResult(roll, lostChance, preparedAssets, true, effect, 6, false),
            LostEffect.DangerouslyOffCourse => new NavigationResult(roll, lostChance, preparedAssets, true, effect, 12, false),
            LostEffect.UtterlyLost => new NavigationResult(roll, lostChance, preparedAssets, true, effect, 0, true),
            _ => throw new InvalidOperationException(),
        };
    }
}
