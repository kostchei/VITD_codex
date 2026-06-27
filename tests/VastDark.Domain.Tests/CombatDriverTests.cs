using VastDark.Domain;
using static TestKit;

internal static class CombatDriverTests
{
    public static void Run()
    {
        InitiativeOrdersBothSidesTogether();
        PartyAttackCanWinTheFight();
        EnemyTurnSpendsTravelerGrit();
        HalvedEnemiesCheckMoraleAndFlee();
        WarbandEncounterAssemblesTheRoster();
    }

    private static readonly Weapon Sword = new("Sword", "1d6");
    private static readonly Weapon Claw = new("Claw", "1d4");

    private static Traveler Hero(int dex = 10, Vitality? vitality = null) =>
        new("Hero", abilityScores: new AbilityScores(10, dex, 12, 10, 10, 10), vitality: vitality);

    private static void InitiativeOrdersBothSidesTogether()
    {
        var hero = Combatant.ForTraveler(Hero(dex: 14)); // DEX +2
        var goon = Combatant.ForMonster("Goon", new Monster("Goon", 4, 10), new EnemyAttack(0, Claw));
        // Initiative rolls in list order: Hero 10(+2)=12, Goon 15(+0)=15 -> Goon acts first.
        var combat = new CombatEncounter([hero, goon], new ScriptedRandom(10, 15));
        Assert(combat.InitiativeOrder[0].Name == "Goon" && combat.InitiativeOrder[1].Name == "Hero", "Initiative interleaves both sides, highest first.");
        Assert(combat.CurrentActor.Name == "Goon", "The highest initiative acts first.");
    }

    private static void PartyAttackCanWinTheFight()
    {
        var hero = Combatant.ForTraveler(Hero());
        var goon = Combatant.ForMonster("Goon", new Monster("Goon", 4, 10), new EnemyAttack(0, Claw));
        // Init: Hero 18, Goon 1 -> Hero first. Attack: 12 hits AC 10, 1d6 = 4 kills the 4-HP Goon.
        var combat = new CombatEncounter([hero, goon], new ScriptedRandom(18, 1));
        var result = combat.PartyAttack(goon, Sword, new ScriptedRandom(12, 4));
        Assert(result is { Hit: true, Damage: 4 }, "The Traveler's sword hits for 4.");
        Assert(combat.Outcome == CombatOutcome.PartyVictory, "Dropping the last enemy wins the fight.");
    }

    private static void EnemyTurnSpendsTravelerGrit()
    {
        var hero = Combatant.ForTraveler(Hero(vitality: new Vitality(5, 4)));
        var goon = Combatant.ForMonster("Goon", new Monster("Goon", 10, 10), new EnemyAttack(0, Claw));
        // Init: Hero 1, Goon 10 -> Goon first. Claw: 12 hits AC 10, 1d4 = 3 damage.
        var combat = new CombatEncounter([hero, goon], new ScriptedRandom(1, 10));
        var result = combat.EnemyTurn(new ScriptedRandom(12, 3));
        Assert(result.Hit && hero.Traveler!.Vitality!.Grit == 2, "An enemy hit spends the Traveler's Grit (5 - 3).");
        Assert(combat is { Outcome: CombatOutcome.InProgress } && combat.CurrentActor.Name == "Hero", "Play passes to the Traveler.");
    }

    private static void HalvedEnemiesCheckMoraleAndFlee()
    {
        var hero = Combatant.ForTraveler(Hero());
        var goonA = Combatant.ForMonster("Goon A", new Monster("Goon A", 3, 10), new EnemyAttack(0, Claw));
        var goonB = Combatant.ForMonster("Goon B", new Monster("Goon B", 3, 10), new EnemyAttack(0, Claw));
        // Init: Hero 18, both goons 1 -> Hero first. Kill Goon A (12 hits, 1d6 = 5) -> 1 of 2 left -> morale.
        var combat = new CombatEncounter([hero, goonA, goonB], new ScriptedRandom(18, 1, 1));
        combat.PartyAttack(goonA, Sword, new ScriptedRandom(12, 5, 5)); // attack d20=12, dmg=5, morale d20=5 (<15)
        Assert(combat.Outcome == CombatOutcome.EnemiesFled, "Reducing the enemies to half forces a morale break.");
    }

    private static void WarbandEncounterAssemblesTheRoster()
    {
        var warband = WarbandRules.Compose(2); // Demagogue + 2 Cutthroats
        // Initiative for 4 combatants (Hero, Demagogue, Cutthroat 1, Cutthroat 2).
        var combat = CombatEncounter.ForWarband([Hero()], warband, new ScriptedRandom(1, 2, 3, 4));
        Assert(combat.Party.Count() == 1, "The party side holds the Travelers.");
        Assert(combat.Enemies.Count() == 3 && combat.Enemies.Any(enemy => enemy.Name == "Demagogue"), "The enemy side is the Demagogue plus the Cutthroats.");
    }
}
