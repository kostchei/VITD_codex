namespace VastDark.Domain;

public sealed record RuinFeatureRule(int MinimumTotal, int? MaximumTotal, string Name, string Effect);

/// <summary>Page 28 1d20 + depth features. The printed table duplicates 25 and omits 24; both are preserved.</summary>
public static class RuinFeatureRules
{
    private static readonly IReadOnlyList<RuinFeatureRule> Features =
    [
        new(1,1,"Warning","Cryptic foreboding surface marks."), new(2,2,"Bug Nest","Harvest 1d3 × 1d3 rations."), new(3,3,"Effigies","Carved Traveler and monster figures."), new(4,4,"Tunnel","Tool/claw fissure leads to a random nearby room."), new(5,5,"Abandoned Camp","1d3 tools and 1d6 rations."), new(6,6,"Map","See the next 1d6 rooms."), new(7,7,"Devastation","Crossing takes twice as long."), new(8,8,"Stash","1d6 tools and 1d6 rations."), new(9,9,"Crevasse","3-in-6 chance of +1d3 Depth."), new(10,10,"Spoiled Pool","Foul water."),
        new(11,11,"Stagnant Pool","One week's clear tasteless water."), new(12,12,"Spawning Pool","One ration daily, or deplete for 1d6 rations."), new(13,13,"Dead Traveler","2-in-6 chance of random tool."), new(14,14,"Shaft","Tools required; +1 Depth."), new(15,15,"Caved-In","Impassable; remove additional entryways/exits."), new(16,16,"Stairs Down","+1 Depth."), new(17,17,"Loot","Roll Treasure."), new(18,18,"Bone Pile","Broken bones cover the floor."), new(19,19,"Freezing","Lingering/searching without fire or cold gear gains exhaustion."), new(20,20,"Excavation","Cavern room, lodestone deposit, or +1 Depth."),
        new(21,21,"Ration Stockpile","Enough food for a settlement for months."), new(22,22,"Howling Wind","Only shouting heard; unprotected lights extinguished."), new(23,23,"Totem","Monstrous bones/body parts/tools display."), new(25,25,"Vein of Metal","Mine and sell as raw lodestone."), new(25,25,"Hideout","1d6 Delvers; 1-in-6 hostile ambush, otherwise neutral; 1d6 treasures."), new(26,26,"Ancient Dwellings","Buried long-gone settlement crafts."), new(27,27,"Vein of Precious Metal","Mine as raw lodestone at twice value."), new(28,28,"Treasure","Roll Treasure."), new(29,29,"A Familiar Room","Calm and strangely familiar."), new(30,30,"Unstable","Damage with tools or explosives collapses room and pathways; Travelers must flee or be crushed."), new(31,null,"Entrance to the Deep","See page 30."),
    ];
    public static IReadOnlyList<RuinFeatureRule> Get(int total) => total < 1 ? throw new ArgumentOutOfRangeException(nameof(total)) : Features.Where(feature => total >= feature.MinimumTotal && (feature.MaximumTotal is null || total <= feature.MaximumTotal)).ToList();
}

public static class RuinCollapseService
{
    public static GeneratedRuin CollapseRoom(GeneratedRuin ruin, GridCoord room)
    {
        ArgumentNullException.ThrowIfNull(ruin);
        var rooms = ruin.Rooms.Where(candidate => candidate.Coordinate != room).ToList();
        if (rooms.Count == ruin.Rooms.Count) throw new ArgumentException("The selected room does not exist in this ruin.", nameof(room));
        var passages = ruin.Passages.Where(passage => passage.From != room && passage.To != room).ToList();
        return new GeneratedRuin(ruin.VisibleFaces, rooms, passages);
    }
}
