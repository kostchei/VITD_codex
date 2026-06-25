namespace VastDark.Domain;

public enum SettlementPopulation { Barren, Middling, Overcrowded }
public enum SettlementScarcity { Desperate, LimitedInventory, SteepPrices, DifficultBargains, Middling, Bountiful }
public enum SettlementAtmosphere { Hidden, Piety, Mirth, Despair, Stoic, Primal }

public sealed record SettlementPopulationRule(SettlementPopulation Population, int MinimumRoll, int MaximumRoll, string Description, string LocationsDice, string FactionsDice);
public sealed record SettlementScarcityRule(SettlementScarcity Scarcity, int Roll, string Description);
public sealed record SettlementAtmosphereRule(SettlementAtmosphere Atmosphere, int Roll, string Description);
public sealed record GeneratedSettlement(SettlementPopulationRule Population, SettlementScarcityRule Scarcity, SettlementAtmosphereRule Atmosphere);

public static class SettlementGenerationRules
{
    private static readonly IReadOnlyList<SettlementPopulationRule> Populations =
    [
        new(SettlementPopulation.Barren, 1, 3, "No more than a dozen survivors working together.", "1d3", "1"),
        new(SettlementPopulation.Middling, 4, 5, "A few dozen from disparate corners.", "1d6", "1d3"),
        new(SettlementPopulation.Overcrowded, 6, 6, "Over a hundred souls, perhaps too many.", "2d6", "1d6"),
    ];
    private static readonly IReadOnlyDictionary<int, SettlementScarcityRule> Scarcities = new Dictionary<int, SettlementScarcityRule>
    {
        [1] = new(SettlementScarcity.Desperate, 1, "Cannot make purchases; only sell."), [2] = new(SettlementScarcity.LimitedInventory, 2, "Buy only 1d6 items collectively."), [3] = new(SettlementScarcity.SteepPrices, 3, "Items cost double."), [4] = new(SettlementScarcity.DifficultBargains, 4, "Every purchase also requires at least one barter item."), [5] = new(SettlementScarcity.Middling, 5, "Prices and availability are normal."), [6] = new(SettlementScarcity.Bountiful, 6, "When buying supplies, purchase one extra item free."),
    };
    private static readonly IReadOnlyDictionary<int, SettlementAtmosphereRule> Atmospheres = new Dictionary<int, SettlementAtmosphereRule>
    {
        [1] = new(SettlementAtmosphere.Hidden, 1, "Secretive dwellers hide in crawls and tunnels."), [2] = new(SettlementAtmosphere.Piety, 2, "Prayer, icons, and alien belief fill the settlement."), [3] = new(SettlementAtmosphere.Mirth, 3, "Revelers seek distraction in communal spaces."), [4] = new(SettlementAtmosphere.Despair, 4, "Broken souls endure curses, doom, and futility."), [5] = new(SettlementAtmosphere.Stoic, 5, "Ascetic dwellers live in quiet contemplation."), [6] = new(SettlementAtmosphere.Primal, 6, "Survival charms and half-remembered communication dominate."),
    };

    public static SettlementPopulationRule GetPopulation(int roll) => Populations.FirstOrDefault(rule => roll >= rule.MinimumRoll && roll <= rule.MaximumRoll) ?? throw new ArgumentOutOfRangeException(nameof(roll));
    public static SettlementScarcityRule GetScarcity(int roll) => Scarcities.TryGetValue(roll, out var scarcity) ? scarcity : throw new ArgumentOutOfRangeException(nameof(roll));
    public static SettlementAtmosphereRule GetAtmosphere(int roll) => Atmospheres.TryGetValue(roll, out var atmosphere) ? atmosphere : throw new ArgumentOutOfRangeException(nameof(roll));

    public static SettlementPopulationRule GetPopulationRule(SettlementPopulation population) => Populations.Single(rule => rule.Population == population);
    public static SettlementScarcityRule GetScarcityRule(SettlementScarcity scarcity) => Scarcities.Values.Single(rule => rule.Scarcity == scarcity);
    public static SettlementAtmosphereRule GetAtmosphereRule(SettlementAtmosphere atmosphere) => Atmospheres.Values.Single(rule => rule.Atmosphere == atmosphere);
    public static GeneratedSettlement Generate(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return new GeneratedSettlement(GetPopulation(random.Next(1, 7)), GetScarcity(random.Next(1, 7)), GetAtmosphere(random.Next(1, 7)));
    }
}
