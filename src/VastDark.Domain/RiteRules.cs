namespace VastDark.Domain;

public enum RiteGainMethod { MotionsOfTheLabyrinth, ShunningLight, EmbracingDark, ErosionOfSelf }

/// <summary>Page 38 Rite currency, gain routes, and Erosion of Self lockout.</summary>
public sealed class RiteLedger
{
    private readonly HashSet<string> _firstMotionLocations = new(StringComparer.OrdinalIgnoreCase);
    public int Rites { get; private set; }
    public int LockedErosionExhaustion { get; private set; }
    public bool GainFromMotion(string locationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);
        if (!_firstMotionLocations.Add(locationId)) return false;
        Rites++;
        return true;
    }
    public void GainFromShunningLight(bool sacrificedToolOrWeekRations)
    {
        if (!sacrificedToolOrWeekRations) throw new InvalidOperationException("Shunning Light requires a tool or a week's rations.");
        Rites++;
    }
    public void GainFromEmbracingDark(bool consumedRawLodestone)
    {
        if (!consumedRawLodestone) throw new InvalidOperationException("Embracing Dark requires Raw Lodestone.");
        Rites++;
    }
    public void GainFromErosionOfSelf()
    {
        Rites++;
        LockedErosionExhaustion++;
    }
    public bool TrySpendToCast()
    {
        if (Rites == 0) return false;
        Rites--;
        if (LockedErosionExhaustion > 0) LockedErosionExhaustion--;
        return true;
    }
}
