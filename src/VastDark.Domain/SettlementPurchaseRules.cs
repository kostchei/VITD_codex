namespace VastDark.Domain;

public sealed class SettlementMarket
{
    private int? _remainingLimitedPurchases;
    public SettlementMarket(SettlementScarcity scarcity, IRandomSource? random = null)
    {
        Scarcity = scarcity;
        if (scarcity == SettlementScarcity.LimitedInventory)
        {
            ArgumentNullException.ThrowIfNull(random);
            _remainingLimitedPurchases = random.Next(1, 7);
        }
    }

    /// <summary>Rehydrates a saved market, restoring its remaining limited-inventory purchases.</summary>
    public SettlementMarket(SettlementScarcity scarcity, int? remainingLimitedPurchases)
    {
        Scarcity = scarcity;
        _remainingLimitedPurchases = scarcity == SettlementScarcity.LimitedInventory
            ? remainingLimitedPurchases ?? throw new ArgumentNullException(nameof(remainingLimitedPurchases), "Limited inventory settlements require a remaining purchase count.")
            : null;
    }

    public SettlementScarcity Scarcity { get; }
    public int? RemainingLimitedPurchases => _remainingLimitedPurchases;

    public SettlementPurchaseResult Purchase(int normalCoinCost, int quantity, bool supplies, bool offersBarterItem)
    {
        if (normalCoinCost < 0 || quantity < 1) throw new ArgumentOutOfRangeException(nameof(normalCoinCost));
        if (Scarcity == SettlementScarcity.Desperate) return SettlementPurchaseResult.Rejected("Desperate settlements permit selling only.");
        if (Scarcity == SettlementScarcity.DifficultBargains && !offersBarterItem) return SettlementPurchaseResult.Rejected("A barter item is required for every purchase.");
        if (_remainingLimitedPurchases is { } remaining && quantity > remaining) return SettlementPurchaseResult.Rejected("The settlement's limited inventory is exhausted.");
        if (_remainingLimitedPurchases is not null) _remainingLimitedPurchases -= quantity;
        var coinCost = normalCoinCost * quantity * (Scarcity == SettlementScarcity.SteepPrices ? 2 : 1);
        var received = quantity + (Scarcity == SettlementScarcity.Bountiful && supplies ? 1 : 0);
        return new SettlementPurchaseResult(true, coinCost, received, null);
    }
}

public sealed record SettlementPurchaseResult(bool Purchased, int CoinCost, int QuantityReceived, string? Failure)
{
    public static SettlementPurchaseResult Rejected(string failure) => new(false, 0, 0, failure);
}

public static class SettlementRestService
{
    /// <summary>Storytellers add a 1-in-6 extra exhaustion recovery during settlement rest.</summary>
    public static bool TryRecoverStorytellerExhaustion(Traveler traveler, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(traveler);
        ArgumentNullException.ThrowIfNull(random);
        return random.Next(1, 7) == 1 && traveler.RecoverExhaustionFromFullRest();
    }
}
