using System.Text.Json;

namespace VastDark.Domain;

public sealed record RegionalCellState(int Column, int Row, Terrain Terrain, int? DieRoll);

public sealed record LocalCellState(int Q, int R, Terrain Terrain, int? DieRoll);

public sealed record RoamingHazardState(int Q, int R, int DieRoll);

public sealed record NamedValueState(string Name, int Value);

public sealed record InventoryItemState(string Name, int Slots, bool IsUniqueOrMagical);
public sealed record LoadoutState(string Purpose, int Slots);
public sealed record TravelerRulesState(
    List<InventoryItemState>? Items = null,
    List<LoadoutState>? Loadouts = null,
    List<PackType>? Packs = null,
    List<string>? Memories = null,
    int Rites = 0,
    int LockedErosionExhaustion = 0,
    List<string>? RiteMotionLocations = null,
    List<DeepGift>? Gifts = null,
    List<DeepGift>? GiftDailyUses = null,
    List<WastesFaction>? WastesFactions = null,
    List<SettlementFaction>? SettlementFactions = null);

public sealed record TravelerState(
    string Name,
    int Health,
    int Rations,
    int Exhaustion,
    List<NamedValueState>? Skills = null,
    List<NamedValueState>? Resources = null,
    List<string>? Conditions = null,
    AbilityScores? AbilityScores = null,
    List<ExhaustionSource>? ExhaustionSources = null,
    int Level = 1,
    Vitality? Vitality = null,
    TravelerRulesState? Rules = null);

public sealed record PartyState(List<TravelerState> Members);

public sealed record TravelLogEntryState(int Day, string Message);

public sealed record LocalMapState(
    int ParentColumn,
    int ParentRow,
    Terrain ParentTerrain,
    int DensityRoll,
    int DiceCount,
    List<LocalCellState> Cells,
    List<RoamingHazardState>? RoamingHazards = null,
    int RoamingHazardDay = 0);

public sealed record PartyTravelStateState(
    int RegionalColumn,
    int RegionalRow,
    int LocalQ,
    int LocalR,
    int Day,
    int DailyMiles,
    int Exhaustion,
    bool ForcedMarchUsed,
    int Rations = 0);

public sealed record CampaignState(
    List<RegionalCellState> RegionalCells,
    List<LocalMapState> LocalMaps,
    PartyTravelStateState? PartyTravel = null,
    PartyState? Party = null,
    List<TravelLogEntryState>? TravelLog = null);

public static class CampaignFile
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static Campaign LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                var state = JsonSerializer.Deserialize<CampaignState>(File.ReadAllText(path), JsonOptions);
                if (state is not null)
                {
                    return new Campaign(state);
                }
            }
            catch (JsonException)
            {
                // A malformed save is replaced with a new valid campaign.
            }
            catch (InvalidDataException)
            {
                // A structurally invalid save is replaced with a new valid campaign.
            }
            catch (IOException)
            {
                // Use a new in-memory campaign if the existing file cannot be read.
            }
        }

        var campaign = new Campaign();
        Save(campaign, path);
        return campaign;
    }

    public static void Save(Campaign campaign, string path)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(campaign.ToState(), JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }
}
