namespace VastDark.Domain;

/// <summary>
/// Combat hit points for monsters. Unlike a Traveler (whose Grit absorbs damage before Flesh and who
/// has a dying state), a monster is simply dead at 0 HP.
/// </summary>
public sealed record HitPoints(int Maximum, int Current)
{
    public bool IsDead => Current <= 0;

    public HitPoints Damage(int amount) =>
        amount < 0 ? throw new ArgumentOutOfRangeException(nameof(amount)) : this with { Current = Math.Max(0, Current - amount) };

    public HitPoints Heal(int amount) =>
        amount < 0 ? throw new ArgumentOutOfRangeException(nameof(amount)) : this with { Current = Math.Min(Maximum, Current + amount) };
}

/// <summary>Maps the Vast bestiary's "Defense: As &lt;armor&gt;" descriptors to flat Shadowdark AC values.</summary>
public static class MonsterArmor
{
    public static int ArmorClass(string defense)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defense);
        var normalized = defense.Trim();
        if (normalized.StartsWith("As ", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[3..].Trim();
        }

        return normalized.ToLowerInvariant() switch
        {
            "none" => 10,
            "hide" => 11,
            "leather" => 11,
            "scale" => 13,
            "chain-shirt" or "chainshirt" or "chain shirt" => 13,
            "plate" => 15,
            _ => throw new ArgumentOutOfRangeException(nameof(defense), $"No AC mapping is defined for armor '{defense}'."),
        };
    }
}

/// <summary>A live monster combatant built from a source stat block: tracked HP (0 = dead) and a flat AC.</summary>
public sealed class Monster
{
    public Monster(string name, int maxHitPoints, int armorClass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (maxHitPoints < 1) throw new ArgumentOutOfRangeException(nameof(maxHitPoints));
        Name = name;
        HitPoints = new HitPoints(maxHitPoints, maxHitPoints);
        ArmorClass = armorClass;
    }

    public string Name { get; }
    public HitPoints HitPoints { get; private set; }
    public int ArmorClass { get; }
    public bool IsDead => HitPoints.IsDead;

    public void Damage(int amount) => HitPoints = HitPoints.Damage(amount);

    public static Monster FromCrawl(CrawlCreature creature)
    {
        var rule = CrawlCreatureRules.Get(creature);
        return new Monster(creature.ToString(), rule.HitPoints, MonsterArmor.ArmorClass(rule.Defense));
    }

    public static Monster FromStatBlock(RuinCreatureStatRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new Monster(rule.Name, rule.HitPoints, MonsterArmor.ArmorClass(rule.Armor));
    }
}
