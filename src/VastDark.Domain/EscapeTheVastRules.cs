namespace VastDark.Domain;

public enum VastTerminalOutcome { InProgress, EscapedHome, ReawakenedInLies }

/// <summary>Page 37's explicit Leaving the Vast ritual, recorded as sequential source requirements.</summary>
public sealed class EscapeTheVastRitual
{
    public bool SoughtLushMendedFamiliar { get; private set; }
    public bool SearchedCannyClearBenign { get; private set; }
    public bool HidSpotOutsideSenses { get; private set; }
    public bool SpokeWish { get; private set; }
    public VastTerminalOutcome Outcome { get; private set; }
    public void SeekLushMendedFamiliar() => SoughtLushMendedFamiliar = true;
    public void SearchCannyClearBenign() { Require(SoughtLushMendedFamiliar); SearchedCannyClearBenign = true; }
    public void HideSpotOutsideSenses() { Require(SearchedCannyClearBenign); HidSpotOutsideSenses = true; }
    public void SpeakWishToLeaveTheVast() { Require(HidSpotOutsideSenses); SpokeWish = true; }
    public VastTerminalOutcome FallForward() { Require(SpokeWish); return Outcome = VastTerminalOutcome.EscapedHome; }
    public void ReawakenFromLiesDeath() => Outcome = VastTerminalOutcome.ReawakenedInLies;
    private static void Require(bool condition) { if (!condition) throw new InvalidOperationException("The Leaving the Vast ritual must follow the source sequence."); }
}
