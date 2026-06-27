namespace VastDark.Domain;

public enum CombatSide { Party, Enemies }

public enum CombatOutcome { InProgress, PartyVictory, PartyDefeat, EnemiesFled }

/// <summary>An enemy's attack: its to-hit modifier and the weapon it swings.</summary>
public sealed record EnemyAttack(int Modifier, Weapon Weapon);

/// <summary>
/// A combatant in a fight: either a Party Traveler (Grit/Flesh, player-directed) or an enemy Monster
/// (HP, AI-directed) carrying an attack profile.
/// </summary>
public sealed class Combatant
{
    private Combatant(string id, CombatSide side, int dexterityModifier, Traveler? traveler, Monster? monster, EnemyAttack? attack)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        Side = side;
        DexterityModifier = dexterityModifier;
        Traveler = traveler;
        Monster = monster;
        Attack = attack;
    }

    public string Id { get; }
    public CombatSide Side { get; }
    public int DexterityModifier { get; }
    public Traveler? Traveler { get; }
    public Monster? Monster { get; }
    public EnemyAttack? Attack { get; }
    public int InitiativeTotal { get; internal set; }

    public string Name => Traveler?.Name ?? Monster!.Name;
    public bool IsAlive => Traveler is { } traveler ? !traveler.IsDefeated : !Monster!.IsDead;
    public int ArmorClass => Traveler?.ArmorClass ?? Monster!.ArmorClass;

    public static Combatant ForTraveler(Traveler traveler)
    {
        ArgumentNullException.ThrowIfNull(traveler);
        return new Combatant(traveler.Name, CombatSide.Party, traveler.GetAbilityModifier(Ability.Dexterity), traveler, null, null);
    }

    public static Combatant ForMonster(string id, Monster monster, EnemyAttack attack, int dexterityModifier = 0)
    {
        ArgumentNullException.ThrowIfNull(monster);
        ArgumentNullException.ThrowIfNull(attack);
        return new Combatant(id, CombatSide.Enemies, dexterityModifier, null, monster, attack);
    }
}

/// <summary>
/// Drives a Shadowdark fight: rolls initiative for every combatant, steps turns highest-first, resolves
/// attacks (player-chosen for Travelers, first-living-target AI for enemies), checks enemy morale once
/// the group is halved, and reports the outcome. Pure domain — a UI calls the same methods a test does.
/// </summary>
public sealed class CombatEncounter
{
    private readonly List<Combatant> _order;
    private readonly List<string> _log = [];
    private readonly int _originalEnemyCount;
    private readonly int _leaderWisdomModifier;
    private int _turnIndex;
    private bool _moraleBroken;
    private bool _moraleChecked;

    public CombatEncounter(IReadOnlyList<Combatant> combatants, IRandomSource random, int leaderWisdomModifier = 0)
    {
        ArgumentNullException.ThrowIfNull(combatants);
        ArgumentNullException.ThrowIfNull(random);
        if (combatants.Count == 0) throw new ArgumentException("A combat needs at least one combatant.", nameof(combatants));

        foreach (var combatant in combatants)
        {
            combatant.InitiativeTotal = InitiativeRules.Roll(combatant.DexterityModifier, random);
        }

        _order = combatants
            .OrderByDescending(combatant => combatant.InitiativeTotal)
            .ThenByDescending(combatant => combatant.DexterityModifier)
            .ThenBy(combatant => combatant.Id, StringComparer.Ordinal)
            .ToList();
        _originalEnemyCount = _order.Count(combatant => combatant.Side == CombatSide.Enemies);
        _leaderWisdomModifier = leaderWisdomModifier;
        _turnIndex = 0;
        if (!CurrentActor.IsAlive) AdvanceToNextLivingActor();
    }

    public IReadOnlyList<Combatant> InitiativeOrder => _order;
    public IReadOnlyList<string> Log => _log;
    public CombatOutcome Outcome { get; private set; } = CombatOutcome.InProgress;
    public Combatant CurrentActor => _order[_turnIndex];
    public IEnumerable<Combatant> Party => _order.Where(combatant => combatant.Side == CombatSide.Party);
    public IEnumerable<Combatant> Enemies => _order.Where(combatant => combatant.Side == CombatSide.Enemies);
    public IEnumerable<Combatant> LivingEnemies => Enemies.Where(combatant => combatant.IsAlive);
    public IEnumerable<Combatant> LivingParty => Party.Where(combatant => combatant.IsAlive);

