namespace VastDark.Domain;

public enum PillarWork { Gathering, Mining }
public sealed record PillarWorkResult(PillarWork Work, int RawLodestoneRolled, int RawLodestoneCollected, int EncounterRollModifier);

/// <summary>Page 14 hourly Pillar gathering, mining, and settlement refinement.</summary>
public static class PillarMiningService
{
    public static PillarWorkResult WorkHour(PillarWork work, bool hasMiningTools, TravelerInventory inventory, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(random);
        if (work == PillarWork.Mining && !hasMiningTools) throw new InvalidOperationException("Mining a Pillar requires proper tools.");
        var rawRolled = work == PillarWork.Gathering ? random.Next(1, 3) : random.Next(1, 7);
        var encounterRollModifier = work == PillarWork.Gathering ? random.Next(1, 7) : random.Next(1, 7) + random.Next(1, 7);
        var collected = Math.Min(rawRolled, inventory.AvailableSlots);
        for (var raw = 0; raw < collected; raw++) inventory.RecordOwnItem(new InventoryItem("Raw Lodestone", 1));
        return new PillarWorkResult(work, rawRolled, collected, encounterRollModifier);
    }

    public static int RefineAtSettlement(int rawLodestone, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (rawLodestone < 0) throw new ArgumentOutOfRangeException(nameof(rawLodestone));
        return Enumerable.Range(0, rawLodestone).Sum(_ => random.Next(1, 11) * 10);
    }

    public static int RefineAtSettlement(TravelerInventory inventory, int rawLodestone, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        inventory.RemoveRecordedItems("Raw Lodestone", rawLodestone);
        return RefineAtSettlement(rawLodestone, random);
    }
}
