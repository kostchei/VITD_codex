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
            traveler.AddExhaustion(1);
        }
    }

    internal PartyRestResult Rest(TravelParty party)
    {
        var fedTravelers = 0;
        foreach (var traveler in party.Members)
        {
            if (traveler.ConsumeRation())
            {
                fedTravelers++;
            }
            else
            {
                traveler.AddExhaustion(1);
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

public sealed record PartyMoveResult(bool Moved, string Message, int DailyMiles, int DailyMileLimit, bool RestRequired);

public sealed record PartyRestResult(int FedTravelers, int UnfedTravelers);
