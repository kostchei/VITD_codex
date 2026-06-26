namespace VastDark.Domain;

/// <summary>An NdM dice expression (e.g. "1d8", "2d6"), used for weapon and effect damage.</summary>
public readonly record struct DiceExpression(int Count, int Sides)
{
    public static DiceExpression Parse(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        var text = expression.Trim();
        var index = text.IndexOf('d', StringComparison.OrdinalIgnoreCase);
        if (index <= 0 || index == text.Length - 1 ||
            !int.TryParse(text[..index], out var count) ||
            !int.TryParse(text[(index + 1)..], out var sides) ||
            count < 1 || sides < 1)
        {
            throw new FormatException($"'{expression}' is not a valid NdM dice expression.");
        }

        return new DiceExpression(count, sides);
    }

    /// <summary>Rolls the expression. On a critical hit the number of dice is doubled (Shadowdark crit).</summary>
    public int Roll(IRandomSource random, bool doubleDice = false)
    {
        ArgumentNullException.ThrowIfNull(random);
        var rolls = doubleDice ? Count * 2 : Count;
        var total = 0;
        for (var index = 0; index < rolls; index++)
        {
            total += random.Next(1, Sides + 1);
        }

        return total;
    }
}
