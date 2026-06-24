namespace VastDark.Domain;

public enum PackType { Bindle, Sack, Backpack }
public enum CargoTransportType { Pulk, Sleigh }

public sealed record PackRule(PackType Type, int AdditionalSlots, int CoinCost);
public sealed record CargoTransportRule(CargoTransportType Type, int Slots, int RestrictedDailyMiles, int MaximumPullersForRestriction);
public sealed record InventoryItem
{
    public InventoryItem(string name, int slots, bool isUniqueOrMagical = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (slots < 1) throw new ArgumentOutOfRangeException(nameof(slots));
        Name = name;
        Slots = slots;
        IsUniqueOrMagical = isUniqueOrMagical;
    }

    public string Name { get; }
    public int Slots { get; }
    public bool IsUniqueOrMagical { get; }
}

public sealed record LoadoutAllocation
{
    public LoadoutAllocation(string purpose, int slots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        if (slots < 1) throw new ArgumentOutOfRangeException(nameof(slots));
        Purpose = purpose;
        Slots = slots;
    }

    public string Purpose { get; }
    public int Slots { get; }
}

/// <summary>Slot accounting for a Traveler's Constitution, packs, planned loadouts, and recorded items.</summary>
public sealed class TravelerInventory
{
    private readonly List<InventoryItem> _items = [];
    private readonly Dictionary<string, int> _loadoutSlots = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PackType> _packs = [];

    public TravelerInventory(int constitutionModifier)
    {
        ConstitutionModifier = constitutionModifier;
    }

    public TravelerInventory(int constitutionModifier, TravelerRulesState? state) : this(constitutionModifier)
    {
        foreach (var pack in state?.Packs ?? []) _packs.Add(pack);
        foreach (var item in state?.Items ?? []) _items.Add(new InventoryItem(item.Name, item.Slots, item.IsUniqueOrMagical));
        foreach (var loadout in state?.Loadouts ?? []) _loadoutSlots[loadout.Purpose] = loadout.Slots;
    }

    public int ConstitutionModifier { get; }
    public int BaseSlots => Math.Max(0, ConstitutionModifier);
    public int PackSlots => _packs.Sum(pack => InventoryRules.GetPack(pack).AdditionalSlots);
    public int Capacity => BaseSlots + PackSlots;
    public int UsedSlots => _items.Sum(item => item.Slots) + _loadoutSlots.Values.Sum();
    public int AvailableSlots => Capacity - UsedSlots;
    public IReadOnlyList<InventoryItem> Items => _items;
    public IReadOnlyList<LoadoutAllocation> Loadouts => _loadoutSlots.OrderBy(pair => pair.Key).Select(pair => new LoadoutAllocation(pair.Key, pair.Value)).ToList();
    public IReadOnlyList<PackType> Packs => _packs;

    public (List<InventoryItemState> Items, List<LoadoutState> Loadouts, List<PackType> Packs) ToState() =>
        (_items.Select(item => new InventoryItemState(item.Name, item.Slots, item.IsUniqueOrMagical)).ToList(), Loadouts.Select(loadout => new LoadoutState(loadout.Purpose, loadout.Slots)).ToList(), _packs.ToList());

    public int BuyPackAtSettlement(PackType type, int availableCoins, bool atSettlement)
    {
        RequireSettlement(atSettlement);
        var rule = InventoryRules.GetPack(type);
        if (availableCoins < rule.CoinCost) throw new InvalidOperationException("Insufficient coins to purchase this pack.");
        _packs.Add(type);
        return rule.CoinCost;
    }

    public int AssignLoadoutAtSettlement(string purpose, int slots, int availableCoins, bool atSettlement)
    {
        RequireSettlement(atSettlement);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        if (slots < 1) throw new ArgumentOutOfRangeException(nameof(slots));
        var cost = slots * 10;
        if (availableCoins < cost) throw new InvalidOperationException("Insufficient coins to fill these loadout slots.");
        if (AvailableSlots < slots) throw new InvalidOperationException("Insufficient inventory slots for this loadout.");
        _loadoutSlots[purpose] = _loadoutSlots.GetValueOrDefault(purpose) + slots;
        return cost;
    }

    public void DrawCommonItem(string purpose, InventoryItem item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentNullException.ThrowIfNull(item);
        if (item.IsUniqueOrMagical) throw new InvalidOperationException("Unique and magical items must use their own inventory slots.");
        var assigned = _loadoutSlots.GetValueOrDefault(purpose);
        if (assigned < item.Slots) throw new InvalidOperationException("This loadout does not have enough assigned slots.");
        ConsumeLoadoutSlots(purpose, item.Slots);
        _items.Add(item);
    }

    public void RecordOwnItem(InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (AvailableSlots < item.Slots) throw new InvalidOperationException("Insufficient inventory slots for this item.");
        _items.Add(item);
    }

    public void RemoveRecordedItems(string name, int count)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        var matches = _items.Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)).Take(count).ToList();
        if (matches.Count != count) throw new InvalidOperationException("The requested recorded items are not available.");
        foreach (var item in matches) _items.Remove(item);
    }

    private void ConsumeLoadoutSlots(string purpose, int slots)
    {
        var remaining = _loadoutSlots[purpose] - slots;
        if (remaining == 0) _loadoutSlots.Remove(purpose);
        else _loadoutSlots[purpose] = remaining;
    }

    private static void RequireSettlement(bool atSettlement)
    {
        if (!atSettlement) throw new InvalidOperationException("Loadouts and packs can only be acquired safely at a settlement.");
    }
}

public static class InventoryRules
{
    private static readonly IReadOnlyDictionary<PackType, PackRule> Packs = new Dictionary<PackType, PackRule>
    {
        [PackType.Bindle] = new(PackType.Bindle, 2, 20),
        [PackType.Sack] = new(PackType.Sack, 6, 80),
        [PackType.Backpack] = new(PackType.Backpack, 10, 120),
    };

    private static readonly IReadOnlyDictionary<CargoTransportType, CargoTransportRule> Transports = new Dictionary<CargoTransportType, CargoTransportRule>
    {
        [CargoTransportType.Pulk] = new(CargoTransportType.Pulk, 10, 12, 1),
        [CargoTransportType.Sleigh] = new(CargoTransportType.Sleigh, 20, 12, 2),
    };

    public static PackRule GetPack(PackType type) => Packs[type];
    public static CargoTransportRule GetTransport(CargoTransportType type) => Transports[type];

    public static int DailyMilesWithTransport(int normalDailyMiles, CargoTransportType type, int pullers) =>
        pullers <= 0 ? throw new ArgumentOutOfRangeException(nameof(pullers)) :
        pullers <= GetTransport(type).MaximumPullersForRestriction ? GetTransport(type).RestrictedDailyMiles : normalDailyMiles;
}
