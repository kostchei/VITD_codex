namespace VastDark.Domain;

public enum MinotaurTouchEffect { WitherTools, ErodeBody, BreakSpirit, DrinkFlesh, DevourMemory }
public sealed record MinotaurRule(int HitDice, string HitPoints, string Move, string Attack);
public sealed record MinotaurTouchResult(MinotaurTouchEffect Effect, int? Amount = null, bool Permanent = false, bool MemoryLost = false);

public sealed class MinotaurPursuit
{
    public bool HasArrived { get; private set; }
    public bool EnterDeepArea(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (random.Next(1, 7) != 1) return false;
        HasArrived = true;
        return true;
    }
}

public static class MinotaurRules
{
    public static MinotaurRule StatBlock { get; } = new(20, "Cannot be harmed", "Half Standard", "Touch of the Minotaur");
    public static MinotaurTouchResult ResolveTouch(int roll, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return roll switch
        {
            1 => new(MinotaurTouchEffect.WitherTools, random.Next(1, 7)),
            2 or 3 => new(MinotaurTouchEffect.ErodeBody, Roll(random, 3, 6)),
            4 => new(MinotaurTouchEffect.BreakSpirit, random.Next(1, 7)),
            5 => new(MinotaurTouchEffect.DrinkFlesh, random.Next(1, 4), Permanent: true),
            6 => new(MinotaurTouchEffect.DevourMemory, MemoryLost: true),
            _ => throw new ArgumentOutOfRangeException(nameof(roll)),
        };
    }
    private static int Roll(IRandomSource random, int dice, int sides) => Enumerable.Range(0, dice).Sum(_ => random.Next(1, sides + 1));
}
