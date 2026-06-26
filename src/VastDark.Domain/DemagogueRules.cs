namespace VastDark.Domain;

public sealed record DemagogueRule(int HitDice, int HitPoints, string Move, string Defense, string Attack, string DamageDice);

/// <summary>
/// The Demagogue (p. 13): the boss that leads a Warband roaming hazard. Slaying the Demagogue
/// destroys the hazard. An avatar of the dark wielding a Lodestone Blade and a stolen artifact.
/// </summary>
public static class DemagogueRules
{
    public static DemagogueRule StatBlock { get; } = new(5, 30, "Standard", "Plate", "Lodestone Blade", "1d10");

    /// <summary>Voice of the Dark: anyone the Demagogue speaks to must Save versus Charm or become frightened.</summary>
    public const string VoiceOfTheDarkSave = "Charm";

    /// <summary>Magic: the Demagogue knows 1d3 random spells.</summary>
    public static int KnownSpellCount(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return random.Next(1, 4);
    }

    /// <summary>Artifact of Power: each Demagogue carries and wields a random artifact — a Great and Terrible treasure.</summary>
    public static RuinTreasureRule RollArtifact(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return RuinTreasureRules.Get(RuinTreasureMagnitude.GreatAndTerrible, random.Next(1, 11));
    }

    public static Monster CreateMonster() => new("Demagogue", StatBlock.HitPoints, MonsterArmor.ArmorClass(StatBlock.Defense));
}
