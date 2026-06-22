namespace VastDark.Domain;

public enum RoamingHazardResolutionKind { Combat, DisplacementDamage, ExhaustionOrCrush, ConditionalLightning, SaveOrDisappear }

public sealed record RoamingHazardRule(
    int DieRoll, string Name, RoamingHazardResolutionKind Kind, string Procedure,
    string? DamageDice = null, string? SaveType = null, int? EncounterDiceSides = null,
    int? TerrainDestructionChanceInSix = null, string? Avoidance = null);

public static class RoamingHazardRules
{
    private static readonly IReadOnlyDictionary<int, RoamingHazardRule> Rules = new Dictionary<int, RoamingHazardRule>
    {
        [1] = new(1, "Warband", RoamingHazardResolutionKind.Combat, "Spawn 5d6 Cutthroats led by a Demagogue; they do not pursue into Ruins or a Settlement. Slaying the Demagogue destroys the hazard.", EncounterDiceSides: 6),
        [2] = new(2, "Maelstrom", RoamingHazardResolutionKind.DisplacementDamage, "Move caught Travelers 1 mile in a random direction.", "3d20", Avoidance: "Hide in Ruins or a sufficiently strong shelter."),
        [3] = new(3, "Crawlherd", RoamingHazardResolutionKind.Combat, "Spawn 1d20 random Crawl; Settlements avoid the herd. Slaying the Crawl destroys the hazard.", EncounterDiceSides: 20),
        [4] = new(4, "Collapse", RoamingHazardResolutionKind.ExhaustionOrCrush, "Each Traveler gains 1 exhaustion running or is crushed; Ruins and Settlements on the hex may become Wastes.", TerrainDestructionChanceInSix: 2),
        [5] = new(5, "Void Lightning", RoamingHazardResolutionKind.ConditionalLightning, "Travelers wearing or wielding metal may be struck by black lightning.", "10d6", TerrainDestructionChanceInSix: 3, Avoidance: "Strip off metal or hide in Ruins."),
        [6] = new(6, "Singing Sand", RoamingHazardResolutionKind.SaveOrDisappear, "Travelers on non-solid ground Save versus Breath or disappear into the ground.", SaveType: "Breath", Avoidance: "Seek high or solid ground."),
    };

    public static RoamingHazardRule Get(int dieRoll) => Rules.TryGetValue(dieRoll, out var rule) ? rule : throw new ArgumentOutOfRangeException(nameof(dieRoll));
}
