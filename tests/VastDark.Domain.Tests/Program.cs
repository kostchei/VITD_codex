var tests = new (string Name, Action Run)[]
{
    ("Map generation", MapGenerationTests.Run),
    ("Traveler rules", TravelerRuleTests.Run),
    ("Rule tables", RuleTableTests.Run),
    ("Encounter resolver", EncounterResolverTests.Run),
    ("Travel and campaign", TravelCampaignTests.Run),
    ("Persistence", PersistenceTests.Run),
};

foreach (var (name, run) in tests)
{
    run();
    Console.WriteLine($"PASS {name}");
}

Console.WriteLine("All VastDark domain checks passed.");
