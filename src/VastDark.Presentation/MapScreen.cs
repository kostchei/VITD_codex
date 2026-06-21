using Godot;
using VastDark.Domain;

namespace VastDark.Presentation;

public partial class MapScreen : Control
{
    private const int NewRegionalMapMenuId = 1;
    private readonly string _campaignPath;
    private MapNavigationService _navigation;
    private readonly Label _locationLabel = new();
    private readonly Label _inspector = new();
    private readonly Button _regionalButton = new() { Text = "Regional" };
    private readonly Button _localButton = new() { Text = "Local" };
    private readonly Button _dungeonButton = new() { Text = "Dungeon" };
    private readonly Button _advanceHazardsButton = new() { Text = "Advance hazards" };
    private readonly MenuButton _campaignMenu = new() { Text = "Campaign" };
    private readonly ConfirmationDialog _newCampaignConfirmation = new()
    {
        Title = "Start a new regional map?",
        DialogText = "This replaces the saved regional and local maps.",
    };
    private readonly List<Button> _depthButtons = [];
    private MapCanvas? _mapCanvas;

    public MapScreen()
    {
        _campaignPath = ProjectSettings.GlobalizePath("user://vastdark-campaign.json");
        _navigation = new MapNavigationService(CampaignFile.LoadOrCreate(_campaignPath));
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 10);
        AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        root.AddChild(header);

        var title = new Label { Text = "VASTDARK  ·  MAP PROTOTYPE" };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.CustomMinimumSize = new Vector2(315, 0);
        header.AddChild(title);
        header.AddChild(_regionalButton);
        header.AddChild(_localButton);
        header.AddChild(_dungeonButton);
        header.AddChild(_advanceHazardsButton);
        header.AddChild(_campaignMenu);

        _locationLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _locationLabel.HorizontalAlignment = HorizontalAlignment.Right;
        header.AddChild(_locationLabel);

        var depthRow = new HBoxContainer();
        depthRow.AddChild(new Label { Text = "Dungeon depth:" });
        for (var depth = 0; depth < Dungeon.MaximumLevels; depth++)
        {
            var selectedDepth = depth;
            var button = new Button { Text = $"{depth}" };
            button.Pressed += () => ChangeDungeonDepth(selectedDepth);
            _depthButtons.Add(button);
            depthRow.AddChild(button);
        }

        root.AddChild(depthRow);

        var content = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(content);

        _mapCanvas = new MapCanvas(_navigation)
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(800, 600),
        };
        _mapCanvas.CellSelected += SetInspectorText;
        content.AddChild(_mapCanvas);

        var inspectorPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(260, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _inspector.Text = "Select a cell to inspect it.";
        _inspector.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _inspector.AddThemeConstantOverride("margin_left", 12);
        _inspector.AddThemeConstantOverride("margin_right", 12);
        _inspector.AddThemeConstantOverride("margin_top", 12);
        inspectorPanel.AddChild(_inspector);
        content.AddChild(inspectorPanel);

        _regionalButton.Pressed += ShowRegional;
        _localButton.Pressed += ShowLocal;
        _dungeonButton.Pressed += ShowDungeon;
        _advanceHazardsButton.Pressed += AdvanceRoamingHazards;
        _campaignMenu.GetPopup().AddItem("New regional map", NewRegionalMapMenuId);
        _campaignMenu.GetPopup().IdPressed += ShowCampaignMenuAction;
        _newCampaignConfirmation.Confirmed += StartNewRegionalMap;
        AddChild(_newCampaignConfirmation);
        RefreshUi();
    }

    private void ShowRegional()
    {
        _navigation.ReturnToRegional();
        RefreshUi();
    }

    private void ShowLocal()
    {
        switch (_navigation.Current)
        {
            case MapLocation.Regional regional:
                _navigation.EnterLocal(regional.Coordinate);
                CampaignFile.Save(_navigation.Campaign, _campaignPath);
                break;
            case MapLocation.Dungeon:
                _navigation.ReturnToLocal();
                break;
        }

        RefreshUi();
    }

    private void ShowDungeon()
    {
        if (_navigation.TryEnterDungeon())
        {
            RefreshUi();
        }
    }

    private void ChangeDungeonDepth(int depth)
    {
        if (_navigation.Current is MapLocation.Dungeon)
        {
            _navigation.SetDungeonDepth(depth);
            RefreshUi();
        }
    }

    private void AdvanceRoamingHazards()
    {
        if (_navigation.Current is not MapLocation.Local local)
        {
            return;
        }

        var map = _navigation.Campaign.GetLocalMap(local.RegionalCoordinate);
        map.AdvanceRoamingHazards();
        CampaignFile.Save(_navigation.Campaign, _campaignPath);
        _inspector.Text = $"Roaming hazards advanced to day {map.RoamingHazardDay}.";
        RefreshUi();
    }

    private void ShowCampaignMenuAction(long id)
    {
        if (id == NewRegionalMapMenuId)
        {
            _newCampaignConfirmation.PopupCentered();
        }
    }

    private void StartNewRegionalMap()
    {
        _navigation = new MapNavigationService(new Campaign());
        CampaignFile.Save(_navigation.Campaign, _campaignPath);
        _mapCanvas?.SetNavigation(_navigation);
        _inspector.Text = "New regional map generated and saved.";
        RefreshUi();
    }

    private void RefreshUi()
    {
        var current = _navigation.Current;
        _localButton.Disabled = current is MapLocation.Local;
        _dungeonButton.Disabled = current is not MapLocation.Local currentLocal ||
                                  !_navigation.Campaign.HasDungeonEntrance(currentLocal.RegionalCoordinate);
        _advanceHazardsButton.Disabled = current is not MapLocation.Local;

        foreach (var button in _depthButtons)
        {
            button.Disabled = current is not MapLocation.Dungeon;
        }

        _locationLabel.Text = current switch
        {
            MapLocation.Regional regional => $"Regional cell {regional.Coordinate}",
            MapLocation.Local local => $"Local map for regional cell {local.RegionalCoordinate}",
            MapLocation.Dungeon dungeon => $"Dungeon at {dungeon.RegionalCoordinate}, depth {dungeon.Depth}",
            _ => string.Empty,
        };
        _mapCanvas?.Refresh();
    }

    private void SetInspectorText(string text)
    {
        _inspector.Text = text;
    }
}
