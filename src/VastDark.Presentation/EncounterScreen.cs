using Godot;
using VastDark.Domain;

namespace VastDark.Presentation;

public sealed partial class EncounterScreen : Control
{
    private readonly Label _title = new();
    private readonly Label _details = new();

    public EncounterScreen()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        var shade = new ColorRect { Color = new Color(0.02f, 0.03f, 0.05f, 0.78f) };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(shade);
        var panel = new VBoxContainer { CustomMinimumSize = new Vector2(520, 0) };
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.AddThemeConstantOverride("separation", 14);
        AddChild(panel);
        _title.AddThemeFontSizeOverride("font_size", 24);
        panel.AddChild(_title);
        _details.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        panel.AddChild(_details);
        var continueButton = new Button { Text = "Continue" };
        continueButton.Pressed += () => Visible = false;
        panel.AddChild(continueButton);
    }

    public void Present(TravelInterruption interruption)
    {
        _title.Text = interruption.Title;
        _details.Text = interruption.Kind switch
        {
            TravelInterruptionKind.RoamingHazard => RoamingHazardRules.Get(interruption.HazardDieRoll!.Value).Procedure,
            TravelInterruptionKind.Ruins => "Travel stops at the Ruins. Enter the generated Ruin graph to explore rooms, encounters, treasure, and depth.",
            TravelInterruptionKind.Settlement => "Travel stops at the Settlement. Resolve rest, resupply, services, factions, and trade before continuing.",
            _ => "Travel stops here.",
        };
        Visible = true;
        MoveToFront();
    }

    public void Present(TravelInterruptionResolution resolution)
    {
        _title.Text = resolution.Title;
        _details.Text = resolution.Summary;
        Visible = true;
        MoveToFront();
    }
}
