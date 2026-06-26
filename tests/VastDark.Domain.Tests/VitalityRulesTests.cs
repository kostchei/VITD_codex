using VastDark.Domain;
using static TestKit;

internal static class VitalityRulesTests
{
    public static void Run()
    {
        GritIsShadowdarkHitPointsPerLevel();
        GritIsAtLeastOnePerLevel();
        DamageSpendsGritBeforeFlesh();
    }

    private static void GritIsShadowdarkHitPointsPerLevel()
    {
        // Shadowdark HP: a d8 plus CON each level. Level 3, CON +2, rolls 5/5/5 -> (5+2) * 3 = 21
        // (the old Vast formula added CON only once, giving 17).
        Assert(VitalityRules.StartingGrit(3, 2, [5, 5, 5]) == 21, "Grit must add the Constitution modifier per level, not once.");
    }

    private static void GritIsAtLeastOnePerLevel()
    {
        // A heavy CON penalty still yields a minimum of 1 Grit per level.
        Assert(VitalityRules.StartingGrit(2, -4, [1, 2]) == 2, "Each level must contribute at least 1 Grit regardless of CON penalty.");
    }

    private static void DamageSpendsGritBeforeFlesh()
    {
        // Grit absorbs first; overflow wounds Flesh and forces a random injury.
        var resolution = VitalityRules.ApplyDamage(new Vitality(5, 4), 8);
        Assert(resolution.Vitality.Grit == 0 && resolution.Vitality.Flesh == 1, "Damage must empty Grit before reducing Flesh.");
        Assert(resolution.InjuryRequired, "Flesh damage must require a Traveler injury.");
    }
}
