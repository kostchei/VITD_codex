namespace VastDark.Domain;

public readonly record struct LocalMapCoord(RegionalCoord RegionalCoordinate, HexCoord LocalCoordinate);

public sealed class PartyTravelState
{
    public const int NormalDailyMiles = 18;
    public const int ForcedMarchMiles = 6;

    public PartyTravelState(RegionalCoord regionalCoordinate, HexCoord localCoordinate, int day = 1, int dailyMiles = 0, bool forcedMarchUsed = false)
    {
        if (day < 1 || dailyMiles is < 0 or > NormalDailyMiles + ForcedMarchMiles)
        {
            throw new ArgumentOutOfRangeException(nameof(day), "Party travel state contains an invalid value.");
        }

        if (dailyMiles > NormalDailyMiles && !forcedMarchUsed)
        {
            throw new ArgumentException("Miles beyond the normal daily limit require a forced march.", nameof(dailyMiles));
        }

        RegionalCoordinate = regionalCoordinate;
        LocalCoordinate = localCoordinate;
        Day = day;
        DailyMiles = dailyMiles;
        ForcedMarchUsed = forcedMarchUsed;
    }

    public RegionalCoord RegionalCoordinate { get; private set; }
    public HexCoord LocalCoordinate { get; private set; }
    public int Day { get; private set; }
    public int DailyMiles { get; private set; }
    public bool ForcedMarchUsed { get; private set; }
    public int DailyMileLimit => ForcedMarchUsed ? NormalDailyMiles + ForcedMarchMiles : NormalDailyMiles;
    public bool RestRequired => DailyMiles >= DailyMileLimit;
    public bool CanForcedMarch => DailyMiles == NormalDailyMiles && !ForcedMarchUsed;
    public bool CanRest => true;

    internal void MoveTo(RegionalCoord regionalCoordinate, HexCoord localCoordinate)
    {
        if (RestRequired)
        {
            throw new InvalidOperationException("The party must rest before moving again.");
        }

        RegionalCoordinate = regionalCoordinate;
        LocalCoordinate = localCoordinate;
        DailyMiles++;
    }

    internal void BeginForcedMarch(TravelParty party)
    {
        if (!CanForcedMarch)
        {
            throw new InvalidOperationException("Forced march is available only after 18 normal miles and before resting.");
        }

        ForcedMarchUsed = true;
        foreach (var traveler in party.Members)
        {
            traveler.AddExhaustion(1, ExhaustionSource.ForcedMarch);
        }
    }

    internal PartyRestResult Rest(TravelParty party)
    {
        var fedTravelers = 0;
        foreach (var traveler in party.Members)
        {
            traveler.RecoverExhaustionFromFullRest();
            if (traveler.ConsumeRation())
            {
                fedTravelers++;
            }
            else
            {
                traveler.AddExhaustion(1, ExhaustionSource.Hunger);
            }
        }

        Day++;
        DailyMiles = 0;
        ForcedMarchUsed = false;
        return new PartyRestResult(fedTravelers, party.Members.Count - fedTravelers);
    }

    internal PartyTravelStateState ToState() => new(
        RegionalCoordinate.Column,
        RegionalCoordinate.Row,
        LocalCoordinate.Q,
        LocalCoordinate.R,
        Day,
        DailyMiles,
        Exhaustion: 0,
        ForcedMarchUsed: ForcedMarchUsed,
        Rations: 0);
}

public enum TravelInterruptionKind { Ruins, Settlement, RoamingHazard }

public sealed record TravelInterruption(TravelInterruptionKind Kind, Terrain Terrain, int? HazardDieRoll = null)
{
    public string Title => Kind switch
    {
        TravelInterruptionKind.Ruins => "Ruins encountered",
        TravelInterruptionKind.Settlement => "Settlement reached",
        TravelInterruptionKind.RoamingHazard => $"Roaming hazard: {LocalMap.GetRoamingHazardName(HazardDieRoll!.Value)}",
        _ => "Travel interrupted",
    };
}

public sealed record TravelResolutionOptions(
    bool HasStrongShelter = false,
    bool RunFromCollapse = true,
    bool HasExposedMetal = false,
    bool OnSolidOrRockyGround = false);

public sealed record AppliedDamage(string TravelerName, int Amount, DamageResolution? VitalityResolution);

public sealed record TravelInterruptionResolution(
    TravelInterruption Interruption,
    string Title,
    IReadOnlyList<string> Log,
    RoamingHazardResolution? Hazard = null,
    IReadOnlyList<AppliedDamage>? AppliedDamage = null)
{
    public string Summary => string.Join(Environment.NewLine, Log);
}

public static class TravelInterruptionResolver
{
    public static TravelInterruptionResolution Resolve(
        TravelInterruption interruption,
        TravelParty party,
        IRandomSource random,
        TravelResolutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(interruption);
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(random);
        options ??= new TravelResolutionOptions();

        return interruption.Kind switch
        {
            TravelInterruptionKind.Ruins => new TravelInterruptionResolution(
                interruption,
                interruption.Title,
                ["Travel stops at the Ruins. Enter the generated Ruin graph to explore rooms, encounters, treasure, and depth."],
                AppliedDamage: []),
            TravelInterruptionKind.Settlement => new TravelInterruptionResolution(
                interruption,
                interruption.Title,
                ["Travel stops at the Settlement. Resolve rest, resupply, services, factions, and trade before continuing."],
                AppliedDamage: []),
            TravelInterruptionKind.RoamingHazard => ResolveRoamingHazard(interruption, party, random, options),
            _ => throw new InvalidOperationException("Unknown travel interruption kind."),
        };
    }

    private static TravelInterruptionResolution ResolveRoamingHazard(
        TravelInterruption interruption,
        TravelParty party,
        IRandomSource random,
        TravelResolutionOptions options)
    {
        if (interruption.HazardDieRoll is not { } dieRoll)
        {
            throw new InvalidOperationException("A roaming-hazard interruption requires its d6 face.");
        }

        var context = new RoamingHazardContext(
            interruption.Terrain,
            options.HasStrongShelter,
            options.RunFromCollapse,
            options.HasExposedMetal,
            options.OnSolidOrRockyGround);

        // The shared encounter resolver owns hazard mechanics; the travel layer just re-titles the
        // outcome and flattens its pending decisions into the legacy interruption summary.
        var resolution = EncounterResolver.ResolveRoamingHazard(dieRoll, party, context, random);
        var log = resolution.Log.Concat(resolution.PendingDecisions.Select(decision => decision.Prompt)).ToList();
        return new TravelInterruptionResolution(interruption, interruption.Title, log, resolution.RoamingHazard, resolution.AppliedDamage);
    }
}

public sealed record PartyMoveResult(bool Moved, string Message, int DailyMiles, int DailyMileLimit, bool RestRequired, TravelInterruption? Interruption = null);

public sealed record PartyRestResult(int FedTravelers, int UnfedTravelers);
