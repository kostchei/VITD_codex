namespace VastDark.Domain;

/// <summary>
/// Cutthroats use the "Traveler or Cutthroat" short stat block (3 HD / 18 HP / Scale AC 13, attack as
/// weapon). Their improvised arsenal: vicious daggers (1d4 + STR/DEX, crit ×3), rocks and stone slabs
/// (non-proficient 1d6 bludgeoning), and makeshift spears (spear 1d6 −1, break on a fumble).
/// </summary>
public static class CutthroatRules
{
    public static Weapon ViciousDagger { get; } = new("Vicious Dagger", "1d4", CriticalMultiplier: 3);
    public static Weapon Rock { get; } = new("Rock", "1d6", AddsAbilityModifier: false);
    public static Weapon MakeshiftSpear { get; } = new("Makeshift Spear", "1d6", DamageBonus: -1, BreaksOnFumble: true);

    public static IReadOnlyList<Weapon> Arsenal { get; } = [ViciousDagger, Rock, MakeshiftSpear];

    public static Monster CreateMonster()
    {
        var stats = RuinEncounterRules.GetStatBlock("Traveler or Cutthroat");
        return new Monster("Cutthroat", stats.HitPoints, MonsterArmor.ArmorClass(stats.Armor));
    }
}

/// <summary>A Warband roaming hazard: 5d6 Cutthroats led by a Demagogue. Slaying the Demagogue ends it.</summary>
public sealed record Warband(Monster Demagogue, IReadOnlyList<Monster> Cutthroats);

public static class WarbandRules
{
    public static Warband Create(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var count = Enumerable.Range(0, 5).Sum(_ => random.Next(1, 7));
        var cutthroats = Enumerable.Range(0, count).Select(_ => CutthroatRules.CreateMonster()).ToList();
        return new Warband(DemagogueRules.CreateMonster(), cutthroats);
    }
}
