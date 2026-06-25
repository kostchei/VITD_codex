using VastDark.Domain;
using static TestKit;

internal static class SettlementGenerationTests
{
    public static void Run()
    {
        GeneratesSourceContentFromDice();
        NonLimitedSettlementsRollNoStock();
        ReverseLookupsResolveCanonicalRules();
        LimitedMarketRehydratesAndDrawsDown();
        SettlementRoundTripsThroughState();
        CampaignGeneratesPersistsAndReusesSettlements();
    }

    private static readonly RegionalCoord SampleRegion = new(2, 3);
    private static readonly HexCoord SampleLocal = new(1, -1);

    private static void GeneratesSourceContentFromDice()
    {
        // Population d6 = 6 (Overcrowded), scarcity d6 = 2 (Limited Inventory), atmosphere d6 = 3 (Mirth),
        // then Limited Inventory rolls its 1d6 stock = 4.
        var settlement = Settlement.Generate(SampleRegion, SampleLocal, new ScriptedRandom(6, 2, 3, 4));
        Assert(settlement.Population == SettlementPopulation.Overcrowded, "Population roll 6 must be Overcrowded.");
        Assert(settlement.Scarcity == SettlementScarcity.LimitedInventory, "Scarcity roll 2 must be Limited Inventory.");
        Assert(settlement.Atmosphere == SettlementAtmosphere.Mirth, "Atmosphere roll 3 must be Mirth.");
        Assert(settlement.Market.RemainingLimitedPurchases == 4, "Limited Inventory must roll and store its 1d6 collective stock.");
    }

    private static void NonLimitedSettlementsRollNoStock()
    {
        // Population 1 (Barren), scarcity 5 (Middling), atmosphere 5 (Stoic) — no fourth die for stock.
        var settlement = Settlement.Generate(SampleRegion, SampleLocal, new ScriptedRandom(1, 5, 5));
        Assert(settlement.Scarcity == SettlementScarcity.Middling, "Scarcity roll 5 must be Middling.");
        Assert(settlement.Market.RemainingLimitedPurchases is null, "Non-limited settlements must not track limited stock.");
    }

    private static void ReverseLookupsResolveCanonicalRules()
    {
        Assert(SettlementGenerationRules.GetScarcityRule(SettlementScarcity.SteepPrices).Roll == 3, "Steep Prices must resolve back to roll 3.");
        Assert(SettlementGenerationRules.GetPopulationRule(SettlementPopulation.Overcrowded).Population == SettlementPopulation.Overcrowded, "Population reverse lookup must round-trip.");
        Assert(SettlementGenerationRules.GetAtmosphereRule(SettlementAtmosphere.Hidden).Roll == 1, "Hidden must resolve back to roll 1.");
    }

    private static void LimitedMarketRehydratesAndDrawsDown()
    {
        var market = new SettlementMarket(SettlementScarcity.LimitedInventory, 2);
        Assert(market.RemainingLimitedPurchases == 2, "A rehydrated limited market must restore its remaining stock.");
        Assert(market.Purchase(10, 2, supplies: false, offersBarterItem: false).Purchased && market.RemainingLimitedPurchases == 0, "Purchasing must draw down the limited stock.");
        Assert(!market.Purchase(10, 1, supplies: false, offersBarterItem: false).Purchased, "An exhausted limited market must reject further purchases.");
    }

    private static void SettlementRoundTripsThroughState()
    {
        var original = Settlement.Generate(SampleRegion, SampleLocal, new ScriptedRandom(6, 2, 3, 4));
        var restored = Settlement.FromState(original.ToState());
        Assert(
            restored.Region == SampleRegion && restored.Local == SampleLocal &&
            restored.Population == original.Population && restored.Scarcity == original.Scarcity &&
            restored.Atmosphere == original.Atmosphere && restored.Market.RemainingLimitedPurchases == 4,
            "A settlement must survive a state round-trip with its identity and remaining stock intact.");
    }

    private static void CampaignGeneratesPersistsAndReusesSettlements()
    {
        const int seed = 20240625;
        var campaign = new Campaign(new Random(seed));
        var (region, local) = FindSettlementCell(campaign);

        var first = campaign.GetSettlement(region, local);
        Assert(ReferenceEquals(first, campaign.GetSettlement(region, local)), "Revisiting a settlement cell must reuse the cached settlement.");

        // The same world seed must regenerate the identical settlement in a fresh campaign.
        var twin = new Campaign(new Random(seed));
        var twinSettlement = twin.GetSettlement(region, local);
        Assert(
            twinSettlement.Population == first.Population && twinSettlement.Scarcity == first.Scarcity && twinSettlement.Atmosphere == first.Atmosphere,
            "Settlement generation must be deterministic for a given world seed and cell.");

        // Persisting and reloading must restore the generated settlement.
        var restored = new Campaign(campaign.ToState());
        var restoredSettlement = restored.GetSettlement(region, local);
        Assert(
            restoredSettlement.Population == first.Population && restoredSettlement.Scarcity == first.Scarcity &&
            restoredSettlement.Atmosphere == first.Atmosphere && restoredSettlement.Market.RemainingLimitedPurchases == first.Market.RemainingLimitedPurchases,
            "A persisted campaign must restore each discovered settlement exactly.");
    }

    private static (RegionalCoord Region, HexCoord Local) FindSettlementCell(Campaign campaign)
    {
        foreach (var region in campaign.Regional.Cells)
        {
            var map = campaign.GetLocalMap(region);
            foreach (var local in map.VisibleCells)
            {
                if (map.GetTerrain(local) == Terrain.Settlements)
                {
                    return (region, local);
                }
            }
        }

        throw new InvalidOperationException("The test seed produced no settlement subhex to exercise.");
    }
}
