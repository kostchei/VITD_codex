namespace VastDark.Domain;

public sealed record RuinEncounterRule(int MinimumTotal, int? MaximumTotal, string Name, string Quantity, string Description, bool RequiresMood = false);
public sealed record RuinMoodRule(int Roll, string Name, string Effect);
public sealed record RuinCreatureStatRule(string Name, int HitDice, int HitPoints, string Armor, string SpecialRules);

public static class RuinEncounterRules
{
    private static readonly IReadOnlyList<RuinEncounterRule> Encounters =
    [
        new(1,5,"Nothing","","You are alone for now."), new(6,6,"Lost Travelers","1d6","Helpful if assisted."), new(7,7,"Merchants","1d3","Heavy-pulk traders; limit 100 coin."), new(8,8,"Bandits","1d6","Prowling for an easy score.", true), new(9,9,"Travelers","2d6","Searching for supplies or treasure; friendly if joined, hostile if stopped."), new(10,10,"Cyclops","1d6","Crawling through the dark."), new(11,11,"Waning Lodge Devotees","1d6","Demolishing the room; it collapses on return unless stopped."), new(12,12,"Harpies","1d6","Clinging to walls."), new(13,13,"Medusa","1d3","Hidden and listening."), new(14,14,"Delvers","1d8","Haven't seen surface for a long time.", true), new(15,15,"Ogre","1","Slithers across walls and floor."), new(16,16,"Shades","1d4","Scouring for food."), new(17,17,"Lone Survivor","1","Warns of something terrible."), new(18,18,"Centaur","1","Searching limbs move across ceiling."), new(19,19,"Sirens","1d3","Waiting silently for prey."), new(20,20,"Griffon","1","Hungrily watches from above."), new(21,21,"Hydra","1","Screaming limbs echo."), new(22,null,"Wyrm","1","Arrives in terrible splendor."),
    ];
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, RuinMoodRule>> Moods = new Dictionary<string, IReadOnlyDictionary<int, RuinMoodRule>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Bandits"] = new Dictionary<int, RuinMoodRule> { [1] = new(1,"Crazed","Attacks to kill and loot."), [2] = new(2,"Crazed","Attacks to kill and loot."), [3] = new(3,"Tribute","Demands 100 coins or one ration per Traveler."), [4] = new(4,"Tribute","Demands 100 coins or one ration per Traveler."), [5] = new(5,"Tribute","Demands 100 coins or one ration per Traveler."), [6] = new(6,"Curious","Lets Travelers pass if they answer questions.") },
        ["Delvers"] = new Dictionary<int, RuinMoodRule> { [1] = new(1,"Vicious","Attacks."), [2] = new(2,"Curious","Cautiously allows strangers into camp."), [3] = new(3,"Curious","Cautiously allows strangers into camp."), [4] = new(4,"Curious","Cautiously allows strangers into camp."), [5] = new(5,"Helpful","Trades; limit 2000 coin."), [6] = new(6,"Helpful","Trades; limit 2000 coin.") },
    };
    private static readonly IReadOnlyDictionary<string, RuinCreatureStatRule> StatBlocks = new Dictionary<string, RuinCreatureStatRule>(StringComparer.OrdinalIgnoreCase)
    {
        ["Bandits"] = new("Bandits",2,10,"Hide","Attack as weapon; retreats at half health."), ["Traveler or Cutthroat"] = new("Traveler or Cutthroat",3,18,"Scale","Attack as weapon; 1-in-6 spell; 1-in-20 treasure; retreats at half health, 2-in-6 fights to death."), ["Delvers"] = new("Delvers",5,20,"Scale","Attack as weapon; 1-in-6 knows 1d3 spells; 1-in-20 treasure; fights to death."),
    };
    public static RuinEncounterRule Get(int total) => total < 1 ? throw new ArgumentOutOfRangeException(nameof(total)) : Encounters.First(rule => total >= rule.MinimumTotal && (rule.MaximumTotal is null || total <= rule.MaximumTotal));
    public static RuinMoodRule GetMood(string encounter, int roll) => Moods.TryGetValue(encounter, out var moods) && moods.TryGetValue(roll, out var mood) ? mood : throw new ArgumentOutOfRangeException(nameof(roll));
    public static RuinCreatureStatRule GetStatBlock(string name) => StatBlocks.TryGetValue(name, out var statBlock) ? statBlock : throw new ArgumentOutOfRangeException(nameof(name));
}