    /// <summary>The current Traveler attacks a chosen enemy with the given weapon.</summary>
    public AttackResult PartyAttack(Combatant target, Weapon weapon, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(weapon);
        ArgumentNullException.ThrowIfNull(random);
        EnsureInProgress();
        if (CurrentActor.Traveler is not { } attacker) throw new InvalidOperationException("It is not a Traveler's turn.");
        if (target.Side != CombatSide.Enemies || target.Monster is not { } enemy) throw new InvalidOperationException("Travelers attack enemies.");
        if (!target.IsAlive) throw new InvalidOperationException("That enemy is already down.");

        var result = AttackResolver.Strike(enemy, attacker.AttackModifierFor(weapon), weapon, attacker.DamageModifierFor(weapon), random);
        LogAttack(attacker.Name, target.Name, weapon.Name, result);
        EndTurn(random);
        return result;
    }

    /// <summary>Resolves the current enemy's turn: it attacks the first living Traveler in initiative order.</summary>
    public AttackResult EnemyTurn(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        EnsureInProgress();
        var attacker = CurrentActor;
        if (attacker.Side != CombatSide.Enemies || attacker.Attack is not { } attack) throw new InvalidOperationException("It is not an enemy's turn.");
        var target = LivingParty.FirstOrDefault() ?? throw new InvalidOperationException("No living Traveler to target.");

        var result = AttackResolver.Strike(target.Traveler!, attack.Modifier, attack.Weapon, abilityDamageModifier: 0, random);
        LogAttack(attacker.Name, target.Name, attack.Weapon.Name, result);
        EndTurn(random);
        return result;
    }

    public static CombatEncounter ForWarband(IEnumerable<Traveler> party, Warband warband, IRandomSource random, int leaderWisdomModifier = 0)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(warband);
        var combatants = party.Select(Combatant.ForTraveler).ToList();
        combatants.Add(Combatant.ForMonster("Demagogue", warband.Demagogue, new EnemyAttack(0, new Weapon(DemagogueRules.StatBlock.Attack, DemagogueRules.StatBlock.DamageDice))));
        var index = 1;
        foreach (var cutthroat in warband.Cutthroats)
        {
            combatants.Add(Combatant.ForMonster($"Cutthroat {index++}", cutthroat, new EnemyAttack(0, CutthroatRules.ViciousDagger)));
        }

        return new CombatEncounter(combatants, random, leaderWisdomModifier);
    }

    private void EndTurn(IRandomSource random)
    {
        CheckEnemyMorale(random);
        if (Outcome != CombatOutcome.InProgress) return;
        Outcome = DetermineOutcome();
        if (Outcome != CombatOutcome.InProgress) return;
        AdvanceToNextLivingActor();
    }

    private void AdvanceToNextLivingActor()
    {
        for (var step = 0; step < _order.Count; step++)
        {
            _turnIndex = (_turnIndex + 1) % _order.Count;
            if (_order[_turnIndex].IsAlive) return;
        }
    }

    private void CheckEnemyMorale(IRandomSource random)
    {
        if (_moraleChecked || _moraleBroken) return;
        var living = LivingEnemies.Count();
        if (living == 0) return;

        var mustCheck = _originalEnemyCount > 1
            ? MoraleRules.GroupMustCheck(_originalEnemyCount, living)
            : Enemies.Any(enemy => MoraleRules.SoloMustCheck(enemy.Monster!));
        if (!mustCheck) return;

        _moraleChecked = true;
        var morale = MoraleRules.Check(_leaderWisdomModifier, random);
        _log.Add(morale.Holds ? "The enemies hold their nerve." : "The enemies break and flee.");
        if (morale.Flees)
        {
            _moraleBroken = true;
            Outcome = CombatOutcome.EnemiesFled;
        }
    }

    private CombatOutcome DetermineOutcome()
    {
        if (!LivingParty.Any()) return CombatOutcome.PartyDefeat;
        if (!LivingEnemies.Any()) return CombatOutcome.PartyVictory;
        return CombatOutcome.InProgress;
    }

    private void EnsureInProgress()
    {
        if (Outcome != CombatOutcome.InProgress) throw new InvalidOperationException($"The combat is over: {Outcome}.");
    }

    private void LogAttack(string attacker, string target, string weapon, AttackResult result)
    {
        var headline = result.Hit
            ? $"{attacker} hits {target} with {weapon} for {result.Damage}{(result.Critical ? " (critical)" : string.Empty)}."
            : $"{attacker} misses {target} with {weapon}.";
        if (result.WeaponBroke) headline += $" {weapon} shatters.";
        _log.Add(headline);
    }
}
