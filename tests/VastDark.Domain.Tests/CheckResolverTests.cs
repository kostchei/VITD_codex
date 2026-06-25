using VastDark.Domain;
using static TestKit;

internal static class CheckResolverTests
{
    public static void Run()
    {
        CheckSucceedsWhenTotalMeetsDc();
        NaturalTwentyAndOneOverrideTheTotal();
        AdvantageAndDisadvantagePickTheExtremeRoll();
        SavesMapToTheChosenAbilities();
        ResolvesADeferredSaveDecisionForTheNamedTraveler();
    }

    private static void CheckSucceedsWhenTotalMeetsDc()
    {
        Assert(CheckResolver.Resolve(2, CheckDifficulty.Normal, new ScriptedRandom(10)).Success, "Roll 10 + 2 must meet DC 12.");
        Assert(!CheckResolver.Resolve(2, CheckDifficulty.Normal, new ScriptedRandom(9)).Success, "Roll 9 + 2 must miss DC 12.");
    }

    private static void NaturalTwentyAndOneOverrideTheTotal()
    {
        var crit = CheckResolver.Resolve(-4, CheckDifficulty.Extreme, new ScriptedRandom(20));
        Assert(crit is { Success: true, CriticalSuccess: true }, "A natural 20 must succeed regardless of modifier or DC.");
        var fumble = CheckResolver.Resolve(4, CheckDifficulty.Easy, new ScriptedRandom(1));
        Assert(fumble is { Success: false, CriticalFailure: true }, "A natural 1 must fail regardless of modifier or DC.");
    }

    private static void AdvantageAndDisadvantagePickTheExtremeRoll()
    {
        Assert(CheckResolver.Resolve(0, CheckDifficulty.Normal, new ScriptedRandom(3, 17), RollMode.Advantage).NaturalRoll == 17, "Advantage must use the higher of two d20s.");
        Assert(CheckResolver.Resolve(0, CheckDifficulty.Normal, new ScriptedRandom(3, 17), RollMode.Disadvantage).NaturalRoll == 3, "Disadvantage must use the lower of two d20s.");
    }

    private static void SavesMapToTheChosenAbilities()
    {
        Assert(SaveRules.AbilityFor("Breath") == Ability.Constitution, "Breath saves map to Constitution.");
        Assert(SaveRules.AbilityFor("Poison") == Ability.Constitution, "Poison saves map to Constitution.");
        Assert(SaveRules.AbilityFor("Hold") == Ability.Strength, "Hold saves map to Strength.");
        Assert(SaveRules.AbilityFor("Charm") == Ability.Wisdom, "Charm saves map to Wisdom.");
        Assert(SaveRules.AbilityFor("Magic") == Ability.Wisdom, "Magic saves map to Wisdom.");
        Assert(ThrowsArgumentOutOfRange(() => SaveRules.AbilityFor("Spirit")), "Unmapped save types must be rejected, not silently defaulted.");
    }

    private static void ResolvesADeferredSaveDecisionForTheNamedTraveler()
    {
        // Constitution 14 (+2); a Breath save uses CON, so roll 10 + 2 meets DC 12.
        var traveler = new Traveler("Vael", abilityScores: new AbilityScores(10, 10, 14, 10, 10, 10));
        var party = new TravelParty([traveler]);
        var decision = new SavingThrowDecision("Vael", "Breath", "disappear into the ground");
        Assert(SaveRules.Resolve(decision, party, new ScriptedRandom(10)).Success, "A Breath save must roll against the named Traveler's Constitution.");
        Assert(
            ThrowsInvalidOperation(() => SaveRules.Resolve(new SavingThrowDecision("Ghost", "Breath", "x"), party, new ScriptedRandom(10))),
            "Resolving a save for an absent Traveler must throw rather than guess.");
    }

    private static bool ThrowsArgumentOutOfRange(Action action)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException) { return true; }
        return false;
    }
}
