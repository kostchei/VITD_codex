namespace VastDark.Domain;

public enum DeepTrial { Scale, Repetition, Change, Emptiness, Sacrifice }
public sealed record DeepTrialRule(DeepTrial Trial, string Danger, string WayOut);

/// <summary>State transitions for the five trials specified on pages 34-35.</summary>
public sealed class DeepTrialState
{
    public DeepTrialState(DeepTrial trial) => Trial = trial;
    public DeepTrial Trial { get; }
    public int ScaleReturnDistance { get; private set; }
    public int ChangeRotationDegrees { get; private set; }
    public int EmptinessDistanceTravelled { get; private set; }
    public bool SimulacraActive { get; private set; }
    public bool ExitOpen { get; private set; }

    public void EnterUnexploredScaleRoom() { Require(DeepTrial.Scale); ScaleReturnDistance = ScaleReturnDistance == 0 ? 2 : ScaleReturnDistance * 2; }
    public bool ReturnToScaleOrigin(int travelledBack) { Require(DeepTrial.Scale); ExitOpen = travelledBack >= ScaleReturnDistance; return ExitOpen; }
    public void EnterReflectionRoom() { Require(DeepTrial.Repetition); SimulacraActive = true; }
    public void EnterChangeRoom() { Require(DeepTrial.Change); ChangeRotationDegrees = (ChangeRotationDegrees + 90) % 360; }
    public bool ReachChangeCenter(bool atCenter) { Require(DeepTrial.Change); ExitOpen = atCenter; return ExitOpen; }
    public bool TraverseEmptiness(int distance, bool hasRigging)
    {
        Require(DeepTrial.Emptiness);
        if (distance < 1) throw new ArgumentOutOfRangeException(nameof(distance));
        if (distance > 100 && !hasRigging) return false;
        EmptinessDistanceTravelled += distance;
        ExitOpen = EmptinessDistanceTravelled >= 1000;
        return true;
    }
    public bool ResolveSacrifice(bool mortalRemainsBehind) { Require(DeepTrial.Sacrifice); ExitOpen = mortalRemainsBehind; return ExitOpen; }
    private void Require(DeepTrial expected) { if (Trial != expected) throw new InvalidOperationException("This action does not apply to the current trial."); }
}

public static class DeepTrialRules
{
    private static readonly IReadOnlyDictionary<DeepTrial, DeepTrialRule> Rules = new Dictionary<DeepTrial, DeepTrialRule>
    {
        [DeepTrial.Scale] = new(DeepTrial.Scale,"Each unexplored room creates one behind, doubling return distance.","Return to original entry to find descent and original entrance."),
        [DeepTrial.Repetition] = new(DeepTrial.Repetition,"Reflection rooms contain weaponless Simulacra that replace slain counterparts.","The source page supplies no separate exit procedure."),
        [DeepTrial.Change] = new(DeepTrial.Change,"Area rotates 90 degrees clockwise whenever someone enters a room; disconnected passages are sheer drops.","Reach center-most room pit."),
        [DeepTrial.Emptiness] = new(DeepTrial.Emptiness,"No gravity; bad jumps drift out of view and disappear; rigging prevents it.","Reach light 1000 ft away directly or via 100-ft slabs."),
        [DeepTrial.Sacrifice] = new(DeepTrial.Sacrifice,"A delver flees; the room only exits while one mortal remains.","Leave one mortal behind to keep passage open."),
    };
    public static DeepTrialRule Get(DeepTrial trial) => Rules[trial];
}
