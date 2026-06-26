using VastDark.Domain;
using static TestKit;

internal static class CombatFlowTests
{
    public static void Run()
    {
        AdvantageAndDisadvantageCancel();
        ContestedChecksRerollTies();
        InitiativeOrdersHighestFirst();
        SurpriseGrantsAdvantage();
        FinesseWeaponsUseTheBetterAbility();
    }

    private static void AdvantageAndDisadvantageCancel()
    {
        Assert(RollModeRules.Combine(advantage: true, disadvantage: true) == RollMode.Normal, "Advantage and disadvantage cancel to a normal roll.");
        Assert(RollModeRules.Combine(advantage: true, disadvantage: false) == RollMode.Advantage, "Advantage alone grants advantage.");
        Assert(RollModeRules.Combine(advantage: false, disadvantage: true) == RollMode.Disadvantage, "Disadvantage alone imposes disadvantage.");
        Assert(RollModeRules.Combine(advantage: false, disadvantage: false) == RollMode.Normal, "No modifiers means a normal roll.");
    }

    private static void ContestedChecksRerollTies()
    {
        // A tie (5 vs 5) is rerolled; the next pair (8 vs 3) decides for the challenger.
        Assert(CheckResolver.Contested(0, 0, new ScriptedRandom(5, 5, 8, 3)), "A contested check rerolls ties and returns the higher roller.");
        Assert(!CheckResolver.Contested(0, 2, new ScriptedRandom(10, 9)), "10 versus 9+2 (=11) loses the contest.");
    }

    private static void InitiativeOrdersHighestFirst()
    {
        // Scout rolls 10 (+2 = 12), Ogre rolls 15 (+0 = 15): the Ogre acts first.
        var order = InitiativeRules.Order([("Scout", 2), ("Ogre", 0)], new ScriptedRandom(10, 15));
        Assert(order[0].Name == "Ogre" && order[1].Name == "Scout", "Initiative orders by total, highest first.");
    }

    private static void SurpriseGrantsAdvantage()
    {
        Assert(SurpriseRules.Surprises(3, 0, new ScriptedRandom(10, 2)), "A stealthier side (13 vs 2) surprises its target.");
        Assert(SurpriseRules.AttackMode(targetSurprised: true) == RollMode.Advantage, "Attacks on a surprised target have advantage.");
        Assert(SurpriseRules.AttackMode(targetSurprised: false) == RollMode.Normal, "Attacks on an alert target are normal.");
    }

    private static void FinesseWeaponsUseTheBetterAbility()
    {
        var traveler = new Traveler("Vael", abilityScores: new AbilityScores(8, 14, 10, 10, 10, 10)); // STR -1, DEX +2
        Assert(traveler.AttackModifierFor(CutthroatRules.ViciousDagger) == 2, "A finesse weapon uses the better of STR/DEX (+2 DEX).");
        Assert(traveler.AttackModifierFor(CutthroatRules.MakeshiftSpear) == -1, "A non-finesse melee weapon uses STR (-1).");
        Assert(traveler.AttackModifierFor(new Weapon("Bow", "1d6", Ranged: true)) == 2, "A ranged weapon uses DEX (+2).");
    }
}
