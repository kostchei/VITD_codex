namespace VastDark.Domain;

public enum Ability { Strength, Dexterity, Constitution, Wisdom, Intelligence, Charisma }

public sealed record Vitality(int Grit, int Flesh, Ability? InjuredAbility = null)
{
    public bool RequiresSettlementHealing => Flesh < 1;
}

public sealed record DamageResolution(Vitality Vitality, int GritDamage, int FleshDamage, bool InjuryRequired);

public static class VitalityRules
{
    public static int StartingGrit(int level, int constitutionBonus, IEnumerable<int> d8Rolls)
    {
        var rolls = d8Rolls?.ToArray() ?? throw new ArgumentNullException(nameof(d8Rolls));
        if (level < 1 || rolls.Length != level || rolls.Any(roll => roll is < 1 or > 8)) throw new ArgumentOutOfRangeException(nameof(d8Rolls));
        return rolls.Sum() + constitutionBonus;
    }

    public static int StartingFlesh(int level, int highestAbilityBonus) => level < 1 ? throw new ArgumentOutOfRangeException(nameof(level)) : level + highestAbilityBonus;

    public static DamageResolution ApplyDamage(Vitality vitality, int damage)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        var gritDamage = Math.Min(vitality.Grit, damage);
        var fleshDamage = damage - gritDamage;
        var flesh = Math.Max(0, vitality.Flesh - fleshDamage);
        return new DamageResolution(new Vitality(vitality.Grit - gritDamage, flesh, vitality.InjuredAbility), gritDamage, fleshDamage, fleshDamage > 0);
    }

    public static Vitality RecoverGrit(Vitality vitality, int recovery) => recovery < 0 ? throw new ArgumentOutOfRangeException(nameof(recovery)) : vitality with { Grit = vitality.Grit + recovery };

    public static Vitality RecoverFleshAtSettlement(Vitality vitality) => vitality with { Flesh = vitality.Flesh + 1 };
}
