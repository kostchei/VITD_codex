using Godot;
using VastDark.Domain;

namespace VastDark.Presentation;

public partial class MapScreen : Control
{
    private const int NewRegionalMapMenuId = 1;
    private readonly string _campaignPath;
    private MapNavigationService _navigation;
    private readonly Label _locationLabel = new();
    private readonly Label _travelLabel = new();
    private readonly Label _inspector = new();
    private readonly Label _partyDetails = new();
    private readonly Label _travelLog = new();
    private readonly Button _regionalButton = new() { Text = "Regional" };
    private readonly Button _localButton = new() { Text = "Local" };
    private readonly Button _dungeonButton = new() { Text = "Dungeon" };
    private readonly Button _movePartyButton = new() { Text = "Move party here" };
    private readonly Button _forcedMarchButton = new() { Text = "Forced march (+6 miles)" };
    private readonly Button _restButton = new() { Text = "Rest" };
    private readonly Button _recentrePartyButton = new() { Text = "Recentre on party" };
    private readonly Button _moveRuinButton = new() { Text = "Move room" };
    private readonly Button _searchRuinButton = new() { Text = "Search room" };
    private readonly Button _descendRuinButton = new() { Text = "Descend" };
    private readonly AcceptDialog _resolutionDialog = new() { Title = "Resolution" };
    private readonly MenuButton _campaignMenu = new() { Text = "Campaign" };
    private readonly ConfirmationDialog _newCampaignConfirmation = new()
    {
        Title = "Start a new regional map?",
        DialogText = "This replaces the saved regional and local maps.",
    };
    private readonly List<Button> _depthButtons = [];
    private MapCanvas? _mapCanvas;
    private bool _animatingParty;

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
        header.AddChild(_campaignMenu);

        _locationLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _locationLabel.HorizontalAlignment = HorizontalAlignment.Right;
        header.AddChild(_locationLabel);

        var travelRow = new HBoxContainer();
        travelRow.AddThemeConstantOverride("separation", 8);
        _travelLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        travelRow.AddChild(_travelLabel);
        travelRow.AddChild(_movePartyButton);
        travelRow.AddChild(_forcedMarchButton);
        travelRow.AddChild(_restButton);
        travelRow.AddChild(_recentrePartyButton);
        travelRow.AddChild(_moveRuinButton);
        travelRow.AddChild(_searchRuinButton);
        travelRow.AddChild(_descendRuinButton);
        root.AddChild(travelRow);

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
        _mapCanvas.PartyPathRequested += AnimatePartyAlongPath;
        _mapCanvas.RegionalPathRequested += AnimateRegionalPath;
        content.AddChild(_mapCanvas);

