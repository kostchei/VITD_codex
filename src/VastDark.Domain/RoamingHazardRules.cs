namespace VastDark.Domain;

public enum RoamingHazardResolutionKind { Combat, DisplacementDamage, ExhaustionOrCrush, ConditionalLightning, SaveOrDisappear }

public sealed record RoamingHazardRule(
    int DieRoll, string Name, RoamingHazardResolutionKind Kind, string Procedure,
    string? DamageDice = null, string? SaveType = null, int? EncounterDiceSides = null,
    int? TerrainDestructionChanceInSix = null, string? Avoidance = null);

public sealed record RoamingHazardContext(
    Terrain Terrain,
    bool HasStrongShelter = false,
    bool RunFromCollapse = true,
    bool HasExposedMetal = false,
    bool OnSolidOrRockyGround = false);

public sealed record HazardDamage(string TravelerName, int Amount);
public sealed record HazardDisplacement(string TravelerName, int Direction);
public sealed record RoamingHazardResolution(
    RoamingHazardRule Rule,
    int? CombatantCount,
    IReadOnlyList<HazardDamage> Damage,
    IReadOnlyList<HazardDisplacement> Displacements,
    IReadOnlyList<string> ExhaustedTravelers,
    IReadOnlyList<string> CrushedTravelers,
    IReadOnlyList<string> BreathSaveTravelers,
    bool TerrainReducedToWastes);

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

/// <summary>Resolves the mechanical consequences of entering a page 11 roaming-hazard hex. Combat and save choices remain with their dedicated systems.</summary>
public static class RoamingHazardService
{
    public static RoamingHazardResolution Resolve(int dieRoll, TravelParty party, RoamingHazardContext context, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(random);
        var rule = RoamingHazardRules.Get(dieRoll);
        var damage = new List<HazardDamage>();
        var displacements = new List<HazardDisplacement>();
        var exhausted = new List<string>();
        var crushed = new List<string>();
        var breathSaves = new List<string>();
        int? combatantCount = null;
        var terrainReduced = false;

        switch (dieRoll)
        {
            case 1:
                combatantCount = Roll(random, 5, 6);
                break;
            case 2:
                if (context.Terrain is not Terrain.Ruins && !context.HasStrongShelter)
                {
                    foreach (var traveler in party.Members)
                    {
                        displacements.Add(new HazardDisplacement(traveler.Name, random.Next(0, 6)));
                        damage.Add(new HazardDamage(traveler.Name, Roll(random, 3, 20)));
                    }
                }
                break;
            case 3:
                if (context.Terrain != Terrain.Settlements) combatantCount = Roll(random, 1, 20);
                break;
            case 4:
                foreach (var traveler in party.Members)
                {
                    if (context.RunFromCollapse)
                    {
                        traveler.AddExhaustion(1, ExhaustionSource.Unspecified);
                        exhausted.Add(traveler.Name);
                    }
                    else crushed.Add(traveler.Name);
                }
                terrainReduced = context.Terrain is Terrain.Ruins or Terrain.Settlements && random.Next(1, 7) <= 2;
                break;
            case 5:
                if (context.Terrain != Terrain.Ruins && context.HasExposedMetal)
                {
                    foreach (var traveler in party.Members)
                    {
                        if (random.Next(1, 7) <= 3) damage.Add(new HazardDamage(traveler.Name, Roll(random, 10, 6)));
                    }
                }
                break;
            case 6:
                if (!context.OnSolidOrRockyGround)
                {
                    breathSaves.AddRange(party.Members.Select(traveler => traveler.Name));
                }
                break;
        }

        return new RoamingHazardResolution(rule, combatantCount, damage, displacements, exhausted, crushed, breathSaves, terrainReduced);
    }

    private static int Roll(IRandomSource random, int dice, int sides) => Enumerable.Range(0, dice).Sum(_ => random.Next(1, sides + 1));
}
