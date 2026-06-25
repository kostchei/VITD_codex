namespace VastDark.Domain;

/// <summary>
/// A discovered settlement bound to a specific local cell. Its population, scarcity, and atmosphere
/// are generated once and then persisted so repeat visits — and the limited-inventory stock that
/// purchases draw down — stay consistent across sessions.
/// </summary>
public sealed class Settlement
{
    private Settlement(RegionalCoord region, HexCoord local, GeneratedSettlement details, SettlementMarket market)
    {
        Region = region;
        Local = local;
        Details = details;
        Market = market;
    }

    public RegionalCoord Region { get; }
    public HexCoord Local { get; }
    public GeneratedSettlement Details { get; }
    public SettlementMarket Market { get; }

    public SettlementPopulation Population => Details.Population.Population;
    public SettlementScarcity Scarcity => Details.Scarcity.Scarcity;
    public SettlementAtmosphere Atmosphere => Details.Atmosphere.Atmosphere;

    public static Settlement Generate(RegionalCoord region, HexCoord local, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var details = SettlementGenerationRules.Generate(random);
        var market = details.Scarcity.Scarcity == SettlementScarcity.LimitedInventory
            ? new SettlementMarket(SettlementScarcity.LimitedInventory, random)
            : new SettlementMarket(details.Scarcity.Scarcity);
        return new Settlement(region, local, details, market);
    }

    public static Settlement FromState(SettlementState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var details = new GeneratedSettlement(
            SettlementGenerationRules.GetPopulationRule(state.Population),
            SettlementGenerationRules.GetScarcityRule(state.Scarcity),
            SettlementGenerationRules.GetAtmosphereRule(state.Atmosphere));
        var market = new SettlementMarket(state.Scarcity, state.RemainingLimitedPurchases);
        return new Settlement(new RegionalCoord(state.ParentColumn, state.ParentRow), new HexCoord(state.LocalQ, state.LocalR), details, market);
    }

    public SettlementState ToState() => new(
        Region.Column,
        Region.Row,
        Local.Q,
        Local.R,
        Population,
        Scarcity,
        Atmosphere,
        Market.RemainingLimitedPurchases);
}
