using System.Text.Json;

namespace VastDark.Domain;

public sealed record RegionalCellState(int Column, int Row, Terrain Terrain, int? DieRoll);

public sealed record LocalCellState(int Q, int R, Terrain Terrain, int? DieRoll);

public sealed record RoamingHazardState(int Q, int R, int DieRoll);

public sealed record LocalMapOverlayState(
    int ParentColumn,
    int ParentRow,
    List<RoamingHazardState>? RoamingHazards = null,
    int RoamingHazardDay = 0);

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
    List<LocalMapState>? LocalMaps = null,
    PartyTravelStateState? PartyTravel = null,
    PartyState? Party = null,
    List<TravelLogEntryState>? TravelLog = null,
    int? Version = null,
    int? WorldSeed = null,
    List<LocalMapOverlayState>? LocalMapOverlays = null,
    PillarDelveState? PillarDelve = null);

public static class CampaignFile
{
    public const int CurrentVersion = 2;

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
                    ValidateVersion(state);
                    return new Campaign(state);
                }
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException or IOException)
            {
                QuarantineInvalidSave(path, exception);
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

    private static void ValidateVersion(CampaignState state)
    {
        if (state.Version is null)
        {
            return;
        }

        if (state.Version != CurrentVersion)
        {
            throw new InvalidDataException($"Unsupported campaign save version {state.Version}; expected {CurrentVersion}.");
        }
    }

    private static void QuarantineInvalidSave(string path, Exception exception)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var quarantinePath = CreateQuarantinePath(path);
        try
        {
            File.Move(path, quarantinePath);
            File.WriteAllText(quarantinePath + ".reason.txt", exception.Message);
        }
        catch (Exception quarantineException) when (quarantineException is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"The campaign save at '{path}' is invalid and could not be quarantined.", quarantineException);
        }
    }

    private static string CreateQuarantinePath(string path)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var candidate = Path.Combine(directory ?? string.Empty, $"{fileName}.invalid-{stamp}");
        var suffix = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory ?? string.Empty, $"{fileName}.invalid-{stamp}-{suffix}");
            suffix++;
        }

        return candidate;
    }
}
