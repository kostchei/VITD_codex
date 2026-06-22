namespace VastDark.Domain;

public enum HarrowingTrigger { DroppingToZeroHitPoints, SeventhExhaustionLevel, ObjectOrPlaceEffect, GreatTragedy }

public sealed record HarrowingResult(HarrowingTrigger Trigger, bool MemoryLost, string? LostMemory, bool FinalMemoryLost);

/// <summary>Tracks page 8's five defining memories. The source gives no loss probability, so the caller supplies the loss result.</summary>
public sealed class Harrowing
{
    private readonly List<string> _remainingMemories;

    public Harrowing(IEnumerable<string> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);
        _remainingMemories = memories.ToList();
        if (_remainingMemories.Count != 5 || _remainingMemories.Any(string.IsNullOrWhiteSpace) || _remainingMemories.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 5)
        {
            throw new ArgumentException("A Traveler must begin the Harrowing with five distinct memories or drives.", nameof(memories));
        }
    }

    public IReadOnlyList<string> RemainingMemories => _remainingMemories;
    public int LostMemoryCount => 5 - _remainingMemories.Count;

    public HarrowingResult Resolve(HarrowingTrigger trigger, bool losesMemory, string? memoryToLose = null)
    {
        if (!losesMemory) return new HarrowingResult(trigger, false, null, false);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryToLose);
        var index = _remainingMemories.FindIndex(memory => string.Equals(memory, memoryToLose, StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new InvalidOperationException("Only a remaining memory can be lost.");
        var lostMemory = _remainingMemories[index];
        _remainingMemories.RemoveAt(index);
        return new HarrowingResult(trigger, true, lostMemory, _remainingMemories.Count == 0);
    }
}
