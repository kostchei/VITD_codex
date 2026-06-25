using System.Text.Json;
using VastDark.Domain;
using static TestKit;

internal static class PersistenceTests
{
    public static void Run()
    {
        var savePath = Path.Combine(Path.GetTempPath(), $"vastdark-{Guid.NewGuid():N}.json");
        try
        {
            var generatedCampaign = new Campaign(new Random(56789));
            var generatedLocal = generatedCampaign.GetLocalMap(new RegionalCoord(2, 2));
            var generatedTraveler = generatedCampaign.Party.Members[0];
            generatedTraveler.AddRations(2);
            generatedTraveler.SetSkill("Survival", 3);
            generatedTraveler.SetResource("Water", 4);
            generatedTraveler.AddCondition("Irradiated");
            CampaignFile.Save(generatedCampaign, savePath);
            var savedState = JsonSerializer.Deserialize<CampaignState>(File.ReadAllText(savePath));
            if (savedState is null)
            {
                throw new InvalidOperationException("Saved campaign JSON must deserialize.");
            }

            Assert(savedState.Version == CampaignFile.CurrentVersion, "Saved campaigns must be stamped with the current save version.");
            Assert(savedState.WorldSeed is not null, "New campaign saves must include the deterministic world seed.");
            Assert(savedState.LocalMaps is { Count: 0 }, "New campaign saves must not persist full generated local maps.");
            Assert(savedState.LocalMapOverlays is { Count: 1 } &&
                   savedState.LocalMapOverlays[0].RoamingHazards?.Count == generatedLocal.RoamingHazards.Count,
                "New campaign saves must persist local-map dynamic overlays.");
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
            Assert(loadedCampaign.PartyTravel.RegionalCoordinate == generatedCampaign.PartyTravel.RegionalCoordinate &&
                   loadedCampaign.PartyTravel.LocalCoordinate == generatedCampaign.PartyTravel.LocalCoordinate &&
                   loadedCampaign.PartyTravel.DailyMiles == generatedCampaign.PartyTravel.DailyMiles,
                "Saved party travel state must reload exactly.");
            Assert(loadedCampaign.Party.Members.Select(member => member.Name).SequenceEqual(generatedCampaign.Party.Members.Select(member => member.Name)),
                "Saved party members must reload exactly.");
            var loadedTraveler = loadedCampaign.Party.Members[0];
            Assert(loadedTraveler.Rations == 2 && loadedTraveler.GetSkill("Survival") == 3 && loadedTraveler.GetResource("Water") == 4 && loadedTraveler.Conditions.Contains("Irradiated"),
                "Saved party supplies, skills, resources, and conditions must reload exactly.");
            Assert(loadedCampaign.TravelLog.SequenceEqual(generatedCampaign.TravelLog), "Saved travel log entries must reload exactly.");
        }
        finally
        {
            DeleteSaveArtifacts(savePath);
        }

        var malformedPath = Path.Combine(Path.GetTempPath(), $"vastdark-malformed-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(malformedPath, "{ malformed json");
            var replacement = CampaignFile.LoadOrCreate(malformedPath);
            Assert(replacement.Regional.DiceRolls.Count == RegionalMap.DiceCount, "A malformed save must be replaced with a valid campaign after quarantine.");
            var quarantined = QuarantinedSaves(malformedPath);
            Assert(quarantined.Count == 1 && File.Exists(quarantined[0] + ".reason.txt"), "A malformed save must be quarantined with a reason file.");
            Assert(File.Exists(malformedPath), "A fresh campaign save must be written after quarantining a malformed save.");
        }
        finally
        {
            DeleteSaveArtifacts(malformedPath);
        }

        var futureVersionPath = Path.Combine(Path.GetTempPath(), $"vastdark-future-{Guid.NewGuid():N}.json");
        try
        {
            var futureState = new Campaign(new Random(13579)).ToState() with { Version = CampaignFile.CurrentVersion + 1 };
            File.WriteAllText(futureVersionPath, JsonSerializer.Serialize(futureState));
            var replacement = CampaignFile.LoadOrCreate(futureVersionPath);
            Assert(replacement.Regional.DiceRolls.Count == RegionalMap.DiceCount, "An unsupported save version must be replaced with a valid campaign after quarantine.");
            var quarantined = QuarantinedSaves(futureVersionPath);
            Assert(quarantined.Count == 1 && File.Exists(quarantined[0] + ".reason.txt"), "An unsupported save version must be quarantined with a reason file.");
        }
        finally
        {
            DeleteSaveArtifacts(futureVersionPath);
        }
    }

    private static IReadOnlyList<string> QuarantinedSaves(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        var fileName = Path.GetFileName(path);
        return Directory.EnumerateFiles(directory, $"{fileName}.invalid-*")
            .Where(candidate => !candidate.EndsWith(".reason.txt", StringComparison.Ordinal))
            .Order()
            .ToList();
    }

    private static void DeleteSaveArtifacts(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        foreach (var quarantine in QuarantinedSaves(path))
        {
            File.Delete(quarantine);
            if (File.Exists(quarantine + ".reason.txt"))
            {
                File.Delete(quarantine + ".reason.txt");
            }
        }
    }
}
