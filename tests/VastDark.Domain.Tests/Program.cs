using VastDark.Domain;

var regional = new RegionalMap(new Random(12345));
Assert(regional.Cells.Count == 80, "Regional map must contain 10 × 8 cells.");
Assert(regional.Contains(new RegionalCoord(9, 7)), "Regional map must include its final cell.");
Assert(!regional.Contains(new RegionalCoord(10, 7)), "Regional map must reject out-of-bounds cells.");
Assert(regional.DiceRolls.Count == RegionalMap.DiceCount, "Regional generation must place exactly eight dice.");
Assert(regional.DiceRolls.Keys.Distinct().Count() == RegionalMap.DiceCount, "Regional dice must occupy distinct hexes.");
foreach (var (coordinate, roll) in regional.DiceRolls)
{
    var expected = roll switch { 1 => Terrain.Wastes, <= 4 => Terrain.Ruins, _ => Terrain.Pillars };
    Assert(regional.GetTerrain(coordinate) == expected, "Regional die result must select the documented terrain.");
}

var local = new LocalMap(new RegionalCoord(0, 0));
Assert(local.Cells.Count == 91, "The local tessellation must cover every clipped edge subhex.");
Assert(LocalMap.SideLengthInSubhexes == 6, "Local map must have six one-mile subhexes along each edge.");
Assert(LocalMap.FlatToFlatSubhexes == 6, "Local map must span six one-mile hex widths flat-to-flat.");
Assert(local.Contains(HexCoord.Zero), "Local map must include its centre.");
Assert(local.Contains(new HexCoord(5, 0)), "Local map must include its sixth edge subhex.");
Assert(!local.Contains(new HexCoord(6, 0)), "Local map must exclude cells outside its six-side footprint.");
Assert(HexCoord.Zero.DistanceTo(new HexCoord(2, -1)) == 2, "Axial hex distance must be correct.");
Assert(local.RoamingHazards.Count is >= 1 and <= 6, "Local maps must start with 1d6 roaming hazards.");
Assert(local.VisibleCells.Count == 41, $"The local map must expose the 41 fully visible subhexes; found {local.VisibleCells.Count}.");
Assert(local.RoamingHazards.Keys.All(local.VisibleCells.Contains), "Roaming hazards must start on fully visible local cells.");
Assert(local.RoamingHazards.Values.All(roll => roll is >= 1 and <= 6), "Roaming hazard faces must be valid d6 values.");
var originalHazardFaces = local.RoamingHazards.Values.Order().ToArray();
local.AdvanceRoamingHazards(new Random(98765));
Assert(local.RoamingHazardDay == 1, "Advancing hazards must increment the local hazard day.");
Assert(local.RoamingHazards.Count == originalHazardFaces.Length, "Advancing hazards must preserve the number of hazards.");
Assert(local.RoamingHazards.Keys.Distinct().Count() == originalHazardFaces.Length, "Roaming hazards must occupy distinct cells after moving.");
Assert(local.RoamingHazards.Values.Order().SequenceEqual(originalHazardFaces), "Moving hazards must preserve their face values.");
Assert(LocalMap.GetRoamingHazardName(4) == "Void Lightning", "Hazard names must match their d6 result.");

var ruinsLocal = new LocalMap(new RegionalCoord(0, 0), Terrain.Ruins, new Random(23456));
Assert(ruinsLocal.DiceCount is 6 or 12 or 32, "Local density must choose 6, 12, or 32 dice.");
Assert(ruinsLocal.DiceRolls.Count == ruinsLocal.DiceCount, "Local dice must occupy distinct hexes.");
Assert(ruinsLocal.DiceRolls.Keys.All(ruinsLocal.VisibleCells.Contains), "Terrain dice must only occupy fully visible local cells.");
foreach (var (coordinate, roll) in ruinsLocal.DiceRolls)
{
    var expected = roll switch { 1 => Terrain.Wastes, <= 4 => Terrain.Ruins, _ => Terrain.Settlements };
    Assert(ruinsLocal.GetTerrain(coordinate) == expected, "Ruins local table must match its d6 result.");
}

