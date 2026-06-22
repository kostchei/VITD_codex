namespace VastDark.Domain;

/// <summary>Reusable source-driven once-per-day gate for effects that explicitly state a daily limit.</summary>
public sealed class DailyUseTracker
{
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);
    public bool TryUse(string effectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectId);
        return _used.Add(effectId);
    }
    public void ResetDay() => _used.Clear();
}
