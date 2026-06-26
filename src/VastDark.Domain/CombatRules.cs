namespace VastDark.Domain;

public sealed record AttackResult(CheckResult Attack, bool Hit, bool Critical, int Damage);

/// <summary>
/// Shadowdark attack resolution: an attack roll (1d20 + attack modifier) against the target's AC,
/// then weapon damage on a hit (doubled dice on a natural-20 critical). Damage lands on a Monster's
/// HP or, for a Traveler, on Grit then Flesh via the existing vitality rules.
/// </summary>
public static class AttackResolver
{
    public static AttackResult Resolve(int attackModifier, int targetArmorClass, string damageExpression, IRandomSource random, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(random);
        var attack = CheckResolver.Resolve(attackModifier, targetArmorClass, random, mode);
        if (!attack.Success)
        {
            return new AttackResult(attack, Hit: false, Critical: false, Damage: 0);
        }

        var damage = DiceExpression.Parse(damageExpression).Roll(random, doubleDice: attack.CriticalSuccess);
        return new AttackResult(attack, Hit: true, Critical: attack.CriticalSuccess, Damage: damage);
    }

    public static AttackResult Strike(Monster target, int attackModifier, string damageExpression, IRandomSource random, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(target);
        var result = Resolve(attackModifier, target.ArmorClass, damageExpression, random, mode);
        if (result.Hit) target.Damage(result.Damage);
        return result;
    }

    public static AttackResult Strike(Traveler target, int attackModifier, string damageExpression, IRandomSource random, RollMode mode = RollMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(target);
        var result = Resolve(attackModifier, target.ArmorClass, damageExpression, random, mode);
        if (result.Hit) target.TakeDamage(result.Damage, random);
        return result;
    }
}

public sealed record MoraleResult(bool Holds, bool Flees, CheckResult Check);

/// <summary>
/// Shadowdark morale (p. 51): a group reduced to half its size, or a solo enemy reduced to half HP,
/// makes a DC 15 Wisdom check (one roll per group, using the leader's WIS). Failure means it flees.
/// </summary>
public static class MoraleRules
{
    public const int FleeDifficultyClass = 15;

    public static bool GroupMustCheck(int originalSize, int remaining)
    {
        if (originalSize < 1) throw new ArgumentOutOfRangeException(nameof(originalSize));
        if (remaining < 0 || remaining > originalSize) throw new ArgumentOutOfRangeException(nameof(remaining));
        return remaining * 2 <= originalSize;
    }

    public static bool SoloMustCheck(Monster monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        return monster.HitPoints.Current * 2 <= monster.HitPoints.Maximum;
    }

    public static MoraleResult Check(int leaderWisdomModifier, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var check = CheckResolver.Resolve(leaderWisdomModifier, FleeDifficultyClass, random);
        return new MoraleResult(check.Success, !check.Success, check);
    }
}
