namespace VastDark.Domain;

public readonly record struct LocalMapCoord(RegionalCoord RegionalCoordinate, HexCoord LocalCoordinate);

public sealed class PartyTravelState
{
    public const int NormalDailyMiles = 18;
    public const int ForcedMarchMiles = 6;

    public PartyTravelState(RegionalCoord regionalCoordinate, HexCoord localCoordinate, int day = 1, int dailyMiles = 0, int exhaustion = 0, bool forcedMarchUsed = false, int rations = 0)
    {
        if (day < 1 || dailyMiles is < 0 or > NormalDailyMiles + ForcedMarchMiles || exhaustion < 0 || rations < 0)
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
        Exhaustion = exhaustion;
        ForcedMarchUsed = forcedMarchUsed;
        Rations = rations;
    }

    public RegionalCoord RegionalCoordinate { get; private set; }
    public HexCoord LocalCoordinate { get; private set; }
    public int Day { get; private set; }
    public int DailyMiles { get; private set; }
    public int Exhaustion { get; private set; }
    public int Rations { get; private set; }
    public bool ForcedMarchUsed { get; private set; }
    public int DailyMileLimit => ForcedMarchUsed ? NormalDailyMiles + ForcedMarchMiles : NormalDailyMiles;
    public bool RestRequired => DailyMiles >= DailyMileLimit;
    public bool CanForcedMarch => DailyMiles == NormalDailyMiles && !ForcedMarchUsed;
    public bool CanRest => true;

    public void AddRations(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Rations += amount;
    }

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

    internal void BeginForcedMarch()
    {
        if (!CanForcedMarch)
        {
            throw new InvalidOperationException("Forced march is available only after 18 normal miles and before resting.");
        }

        ForcedMarchUsed = true;
        Exhaustion++;
    }

    internal bool Rest()
    {
        var consumedRation = Rations > 0;
        if (consumedRation)
        {
            Rations--;
        }
        else
        {
            Exhaustion++;
        }

        Day++;
        DailyMiles = 0;
        ForcedMarchUsed = false;
        return consumedRation;
    }

    internal PartyTravelStateState ToState() => new(
        RegionalCoordinate.Column,
        RegionalCoordinate.Row,
        LocalCoordinate.Q,
        LocalCoordinate.R,
        Day,
        DailyMiles,
        Exhaustion,
        ForcedMarchUsed,
        Rations);
}

public sealed record PartyMoveResult(bool Moved, string Message, int DailyMiles, int DailyMileLimit, bool RestRequired);
