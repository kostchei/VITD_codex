namespace VastDark.Domain;

public enum PackType { Bindle, Sack, Backpack }
public enum CargoTransportType { Pulk, Sleigh }

public sealed record PackRule(PackType Type, int AdditionalSlots, int CoinCost);
public sealed record CargoTransportRule(CargoTransportType Type, int Slots, int RestrictedDailyMiles, int MaximumPullersForRestriction);

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
