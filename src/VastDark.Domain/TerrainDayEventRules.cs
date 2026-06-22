namespace VastDark.Domain;

public sealed record TerrainDayEventResult(Terrain Terrain, WastesWeatherResolution? Weather, WastesEncounterRule? Encounter);

/// <summary>Daily terrain checkpoint. Page 12 explicitly requires Wastes weather and encounters for every day spent there.</summary>
public static class TerrainDayEventService
{
    public static TerrainDayEventResult ResolveDay(Terrain terrain, TravelParty party, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(random);
        if (terrain != Terrain.Wastes) return new TerrainDayEventResult(terrain, null, null);
        var weather = WastesWeatherService.Resolve(random.Next(1, 7) + random.Next(1, 7), party, new WastesWeatherContext(), random);
        var encounter = WastesEncounterRules.GetEncounter(random.Next(1, 13) + weather.EncounterRollModifier);
        return new TerrainDayEventResult(terrain, weather, encounter);
    }
}
