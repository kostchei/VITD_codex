namespace VastDark.Domain;

public enum DyingOutcome { Dying, Revived, Dead }

/// <summary>
/// Shadowdark hit points for combatants that do not use the Vast's Grit/Flesh vitality (creatures and
/// plain Travelers). Replaces the old fixed <c>Health = 10</c> placeholder with rolled HP and the
/// Shadowdark death-and-dying state machine.
/// </summary>
public sealed record HitPoints(int Maximum, int Current, bool IsDying = false, int DeathTimer = 0)
{
    public bool IsDown => Current <= 0;
}

public static class HitPointRules
{
    /// <summary>Roll max HP: a hit die plus the Constitution modifier per level, minimum 1 per level.</summary>
    public static HitPoints Roll(int level, int hitDieSides, int constitutionModifier, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        if (hitDieSides < 1) throw new ArgumentOutOfRangeException(nameof(hitDieSides));

        var maximum = 0;
        for (var index = 0; index < level; index++)
        {
            maximum += Math.Max(1, random.Next(1, hitDieSides + 1) + constitutionModifier);
        }

        return new HitPoints(maximum, maximum);
    }

    public static HitPoints ApplyDamage(HitPoints hitPoints, int amount)
    {
        ArgumentNullException.ThrowIfNull(hitPoints);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        return hitPoints with { Current = Math.Max(0, hitPoints.Current - amount) };
    }

    public static HitPoints Heal(HitPoints hitPoints, int amount)
    {
        ArgumentNullException.ThrowIfNull(hitPoints);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        return hitPoints with { Current = Math.Min(hitPoints.Maximum, hitPoints.Current + amount), IsDying = false, DeathTimer = 0 };
    }

    /// <summary>A full rest restores all hit points and ends any dying state.</summary>
    public static HitPoints Rest(HitPoints hitPoints)
    {
        ArgumentNullException.ThrowIfNull(hitPoints);
        return hitPoints with { Current = hitPoints.Maximum, IsDying = false, DeathTimer = 0 };
    }

    /// <summary>At 0 HP a combatant is unconscious and dying; the death timer is 1d4 + CON (minimum 1 round).</summary>
    public static HitPoints EnterDying(HitPoints hitPoints, int constitutionModifier, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(hitPoints);
        ArgumentNullException.ThrowIfNull(random);
        if (hitPoints.Current > 0) throw new InvalidOperationException("A combatant above 0 HP is not dying.");
        if (hitPoints.IsDying) return hitPoints;
        return hitPoints with { IsDying = true, DeathTimer = Math.Max(1, random.Next(1, 5) + constitutionModifier) };
    }

    /// <summary>Each dying turn: a natural 20 wakes the character at 1 HP, otherwise the timer counts down to death.</summary>
    public static (HitPoints HitPoints, DyingOutcome Outcome) TickDying(HitPoints hitPoints, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(hitPoints);
        ArgumentNullException.ThrowIfNull(random);
        if (!hitPoints.IsDying) throw new InvalidOperationException("The combatant is not dying.");

        if (random.Next(1, 21) == 20)
        {
            return (hitPoints with { Current = 1, IsDying = false, DeathTimer = 0 }, DyingOutcome.Revived);
        }

        var timer = hitPoints.DeathTimer - 1;
        return timer <= 0
            ? (hitPoints with { IsDying = false, DeathTimer = 0 }, DyingOutcome.Dead)
            : (hitPoints with { DeathTimer = timer }, DyingOutcome.Dying);
    }

    /// <summary>A close ally may attempt a DC 15 Intelligence check to stop the dying process (the target stays at 0 HP).</summary>
    public static (HitPoints HitPoints, bool Stabilized) Stabilize(HitPoints hitPoints, int healerIntelligenceModifier, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(hitPoints);
        ArgumentNullException.ThrowIfNull(random);
        if (!hitPoints.IsDying) throw new InvalidOperationException("The combatant is not dying.");

        var check = CheckResolver.Resolve(healerIntelligenceModifier, CheckDifficulty.Hard, random);
        return check.Success ? (hitPoints with { IsDying = false, DeathTimer = 0 }, true) : (hitPoints, false);
    }
}