var wastesLocal = new LocalMap(new RegionalCoord(0, 0), Terrain.Wastes, new Random(34567));
foreach (var (coordinate, roll) in wastesLocal.DiceRolls)
{
    Assert(wastesLocal.GetTerrain(coordinate) == (roll == 6 ? Terrain.Ruins : Terrain.Wastes), "Wastes local table must only produce ruins on a 6.");
}

var pillarsLocal = new LocalMap(new RegionalCoord(0, 0), Terrain.Pillars, new Random(45678));
Assert(pillarsLocal.DiceRolls.Count == pillarsLocal.DiceCount, "Pillars local maps must still place the density roll's dice.");
Assert(pillarsLocal.TerrainByCell.Values.All(terrain => terrain == Terrain.Pillars), "Pillars local maps must remain entirely pillar structures.");

var savePath = Path.Combine(Path.GetTempPath(), $"vastdark-{Guid.NewGuid():N}.json");
try
{
    var generatedCampaign = new Campaign(new Random(56789));
    var generatedLocal = generatedCampaign.GetLocalMap(new RegionalCoord(2, 2));
    CampaignFile.Save(generatedCampaign, savePath);
    var loadedCampaign = CampaignFile.LoadOrCreate(savePath);
    Assert(loadedCampaign.Regional.DiceRolls.Count == RegionalMap.DiceCount, "Saved regional dice must reload.");
    foreach (var coordinate in generatedCampaign.Regional.Cells)
    {
        Assert(loadedCampaign.Regional.GetTerrain(coordinate) == generatedCampaign.Regional.GetTerrain(coordinate), "Saved regional terrain must reload exactly.");
    }

    var loadedLocal = loadedCampaign.GetLocalMap(new RegionalCoord(2, 2));
    Assert(loadedLocal.DensityRoll == generatedLocal.DensityRoll && loadedLocal.DiceCount == generatedLocal.DiceCount, "Saved local density must reload exactly.");
    foreach (var coordinate in generatedLocal.Cells)
    {
        Assert(loadedLocal.GetTerrain(coordinate) == generatedLocal.GetTerrain(coordinate), "Saved local terrain must reload exactly.");
    }

    Assert(loadedLocal.RoamingHazardDay == generatedLocal.RoamingHazardDay, "Saved roaming hazard day must reload exactly.");
    Assert(loadedLocal.RoamingHazards.OrderBy(hazard => hazard.Key.Q).ThenBy(hazard => hazard.Key.R)
        .SequenceEqual(generatedLocal.RoamingHazards.OrderBy(hazard => hazard.Key.Q).ThenBy(hazard => hazard.Key.R)), "Saved roaming hazards must reload exactly.");
}
finally
{
    if (File.Exists(savePath))
    {
        File.Delete(savePath);
    }
}

var dungeon = PrototypeDungeonBuilder.CreateSixLevelDungeon();
Assert(dungeon.Levels.Count == 6, "Prototype dungeon must expose six levels.");
Assert(dungeon.GetLevel(5).Width == 32 && dungeon.GetLevel(5).Height == 24, "Dungeon grid dimensions changed unexpectedly.");
Assert(dungeon.GetLevel(0).GetTile(new GridCoord(20, 8)) == DungeonTile.StairDown, "Level zero must descend.");
Assert(dungeon.GetLevel(5).GetTile(new GridCoord(6, 6)) == DungeonTile.StairUp, "Bottom level must ascend.");

var navigation = new MapNavigationService(new Campaign());
navigation.SelectRegional(Campaign.DungeonRegionalCoordinate);
navigation.EnterLocal(Campaign.DungeonRegionalCoordinate);
Assert(navigation.TryEnterDungeon(), "Dungeon entrance must be reachable from its local map.");
navigation.SetDungeonDepth(5);
Assert(navigation.Current is MapLocation.Dungeon { Depth: 5 }, "Dungeon depth selection failed.");
navigation.ReturnToLocal();
Assert(navigation.Current is MapLocation.Local, "Dungeon must return to its local map.");

Console.WriteLine("All VastDark domain checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