        var sidebar = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(260, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        content.AddChild(sidebar);

        var inspectorPanel = new PanelContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _inspector.Text = "Select a cell to inspect it.";
        _inspector.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _inspector.AddThemeConstantOverride("margin_left", 12);
        _inspector.AddThemeConstantOverride("margin_right", 12);
        _inspector.AddThemeConstantOverride("margin_top", 12);
        inspectorPanel.AddChild(_inspector);
        sidebar.AddChild(inspectorPanel);

        var partyTitle = new Label { Text = "Party status" };
        partyTitle.AddThemeFontSizeOverride("font_size", 16);
        sidebar.AddChild(partyTitle);
        _partyDetails.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        sidebar.AddChild(_partyDetails);

        var logTitle = new Label { Text = "Recent travel" };
        logTitle.AddThemeFontSizeOverride("font_size", 16);
        sidebar.AddChild(logTitle);
        _travelLog.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _travelLog.CustomMinimumSize = new Vector2(0, 150);
        sidebar.AddChild(_travelLog);

        _regionalButton.Pressed += ShowRegional;
        _localButton.Pressed += ShowLocal;
        _dungeonButton.Pressed += ShowDungeon;
        _movePartyButton.Pressed += MovePartyToSelectedHex;
        _forcedMarchButton.Pressed += BeginForcedMarch;
        _restButton.Pressed += RestParty;
        _recentrePartyButton.Pressed += RecentreOnParty;
        _moveRuinButton.Pressed += MoveRuinRoom;
        _searchRuinButton.Pressed += SearchRuinRoom;
        _descendRuinButton.Pressed += DescendRuin;
        _campaignMenu.GetPopup().AddItem("New regional map", NewRegionalMapMenuId);
        _campaignMenu.GetPopup().IdPressed += ShowCampaignMenuAction;
        _newCampaignConfirmation.Confirmed += StartNewRegionalMap;
        AddChild(_newCampaignConfirmation);
        AddChild(_resolutionDialog);
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

    private void MovePartyToSelectedHex()
    {
        if (_mapCanvas?.PreviewLocalPath.Count > 1)
        {
            AnimatePartyAlongPath(_mapCanvas.PreviewLocalPath);
            return;
        }
        if (_navigation.Current is not MapLocation.Local local || _mapCanvas?.SelectedLocalCoordinate is not { } target)
        {
            _inspector.Text = "Open the party's local map and select an adjacent local hex first.";
            return;
        }

        var result = _navigation.Campaign.TryMoveParty(target.RegionalCoordinate, target.LocalCoordinate);
        if (result.Moved)
        {
            if (_navigation.Campaign.PartyTravel.RegionalCoordinate != local.RegionalCoordinate)
            {
                _navigation.EnterLocal(_navigation.Campaign.PartyTravel.RegionalCoordinate);
            }

            CampaignFile.Save(_navigation.Campaign, _campaignPath);
        }

        _inspector.Text = result.Message;
        RefreshUi();
    }

    private void RecentreOnParty()
    {
        _navigation.EnterLocal(_navigation.Campaign.PartyTravel.RegionalCoordinate);
        RefreshUi();
        _mapCanvas?.RecentreOnParty();
    }

    private void BeginForcedMarch()
    {
        var result = _navigation.Campaign.TryBeginForcedMarch();
        if (result.Moved)
        {
            CampaignFile.Save(_navigation.Campaign, _campaignPath);
        }

        _inspector.Text = result.Message;
        RefreshUi();
    }

    private void RestParty()
    {
        var result = _navigation.Campaign.TryRestParty();
        if (result.Moved)
        {
            CampaignFile.Save(_navigation.Campaign, _campaignPath);
        }

        _inspector.Text = result.Message;
        RefreshUi();
    }

    private async void AnimatePartyAlongPath(IReadOnlyList<LocalMapCoord> path)
    {
        if (_animatingParty || path.Count < 2) return;
        _animatingParty = true;
        try
        {
            for (var index = 1; index < path.Count; index++)
            {
                var step = path[index];
                var result = _navigation.Campaign.TryMoveParty(step.RegionalCoordinate, step.LocalCoordinate);
                _inspector.Text = result.Message;
                _mapCanvas?.Refresh();
                RefreshUi();
                if (!result.Moved) break;
                await ToSignal(GetTree().CreateTimer(0.14f), SceneTreeTimer.SignalName.Timeout);
            }
            CampaignFile.Save(_navigation.Campaign, _campaignPath);
        }
        finally
        {
            _animatingParty = false;
            RefreshUi();
        }
    }

    private async void AnimateRegionalPath(IReadOnlyList<RegionalCoord> path)
    {
        if (_animatingParty || path.Count < 2) return;
        _animatingParty = true;
        try
        {
            for (var index = 1; index < path.Count; index++)
            {
                var result = _navigation.Campaign.TryTravelRegionalStep(path[index]);
                _inspector.Text = result.Message;
                RefreshUi();
                if (!result.Moved) break;
                await ToSignal(GetTree().CreateTimer(0.45f), SceneTreeTimer.SignalName.Timeout);
            }
            CampaignFile.Save(_navigation.Campaign, _campaignPath);
        }
        finally
        {
            _animatingParty = false;
            RefreshUi();
        }
    }

    private void MoveRuinRoom()
    {
        if (_navigation.Current is not MapLocation.Dungeon || _mapCanvas?.SelectedRuinRoom is not { } target)
        {
            _inspector.Text = "Select a connected Ruin room first.";
            return;
        }
        if (_navigation.Campaign.TryMoveRuinRoom(target))
        {
            ShowResolution($"Moved to Ruin room {target}. 10 minutes pass. Resolve its feature, encounter, treasure, or referee choice from the inspector.");
            CampaignFile.Save(_navigation.Campaign, _campaignPath);
        }
        else
        {
            _inspector.Text = "That room is not connected to the current Ruin room.";
        }
        RefreshUi();
    }

    private void SearchRuinRoom()
    {
        if (_navigation.Current is not MapLocation.Dungeon) return;
        _navigation.Campaign.SearchRuinRoom();
        ShowResolution($"Searched Ruin room {_navigation.Campaign.Ruin.CurrentRoom}. 30 minutes pass; room effects and choices are shown in the inspector.");
        CampaignFile.Save(_navigation.Campaign, _campaignPath);
        RefreshUi();
    }

    private void DescendRuin()
    {
        if (_navigation.Current is not MapLocation.Dungeon) return;
        _navigation.Campaign.DescendRuin();
        _navigation.SetDungeonDepth(_navigation.Campaign.Ruin.Depth);
        ShowResolution($"Descended to Ruin depth {_navigation.Campaign.Ruin.Depth}. A new generated room graph is active.");
        CampaignFile.Save(_navigation.Campaign, _campaignPath);
        RefreshUi();
    }

    private void ShowResolution(string text)
    {
        _resolutionDialog.DialogText = text;
        _resolutionDialog.PopupCentered();
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
                                  !_navigation.Campaign.HasDungeonEntrance(currentLocal.RegionalCoordinate) ||
                                  !_navigation.Campaign.IsPartyAtDungeonEntrance;
        var partyTravel = _navigation.Campaign.PartyTravel;
        var viewingPartyLocalMap = current is MapLocation.Local partyLocal && partyLocal.RegionalCoordinate == partyTravel.RegionalCoordinate;
        _movePartyButton.Disabled = !viewingPartyLocalMap || _mapCanvas?.PreviewLocalPath.Count is not > 1 || partyTravel.RestRequired || _animatingParty;
        _forcedMarchButton.Disabled = !partyTravel.CanForcedMarch;
        _restButton.Disabled = false;
        var inRuin = current is MapLocation.Dungeon;
        _moveRuinButton.Disabled = !inRuin;
        _searchRuinButton.Disabled = !inRuin;
        _descendRuinButton.Disabled = !inRuin;

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
        var party = _navigation.Campaign.Party;
        _travelLabel.Text = $"Party: {partyTravel.LocalCoordinate} in {partyTravel.RegionalCoordinate} | Day {partyTravel.Day} | {partyTravel.DailyMiles} / {partyTravel.DailyMileLimit} miles | Rations {party.TotalRations} | Exhaustion {party.TotalExhaustion}";
        _partyDetails.Text = string.Join("\n\n", party.Members.Select(member =>
        {
            var conditions = member.Conditions.Count == 0 ? "none" : string.Join(", ", member.Conditions.Order());
            var resources = member.Resources.Count == 0
                ? "none"
                : string.Join(", ", member.Resources.Where(resource => resource.Value > 0).OrderBy(resource => resource.Key).Select(resource => $"{resource.Key} {resource.Value}"));
            var scores = string.Join(" ", Enum.GetValues<Ability>().Select(ability => $"{ability.ToString()[0..3].ToUpperInvariant()} {member.GetAbilityScore(ability):00} ({member.GetAbilityModifier(ability):+0;-0;0})"));
            var vitality = member.Vitality is { } value ? $"Grit {value.Grit} | Flesh {value.Flesh}" : $"HP {member.Health}";
            var memories = member.Harrowing is null ? "unassigned" : string.Join("; ", member.Harrowing.RemainingMemories);
            var factions = string.Join(", ", member.WastesFactions.Select(faction => faction.ToString()).Concat(member.SettlementFactions.Select(faction => faction.ToString())));
            return $"{member.Name}: Level {member.Level} | {vitality} | Rations {member.Rations} | Exhaustion {member.Exhaustion}\n{scores}\nInventory {member.Inventory.UsedSlots}/{member.Inventory.Capacity} slots | Rites {member.Rites.Rites} | Gifts: {(member.Gifts.Gifts.Count == 0 ? "none" : string.Join(", ", member.Gifts.Gifts))}\nMemories: {memories}\nFactions: {(string.IsNullOrEmpty(factions) ? "none" : factions)}\nConditions: {conditions}\nResources: {resources}";
        }));
        _travelLog.Text = _navigation.Campaign.TravelLog.Count == 0
            ? "No travel recorded yet."
            : string.Join("\n", _navigation.Campaign.TravelLog.TakeLast(6).Select(entry => $"Day {entry.Day}: {entry.Message}"));
        _mapCanvas?.Refresh();
    }

    private void SetInspectorText(string text)
    {
        _inspector.Text = text;
        RefreshUi();
    }
}
