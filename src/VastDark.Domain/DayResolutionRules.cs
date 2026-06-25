namespace VastDark.Domain;

public sealed record DayResolution(
    Terrain Terrain,
    NavigationResult Navigation,
    WastesWeatherResolution? Weather,
    EncounterResolution? Encounter,
    IReadOnlyList<AppliedDamage> AppliedDamage,
    IReadOnlyList<PendingDecision> PendingDecisions,
    IReadOnlyList<string> Log)
{
    public string Summary => string.Join(Environment.NewLine, Log.Concat(PendingDecisions.Select(decision => decision.Prompt)));
}

/// <summary>
/// Resolves a single travel day as one operation: the navigation roll, then (in the Wastes) the
/// page-12 weather checkpoint and its weather-modified encounter. Weather damage is applied to the
/// party here rather than only reported; saves and buried Travelers stay explicit pending decisions.
/// Rations, forced march, and rest remain the caller's travel-state concern.
/// </summary>
public static class DayResolutionService
{
    public static DayResolution ResolveDay(
        Terrain terrain,
        TravelParty party,
        IEnumerable<NavigationAsset> navigationAssets,
        WastesWeatherContext weatherContext,
        IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(navigationAssets);
        ArgumentNullException.ThrowIfNull(weatherContext);
        ArgumentNullException.ThrowIfNull(random);

        var log = new List<string>();
        var appliedDamage = new List<AppliedDamage>();
        var decisions = new List<PendingDecision>();
        var travelersByName = party.Members.ToDictionary(traveler => traveler.Name, StringComparer.OrdinalIgnoreCase);

        var navigation = DailyNavigationService.Resolve(navigationAssets, random);
        log.Add(navigation.IsLost
            ? $"Navigation: lost — {navigation.Effect} (rolled {navigation.Roll} against a {navigation.LostChanceInSix}-in-6 chance)."
            : $"Navigation: on course (rolled {navigation.Roll} against a {navigation.LostChanceInSix}-in-6 chance).");

        if (terrain != Terrain.Wastes)
        {
            return new DayResolution(terrain, navigation, null, null, appliedDamage, decisions, log);
        }

        var weather = WastesWeatherService.Resolve(random.Next(1, 7) + random.Next(1, 7), party, weatherContext, random);
        log.Add($"Weather: {weather.Rule.Name}.");
        if (weather.TravelMilesLost > 0) log.Add($"{weather.TravelMilesLost} travel mile(s) are lost to the weather.");
        if (weather.LandmarksObscured) log.Add("Landmarks are obscured.");
        if (weather.LightsExtinguished) log.Add("Unprotected lights are extinguished.");

        foreach (var hit in weather.Damage)
        {
            if (!travelersByName.TryGetValue(hit.TravelerName, out var traveler))
            {
                throw new InvalidOperationException($"Weather damage references unknown Traveler '{hit.TravelerName}'.");
            }

            var vitality = traveler.TakeDamage(hit.Amount, random);
            appliedDamage.Add(new AppliedDamage(hit.TravelerName, hit.Amount, vitality));
            log.Add($"{hit.TravelerName} takes {hit.Amount} weather damage.");
        }

        foreach (var traveler in weather.ExhaustedTravelers)
        {
            log.Add($"{traveler} gains 1 exhaustion fleeing the weather.");
        }

        foreach (var traveler in weather.BreathSaveTravelers)
        {
            decisions.Add(new SavingThrowDecision(traveler, weather.Rule.SaveType ?? "Breath", "suffer the storm's full effect"));
        }

        foreach (var traveler in weather.BuriedTravelers)
        {
            decisions.Add(new RefereeChoiceDecision($"{traveler} is buried by the Dune Wave — resolve death or rescue", ["Buried", "Rescued"]));
        }

        var encounter = EncounterResolver.ResolveWastes(random.Next(1, 13) + weather.EncounterRollModifier, party, random);
        log.AddRange(encounter.Log);
        appliedDamage.AddRange(encounter.AppliedDamage);
        decisions.AddRange(encounter.PendingDecisions);

        return new DayResolution(terrain, navigation, weather, encounter, appliedDamage, decisions, log);
    }
}
