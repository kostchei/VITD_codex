namespace VastDark.Domain;

public enum WastesWeatherEffect { Calm, DustStorm, WindBlast, StoneHail, PillarFog, GritSlide, DuneWave }

public sealed record WastesWeatherRule(int Roll, WastesWeatherEffect Effect, string Name, int TravelMilesLost = 0, int EncounterRollModifier = 0, bool ObscuresLandmarks = false, string? SaveType = null, string? DamageDice = null);
public sealed record WastesWeatherContext(bool Protected = false, bool InOpen = true, bool RunFromDuneWave = true);
public sealed record WastesWeatherResolution(WastesWeatherRule Rule, int TravelMilesLost, int EncounterRollModifier, bool LandmarksObscured, bool LightsExtinguished, IReadOnlyList<HazardDamage> Damage, IReadOnlyList<string> BreathSaveTravelers, IReadOnlyList<string> ExhaustedTravelers, IReadOnlyList<string> BuriedTravelers);

public static class WastesWeatherRules
{
    public static WastesWeatherRule Get(int roll) => roll switch
    {
        >= 2 and <= 6 => new(roll, WastesWeatherEffect.Calm, "Calm"),
        7 => new(7, WastesWeatherEffect.DustStorm, "Dust Storm", TravelMilesLost: 6, ObscuresLandmarks: true),
        8 => new(8, WastesWeatherEffect.WindBlast, "Wind Blast", DamageDice: "3d6"),
        9 => new(9, WastesWeatherEffect.StoneHail, "Stone Hail", SaveType: "Breath", DamageDice: "3d6"),
        10 => new(10, WastesWeatherEffect.PillarFog, "Pillar Fog", EncounterRollModifier: 6, ObscuresLandmarks: true),
        11 => new(11, WastesWeatherEffect.GritSlide, "Grit Slide", TravelMilesLost: 6, SaveType: "Breath", DamageDice: "3d6"),
        12 => new(12, WastesWeatherEffect.DuneWave, "Dune Wave"),
        _ => throw new ArgumentOutOfRangeException(nameof(roll), "Wastes weather uses 2d6 totals from 2 to 12."),
    };
}

public static class WastesWeatherService
{
    public static WastesWeatherResolution Resolve(int roll, TravelParty party, WastesWeatherContext context, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(random);
        var rule = WastesWeatherRules.Get(roll);
        var damage = new List<HazardDamage>();
        var saves = new List<string>();
        var exhausted = new List<string>();
        var buried = new List<string>();
        var lightsExtinguished = rule.Effect == WastesWeatherEffect.WindBlast;

        switch (rule.Effect)
        {
            case WastesWeatherEffect.WindBlast when context.InOpen:
                damage.AddRange(party.Members.Select(traveler => new HazardDamage(traveler.Name, Roll(random, 3, 6))));
                break;
            case WastesWeatherEffect.StoneHail when !context.Protected:
            case WastesWeatherEffect.GritSlide:
                saves.AddRange(party.Members.Select(traveler => traveler.Name));
                break;
            case WastesWeatherEffect.DuneWave:
                foreach (var traveler in party.Members)
                {
                    if (context.RunFromDuneWave)
                    {
                        traveler.AddExhaustion(1);
                        exhausted.Add(traveler.Name);
                    }
                    else buried.Add(traveler.Name);
                }
                break;
        }

        return new WastesWeatherResolution(rule, rule.TravelMilesLost, rule.EncounterRollModifier, rule.ObscuresLandmarks, lightsExtinguished, damage, saves, exhausted, buried);
    }

    private static int Roll(IRandomSource random, int dice, int sides) => Enumerable.Range(0, dice).Sum(_ => random.Next(1, sides + 1));
}
