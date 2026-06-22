namespace VastDark.Domain;

public enum DeepGift { Torpor, VoiceOfTheCrawl, GraftedLimbs, GiftOfTheCyclops, TasteForFlesh, VoidOfPresence, LodestoneHunger, UnhollowCannibal, Melder, GravitySpider }
public sealed record DeepGiftRule(DeepGift Gift, int Roll, string Effect, bool OncePerDay = false);

public sealed class DeepGiftState
{
    private readonly List<DeepGift> _gifts = [];
    private readonly HashSet<DeepGift> _dailyUses = [];
    public IReadOnlyList<DeepGift> Gifts => _gifts;
    public DeepGift GainOnEnterOrNewLevel(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var gift = DeepGiftRules.Get(random.Next(1, 11)).Gift;
        _gifts.Add(gift);
        return gift;
    }
    public bool TryUse(DeepGift gift)
    {
        if (!_gifts.Contains(gift)) return false;
        var rule = DeepGiftRules.Get(gift);
        return !rule.OncePerDay || _dailyUses.Add(gift);
    }
    public void ResetDay() => _dailyUses.Clear();
}

public static class DeepGiftRules
{
    private static readonly IReadOnlyDictionary<int, DeepGiftRule> Gifts = new Dictionary<int, DeepGiftRule>
    {
        [1] = new(DeepGift.Torpor,1,"While motionless, need no food or energy; unlimited duration."), [2] = new(DeepGift.VoiceOfTheCrawl,2,"Speak to Crawl; audible mortals suffer 1d6 damage."), [3] = new(DeepGift.GraftedLimbs,3,"Sever/attach fresh limb: all Grit and 1d6 Flesh; gain its attacks/physical abilities after healing.",true), [4] = new(DeepGift.GiftOfTheCyclops,4,"Call changed beings at any distance; Cyclops bow and do not strike."), [5] = new(DeepGift.TasteForFlesh,5,"Eat Crawl body to recover 1d6 Grit or 1 Flesh."), [6] = new(DeepGift.VoidOfPresence,6,"Disappear from perception for one hour per level; announce to be seen.",true), [7] = new(DeepGift.LodestoneHunger,7,"Eat 1d10 lodestone in place of a meal."), [8] = new(DeepGift.UnhollowCannibal,8,"Eat a Traveler/mortal to regain a victim memory."), [9] = new(DeepGift.Melder,9,"Attached to flesh: deal 1d3 damage and heal 1d3 Grit each turn until torn/cut off."), [10] = new(DeepGift.GravitySpider,10,"Walk walls and ceilings perfectly."),
    };
    public static DeepGiftRule Get(int roll) => Gifts.TryGetValue(roll, out var gift) ? gift : throw new ArgumentOutOfRangeException(nameof(roll));
    public static DeepGiftRule Get(DeepGift gift) => Gifts.Values.Single(rule => rule.Gift == gift);
}
