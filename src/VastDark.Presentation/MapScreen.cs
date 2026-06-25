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
    private readonly Label _partySummary = new();
    private readonly Label _partyDetails = new();
    private readonly Label _travelLog = new();
    private readonly Label _contextTitle = new();
    private readonly VBoxContainer _contextActions = new();
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
    private readonly Button _buyRationButton = new() { Text = $"Buy ration ({Campaign.RationCoinCost} coins)" };
    private readonly Button _refineLodestoneButton = new() { Text = "Refine Raw Lodestone" };
    private readonly Button _gatherLodestoneButton = new() { Text = "Gather Lodestone (1h)" };
    private readonly Button _mineLodestoneButton = new() { Text = "Mine Lodestone (1h, tools)" };
    private readonly Button _enterPillarButton = new() { Text = "Delve into Pillar tunnels" };
    private readonly Button _goDeeperPillarButton = new() { Text = "Go deeper (10 min)" };
    private readonly Button _searchPillarButton = new() { Text = "Search tunnel (30 min)" };
    private readonly Button _exitPillarButton = new() { Text = "Exit Pillar delve" };
    private readonly AcceptDialog _resolutionDialog = new() { Title = "Resolution" };
    private readonly EncounterScreen _encounterScreen = new();
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
        _partySummary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        sidebar.AddChild(_partySummary);
        _partyDetails.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        sidebar.AddChild(_partyDetails);

        _contextTitle.AddThemeFontSizeOverride("font_size", 16);
        sidebar.AddChild(_contextTitle);
        _contextActions.AddThemeConstantOverride("separation", 6);
        sidebar.AddChild(_contextActions);
        _contextActions.AddChild(_buyRationButton);
        _contextActions.AddChild(_refineLodestoneButton);
        _contextActions.AddChild(_gatherLodestoneButton);
        _contextActions.AddChild(_mineLodestoneButton);
        _contextActions.AddChild(_enterPillarButton);
        _contextActions.AddChild(_goDeeperPillarButton);
        _contextActions.AddChild(_searchPillarButton);
        _contextActions.AddChild(_exitPillarButton);

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
        _buyRationButton.Pressed += BuySettlementRation;
        _refineLodestoneButton.Pressed += RefineSettlementLodestone;
        _gatherLodestoneButton.Pressed += GatherPillarLodestone;
        _mineLodestoneButton.Pressed += MinePillarLodestone;
        _enterPillarButton.Pressed += EnterPillarDelve;
        _goDeeperPillarButton.Pressed += GoDeeperInPillar;
        _searchPillarButton.Pressed += SearchPillarTunnel;
        _exitPillarButton.Pressed += ExitPillarDelve;
        _campaignMenu.GetPopup().AddItem("New regional map", NewRegionalMapMenuId);
        _campaignMenu.GetPopup().IdPressed += ShowCampaignMenuAction;
        _newCampaignConfirmation.Confirmed += StartNewRegionalMap;
        AddChild(_newCampaignConfirmation);
        AddChild(_resolutionDialog);
        AddChild(_encounterScreen);
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
        if (result.Interruption is { } interruption) PresentInterruption(interruption);
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
                if (result.Interruption is { } interruption)
                {
                    PresentInterruption(interruption);
                    break;
                }
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
                if (result.Interruption is { } interruption)
                {
                    PresentInterruption(interruption);
                    break;
                }
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

    private void BuySettlementRation() => PresentCampaignAction(_navigation.Campaign.TryBuyRationsAtSettlement());

    private void RefineSettlementLodestone() => PresentCampaignAction(_navigation.Campaign.TryRefineRawLodestoneAtSettlement());

    private void GatherPillarLodestone() => PresentCampaignAction(_navigation.Campaign.TryWorkPillar(PillarWork.Gathering));

    private void MinePillarLodestone() => PresentCampaignAction(_navigation.Campaign.TryWorkPillar(PillarWork.Mining));

    private void EnterPillarDelve() => PresentCampaignAction(_navigation.Campaign.TryEnterPillarDelve());

    private void GoDeeperInPillar() => PresentCampaignAction(_navigation.Campaign.TryGoDeeperInPillar());

    private void SearchPillarTunnel() => PresentCampaignAction(_navigation.Campaign.TrySearchPillarTunnel());

    private void ExitPillarDelve() => PresentCampaignAction(_navigation.Campaign.TryExitPillarDelve());

    private void PresentCampaignAction(CampaignActionResult result)
    {
        _inspector.Text = result.Summary;
        _resolutionDialog.Title = result.Title;
        _resolutionDialog.DialogText = result.Summary;
        _resolutionDialog.PopupCentered();
        if (result.Applied)
        {
            CampaignFile.Save(_navigation.Campaign, _campaignPath);
        }

        RefreshUi();
    }

    private void PresentInterruption(TravelInterruption interruption)
    {
        var resolution = _navigation.Campaign.ResolveTravelInterruption(interruption);
        _encounterScreen.Present(resolution);
        CampaignFile.Save(_navigation.Campaign, _campaignPath);
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
        var inPillarDelve = _navigation.Campaign.IsInPillarDelve;
        _localButton.Disabled = current is MapLocation.Local;
        _dungeonButton.Disabled = current is not MapLocation.Local currentLocal ||
                                  !_navigation.Campaign.HasDungeonEntrance(currentLocal.RegionalCoordinate) ||
                                  !_navigation.Campaign.IsPartyAtDungeonEntrance ||
                                  inPillarDelve;
        var partyTravel = _navigation.Campaign.PartyTravel;
        var viewingPartyLocalMap = current is MapLocation.Local partyLocal && partyLocal.RegionalCoordinate == partyTravel.RegionalCoordinate;
        _movePartyButton.Disabled = inPillarDelve || !viewingPartyLocalMap || _mapCanvas?.PreviewLocalPath.Count is not > 1 || partyTravel.RestRequired || _animatingParty;
        _forcedMarchButton.Disabled = inPillarDelve || !partyTravel.CanForcedMarch;
        _restButton.Disabled = inPillarDelve;
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
        _partySummary.Text =
            $"Terrain { _navigation.Campaign.PartyTerrain } | Coins {_navigation.Campaign.PartyCoins} | Raw Lodestone {_navigation.Campaign.PartyRawLodestone}\n" +
            $"Daily travel {partyTravel.DailyMiles}/{partyTravel.DailyMileLimit} | Forced march {(partyTravel.ForcedMarchUsed ? "used" : "available after 18 miles")}";
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
        RefreshContextActions(current);
        _mapCanvas?.Refresh();
    }

    private void RefreshContextActions(MapLocation current)
    {
        var atPartyLocal = current is MapLocation.Local local &&
                           local.RegionalCoordinate == _navigation.Campaign.PartyTravel.RegionalCoordinate;
        var inPillarDelve = _navigation.Campaign.IsInPillarDelve;
        var atSettlement = atPartyLocal && _navigation.Campaign.IsPartyOnSettlement;
        var atPillar = atPartyLocal && _navigation.Campaign.IsPartyOnPillar && !inPillarDelve;
        var hasContext = atSettlement || atPillar || inPillarDelve;

        _contextTitle.Visible = hasContext;
        _contextActions.Visible = hasContext;
        _contextTitle.Text = atSettlement
            ? "Settlement shop"
            : inPillarDelve
                ? "Pillar delve"
                : atPillar
                    ? "Pillar work"
                    : string.Empty;

        _buyRationButton.Visible = atSettlement;
        _buyRationButton.Disabled = !atSettlement || _navigation.Campaign.PartyCoins < Campaign.RationCoinCost;
        _refineLodestoneButton.Visible = atSettlement;
        _refineLodestoneButton.Disabled = !atSettlement || _navigation.Campaign.PartyRawLodestone == 0;

        _gatherLodestoneButton.Visible = atPillar;
        _gatherLodestoneButton.Disabled = !atPillar;
        _mineLodestoneButton.Visible = atPillar;
        _mineLodestoneButton.Disabled = !atPillar || !_navigation.Campaign.PartyHasMiningTools;
        _enterPillarButton.Visible = atPillar;
        _enterPillarButton.Disabled = !atPillar;

        _goDeeperPillarButton.Visible = inPillarDelve;
        _searchPillarButton.Visible = inPillarDelve;
        _exitPillarButton.Visible = inPillarDelve;
        _goDeeperPillarButton.Disabled = !inPillarDelve;
        _searchPillarButton.Disabled = !inPillarDelve;
        _exitPillarButton.Disabled = !inPillarDelve;
    }

    private void SetInspectorText(string text)
    {
        _inspector.Text = text;
        RefreshUi();
    }
}
