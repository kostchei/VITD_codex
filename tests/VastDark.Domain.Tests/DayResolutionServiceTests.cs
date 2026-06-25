using VastDark.Domain;
using static TestKit;

internal static class DayResolutionServiceTests
{
    public static void Run()
    {
        CalmWastesDayLeavesNothingToResolve();
        WindBlastWeatherIsAppliedToTheParty();
        StoneHailLeavesAnExplicitBreathSave();
        NonWastesDayResolvesNavigationOnly();
    }

    private static TravelParty SoloParty() => new([new Traveler("Vael")]);

    private static readonly NavigationAsset[] AllAssets =
    [
        NavigationAsset.Landmark,
        NavigationAsset.Directions,
        NavigationAsset.Tool,
        NavigationAsset.Light,
        NavigationAsset.DeadReckoning,
    ];

    private static void CalmWastesDayLeavesNothingToResolve()
    {
        // Navigation d6 (never lost with five assets), weather 2d6 = 6 (Calm), encounter d12 = 1 (Nothing).
        var day = DayResolutionService.ResolveDay(
            Terrain.Wastes,
            SoloParty(),
            AllAssets,
            new WastesWeatherContext(),
            new ScriptedRandom(1, 3, 3, 1));
        Assert(!day.Navigation.IsLost, "Five navigation assets must keep the party on course.");
        Assert(day.Weather is { Rule.Effect: WastesWeatherEffect.Calm }, "Weather total 6 must be Calm.");
        Assert(day.Encounter is { Title: "Wastes encounter: Nothing" }, "Encounter roll 1 must resolve to Nothing.");
        Assert(day.AppliedDamage.Count == 0 && day.PendingDecisions.Count == 0, "A calm, empty day must leave nothing to resolve.");
    }

    private static void WindBlastWeatherIsAppliedToTheParty()
    {
        // Navigation d6 = 6 (no assets, not lost), weather 2d6 = 8 (Wind Blast), 3d6 = 6 damage, encounter d12 = 6.
        var day = DayResolutionService.ResolveDay(
            Terrain.Wastes,
            SoloParty(),
            [],
            new WastesWeatherContext(),
            new ScriptedRandom(6, 4, 4, 2, 2, 2, 6, 3));
        Assert(day.Weather is { Rule.Effect: WastesWeatherEffect.WindBlast }, "Weather total 8 must be Wind Blast.");
        Assert(day.AppliedDamage.Single() is { TravelerName: "Vael", Amount: 6 }, "Wind Blast must apply its rolled 3d6 damage to the party.");
        Assert(day.PendingDecisions.Count == 0, "Lost Travelers carry no pending decision.");
    }

    private static void StoneHailLeavesAnExplicitBreathSave()
    {
        // Navigation d6 = 6, weather 2d6 = 9 (Stone Hail) demands a Breath save with no auto-damage, encounter d12 = 1.
        var day = DayResolutionService.ResolveDay(
            Terrain.Wastes,
            SoloParty(),
            [],
            new WastesWeatherContext(),
            new ScriptedRandom(6, 4, 5, 1));
        Assert(day.Weather is { Rule.Effect: WastesWeatherEffect.StoneHail }, "Weather total 9 must be Stone Hail.");
        Assert(day.PendingDecisions.Single() is SavingThrowDecision { SaveType: "Breath" }, "Stone Hail must record a pending Breath save.");
        Assert(day.AppliedDamage.Count == 0, "Stone Hail damage is gated behind the save and must not be auto-applied.");
    }

    private static void NonWastesDayResolvesNavigationOnly()
    {
        // Outside the Wastes there is no weather checkpoint; only the navigation roll happens.
        var day = DayResolutionService.ResolveDay(
            Terrain.Pillars,
            SoloParty(),
            [],
            new WastesWeatherContext(),
            new ScriptedRandom(1));
        Assert(day.Weather is null && day.Encounter is null, "Non-Wastes terrain must skip the weather and encounter checkpoint.");
        Assert(day.Navigation is { IsLost: true, Effect: LostEffect.UtterlyLost }, "A bare navigation failure with no assets must be Utterly Lost.");
        Assert(day.AppliedDamage.Count == 0 && day.PendingDecisions.Count == 0, "A navigation-only day applies no damage and leaves no pending decisions.");
    }
}
