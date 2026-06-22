namespace VastDark.Domain;

public enum RiteSchool { Labyrinth, Dark, Harrow, Sparks }
public enum RiteSpell { WildSeeking, FickleDescent, WasteWhispers, Mazewalk, BoltOfNight, PresenceBlank, ChanceSolitude, WanderersSnare, LikeClay, SunderToDust, OneFlesh, GrimTranspose, Cinderhowl, BrightHand, HeartFurnace }
public sealed record RiteSpellRule(RiteSpell Spell, RiteSchool School, string Effect, bool CostsRite = true);

public static class RiteSpellRules
{
    private static readonly IReadOnlyDictionary<RiteSpell, RiteSpellRule> Spells = new Dictionary<RiteSpell, RiteSpellRule>
    {
        [RiteSpell.WildSeeking] = new(RiteSpell.WildSeeking,RiteSchool.Labyrinth,"Meditate 10 minutes; transport caster and contacts 1d6 nearby rooms/points."), [RiteSpell.FickleDescent] = new(RiteSpell.FickleDescent,RiteSchool.Labyrinth,"Coin face ascends, tail descends a chosen number up to level."), [RiteSpell.WasteWhispers] = new(RiteSpell.WasteWhispers,RiteSchool.Labyrinth,"Perfect landmark/structure navigation; caster cannot move or defend until ended."), [RiteSpell.Mazewalk] = new(RiteSpell.Mazewalk,RiteSchool.Labyrinth,"Sleep-travel to a place visited this week; each traveler gains 1 exhaustion; caster gains 2d3."),
        [RiteSpell.BoltOfNight] = new(RiteSpell.BoltOfNight,RiteSchool.Dark,"Visible target is blind or deaf until caster recasts."), [RiteSpell.PresenceBlank] = new(RiteSpell.PresenceBlank,RiteSchool.Dark,"Imperceptible/intangible for one minute per level; cannot interact and cannot breathe."), [RiteSpell.ChanceSolitude] = new(RiteSpell.ChanceSolitude,RiteSchool.Dark,"Caster and nearby allies are ignored by next random/wandering encounter."), [RiteSpell.WanderersSnare] = new(RiteSpell.WanderersSnare,RiteSchool.Dark,"Audible target cannot navigate or track until caster recasts."),
        [RiteSpell.LikeClay] = new(RiteSpell.LikeClay,RiteSchool.Harrow,"After day/night rest, swap two ability scores until recast."), [RiteSpell.SunderToDust] = new(RiteSpell.SunderToDust,RiteSchool.Harrow,"Touched inorganic material crumbles for six seconds per level."), [RiteSpell.OneFlesh] = new(RiteSpell.OneFlesh,RiteSchool.Harrow,"Target and caster share damage and recovery until ended."), [RiteSpell.GrimTranspose] = new(RiteSpell.GrimTranspose,RiteSchool.Harrow,"Transfer any memories into dead/memoryless body; it resurrects as new Traveler."),
        [RiteSpell.Cinderhowl] = new(RiteSpell.Cinderhowl,RiteSchool.Sparks,"Cinder gout; 1-in-6 chance to alert wandering encounter.",false), [RiteSpell.BrightHand] = new(RiteSpell.BrightHand,RiteSchool.Sparks,"Hand torch/finger candle; finger costs one Grit.",false), [RiteSpell.HeartFurnace] = new(RiteSpell.HeartFurnace,RiteSchool.Sparks,"Fear/psych immunity, double speed/actions; Magic save or recast.",false),
    };
    public static RiteSpellRule Get(RiteSpell spell) => Spells[spell];
    public static int FickleDescentLevels(int casterLevel, int declaredLevels, bool coinFace) => casterLevel < 1 || declaredLevels < 1 || declaredLevels > casterLevel ? throw new ArgumentOutOfRangeException(nameof(declaredLevels)) : coinFace ? declaredLevels : -declaredLevels;
    public static int SunderDurationSeconds(int casterLevel) => casterLevel < 1 ? throw new ArgumentOutOfRangeException(nameof(casterLevel)) : casterLevel * 6;
    public static int SparkGritCost(bool fingerLight, IRandomSource random) => fingerLight ? 1 : random.Next(1, 7);
    public static bool CinderhowlAlertsEncounter(IRandomSource random) => random.Next(1, 7) == 1;
    public static AbilityScores LikeClay(AbilityScores scores, Ability first, Ability second)
    {
        ArgumentNullException.ThrowIfNull(scores);
        var values = Enum.GetValues<Ability>().ToDictionary(ability => ability, scores.Get);
        (values[first], values[second]) = (values[second], values[first]);
        return new AbilityScores(values[Ability.Strength],values[Ability.Dexterity],values[Ability.Constitution],values[Ability.Wisdom],values[Ability.Intelligence],values[Ability.Charisma]);
    }
}
