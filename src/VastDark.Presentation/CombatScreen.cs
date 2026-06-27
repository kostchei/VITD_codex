using Godot;
using VastDark.Domain;

namespace VastDark.Presentation;

/// <summary>
/// Interactive combat view backed by the domain <see cref="CombatEncounter"/>. Shows the initiative
/// order, Grit/Flesh bars for Travelers and HP bars for enemies, and drives turns: the active Traveler
/// gets an attack button per living enemy, and enemy turns resolve via the "Enemy acts" button.
/// </summary>
public sealed partial class CombatScreen : Control
{
    private static readonly Weapon PartyWeapon = new("Sword", "1d6");

    private readonly IRandomSource _random = new SystemRandomSource();
    private readonly Label _title = new();
    private readonly Label _initiative = new();
    private readonly VBoxContainer _partyPanel = new();
    private readonly VBoxContainer _enemyPanel = new();
    private readonly VBoxContainer _actions = new();
    private readonly Label _log = new();
    private CombatEncounter? _encounter;

    public CombatScreen()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var shade = new ColorRect { Color = new Color(0.02f, 0.03f, 0.05f, 0.85f) };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(shade);

        var panel = new VBoxContainer { CustomMinimumSize = new Vector2(720, 0) };
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.AddThemeConstantOverride("separation", 12);
        AddChild(panel);

        _title.AddThemeFontSizeOverride("font_size", 24);
        panel.AddChild(_title);

        _initiative.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        panel.AddChild(_initiative);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 24);
        panel.AddChild(columns);

        columns.AddChild(MakeColumn("Party", _partyPanel));
        columns.AddChild(MakeColumn("Enemies", _enemyPanel));

        _actions.AddThemeConstantOverride("separation", 6);
        panel.AddChild(_actions);

        var logTitle = new Label { Text = "Combat log" };
        logTitle.AddThemeFontSizeOverride("font_size", 16);
        panel.AddChild(logTitle);
        _log.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _log.CustomMinimumSize = new Vector2(0, 120);
        panel.AddChild(_log);
    }

    public void Begin(CombatEncounter encounter)
    {
        _encounter = encounter;
        Visible = true;
        MoveToFront();
        Refresh();
    }

    private static VBoxContainer MakeColumn(string heading, Control body)
    {
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 6);
        var label = new Label { Text = heading };
        label.AddThemeFontSizeOverride("font_size", 16);
        column.AddChild(label);
        column.AddChild(body);
        return column;
    }

    private void Refresh()
    {
        if (_encounter is not { } encounter) return;

        _title.Text = $"Combat — {encounter.Outcome}";
        _initiative.Text = "Initiative: " + string.Join("  >  ", encounter.InitiativeOrder.Select(combatant =>
        {
            var marker = ReferenceEquals(combatant, encounter.CurrentActor) && encounter.Outcome == CombatOutcome.InProgress ? "▶ " : string.Empty;
            var down = combatant.IsAlive ? string.Empty : " (down)";
            return $"{marker}{combatant.Name}{down}";
        }));

        RebuildCombatantRows(_partyPanel, encounter.Party);
        RebuildCombatantRows(_enemyPanel, encounter.Enemies);
        RebuildActions(encounter);

        _log.Text = encounter.Log.Count == 0
            ? "The fight begins."
            : string.Join("\n", encounter.Log.TakeLast(6));
    }

    private static void RebuildCombatantRows(VBoxContainer panel, IEnumerable<Combatant> combatants)
    {
        Clear(panel);
        foreach (var combatant in combatants)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var name = new Label { Text = combatant.Name, CustomMinimumSize = new Vector2(150, 0) };
            if (!combatant.IsAlive) name.Modulate = new Color(0.5f, 0.5f, 0.5f);
            row.AddChild(name);

            if (combatant.Traveler is { } traveler)
            {
                if (traveler.Vitality is { } vitality)
                {
                    row.AddChild(MakeBar("Grit", vitality.Grit, System.Math.Max(vitality.Grit, 1), new Color(0.4f, 0.7f, 1f)));
                    row.AddChild(MakeBar("Flesh", vitality.Flesh, System.Math.Max(vitality.Flesh, 1), new Color(0.9f, 0.4f, 0.4f)));
                }
                else
                {
                    row.AddChild(MakeBar("HP", traveler.Health, System.Math.Max(traveler.Health, 1), new Color(0.4f, 0.7f, 1f)));
                }
            }
            else if (combatant.Monster is { } monster)
            {
                row.AddChild(MakeBar("HP", monster.HitPoints.Current, monster.HitPoints.Maximum, new Color(0.9f, 0.5f, 0.3f)));
                row.AddChild(new Label { Text = $"AC {monster.ArmorClass}" });
            }

            panel.AddChild(row);
        }
    }

    private static Control MakeBar(string label, int value, int maximum, Color color)
    {
        var container = new HBoxContainer();
        container.AddThemeConstantOverride("separation", 4);
        var bar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(90, 16),
            MinValue = 0,
            MaxValue = maximum,
            Value = value,
            ShowPercentage = false,
        };
        bar.AddThemeColorOverride("font_color", color);
        container.AddChild(bar);
        container.AddChild(new Label { Text = $"{label} {value}" });
        return container;
    }

    private void RebuildActions(CombatEncounter encounter)
    {
        Clear(_actions);

        if (encounter.Outcome != CombatOutcome.InProgress)
        {
            var close = new Button { Text = "Close" };
            close.Pressed += () => Visible = false;
            _actions.AddChild(close);
            return;
        }

        var current = encounter.CurrentActor;
        if (current.Side == CombatSide.Party)
        {
            _actions.AddChild(new Label { Text = $"{current.Name}'s turn — attack with {PartyWeapon.Name}:" });
            foreach (var enemy in encounter.LivingEnemies)
            {
                var target = enemy;
                var button = new Button { Text = $"Strike {target.Name}" };
                button.Pressed += () => ResolvePartyAttack(target);
                _actions.AddChild(button);
            }
        }
        else
        {
            _actions.AddChild(new Label { Text = $"{current.Name}'s turn (enemy)." });
            var act = new Button { Text = "Enemy acts" };
            act.Pressed += ResolveEnemyTurn;
            _actions.AddChild(act);
        }
    }

    private void ResolvePartyAttack(Combatant target)
    {
        if (_encounter is not { } encounter || encounter.Outcome != CombatOutcome.InProgress) return;
        encounter.PartyAttack(target, PartyWeapon, _random);
        Refresh();
    }

    private void ResolveEnemyTurn()
    {
        if (_encounter is not { } encounter || encounter.Outcome != CombatOutcome.InProgress) return;
        encounter.EnemyTurn(_random);
        Refresh();
    }

    private static void Clear(Node container)
    {
        while (container.GetChildCount() > 0)
        {
            var child = container.GetChild(0);
            container.RemoveChild(child);
            child.QueueFree();
        }
    }
}
