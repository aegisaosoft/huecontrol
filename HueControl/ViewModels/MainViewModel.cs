// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HueControl.Models;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>Root view model: owns the bridge list and the lights/rooms/scenes of the active bridge.</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly SettingsStore _store = new();
    private readonly List<Home> _homes;

    private HueApiClient? _client;
    private SavedBridge? _selectedBridge;
    private GroupViewModel? _selectedGroup;
    private GroupViewModel? _syncGroup;
    private bool _syncing;
    private string _statusMessage = Loc.T("Status_AddBridgeToStart");
    private bool _isLoading;

    public MainViewModel()
    {
        AddBridgeCommand = new RelayCommand(AddBridge);
        RemoveBridgeCommand = new RelayCommand(RemoveBridge, () => SelectedBridge is not null);
        RefreshCommand = new AsyncRelayCommand(_ => ReloadAsync(), _ => _client is not null && !IsLoading);
        AllOnCommand = new AsyncRelayCommand(_ => SetAllAsync(true), _ => _client is not null);
        AllOffCommand = new AsyncRelayCommand(_ => SetAllAsync(false), _ => _client is not null);
        RoomOnCommand = new RelayCommand(p => SetRoomAll(p as string, true));
        RoomOffCommand = new RelayCommand(p => SetRoomAll(p as string, false));
        AddHomeCommand = new RelayCommand(AddHome);
        AddSceneCommand = new AsyncRelayCommand(p => AddSceneAsync(p as string));
        DeleteSceneCommand = new AsyncRelayCommand(p => DeleteSceneAsync(p as SceneViewModel));

        _homes = _store.LoadHomes();
        RebuildHomes();
        SelectedBridge = _homes.SelectMany(h => h.Bridges).FirstOrDefault();
    }

    /// <summary>Set by the view to show the native colour picker.</summary>
    public Func<Color, Color?> ColorPicker { get; set; } = _ => null;

    /// <summary>Set by the view to display the add-bridge dialog and return the paired result.</summary>
    public Func<AddBridgeResult?> AddBridgeDialog { get; set; } = () => null;

    /// <summary>Set by the view to prompt for a name (title, initial) → entered name or null.</summary>
    public Func<string, string, string?> AskName { get; set; } = (_, _) => null;

    /// <summary>Set by the view to ask a yes/no confirmation.</summary>
    public Func<string, bool> Confirm { get; set; } = _ => false;

    public ObservableCollection<HomeViewModel> Homes { get; } = new();

    /// <summary>Names of existing homes, used to prefill the add-bridge dialog.</summary>
    public IEnumerable<string> HomeNames => _homes.Select(h => h.Name);
    public ObservableCollection<LightViewModel> Lights { get; } = new();
    public ObservableCollection<GroupViewModel> Groups { get; } = new();
    public ObservableCollection<SceneViewModel> Scenes { get; } = new();

    /// <summary>Lights that belong to the currently selected room.</summary>
    public ObservableCollection<LightViewModel> SelectedRoomLights { get; } = new();

    public ICommand AddBridgeCommand { get; }
    public ICommand RemoveBridgeCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AllOnCommand { get; }
    public ICommand AllOffCommand { get; }
    public ICommand RoomOnCommand { get; }
    public ICommand RoomOffCommand { get; }
    public ICommand AddHomeCommand { get; }
    public ICommand AddSceneCommand { get; }
    public ICommand DeleteSceneCommand { get; }

    /// <summary>Set by the view to pick a built-in Hue scene to add.</summary>
    public Func<ScenePreset?> PickScene { get; set; } = () => null;

    public SavedBridge? SelectedBridge
    {
        get => _selectedBridge;
        set
        {
            if (!SetProperty(ref _selectedBridge, value))
                return;

            _client = value is null ? null : new HueApiClient(value.IpAddress, value.AppKey);
            UpdateSelectionHighlight();
            _ = ReloadAsync();
        }
    }

    public GroupViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
                UpdateSelectedRoomLights();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>Builds the Settings view model for the active bridge, or null if none selected.</summary>
    public SettingsViewModel? CreateSettings(Func<string, bool> confirm)
        => _client is null ? null : new SettingsViewModel(SelectedBridge?.Name ?? Loc.T("ThisBridge"), _client, confirm);

    private void AddBridge()
    {
        AddBridgeResult? result = AddBridgeDialog();
        if (result is null)
            return;

        SavedBridge bridge = result.Bridge;

        // Avoid duplicates: drop any bridge with the same IP from any home.
        foreach (Home home in _homes)
            home.Bridges.RemoveAll(b => b.IpAddress == bridge.IpAddress);

        // Place it in the chosen home, creating that home if needed.
        string homeName = string.IsNullOrWhiteSpace(result.HomeName) ? "My Home" : result.HomeName.Trim();
        Home? target = _homes.FirstOrDefault(h => string.Equals(h.Name, homeName, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            target = new Home { Name = homeName };
            _homes.Add(target);
        }
        target.Bridges.Add(bridge);

        _store.SaveHomes(_homes);
        RebuildHomes();
        SelectedBridge = bridge;
    }

    private void RemoveBridge()
    {
        if (_selectedBridge is null)
            return;

        foreach (Home home in _homes)
            home.Bridges.RemoveAll(b => b.IpAddress == _selectedBridge.IpAddress);
        _homes.RemoveAll(h => h.Bridges.Count == 0);

        _store.SaveHomes(_homes);
        RebuildHomes();
        SelectedBridge = _homes.SelectMany(h => h.Bridges).FirstOrDefault();
    }

    private void AddHome()
    {
        string? name = AskName(Loc.T("Ask_AddHome"), string.Empty);
        if (name is null)
            return;

        if (_homes.Any(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = Loc.T("Status_HomeExists_Fmt", name);
            return;
        }

        _homes.Add(new Home { Name = name });
        _store.SaveHomes(_homes);
        RebuildHomes();
    }

    private void RenameHome(HomeViewModel home)
    {
        string? name = AskName(Loc.T("Ask_RenameHome"), home.Name);
        if (name is null)
            return;

        home.Home.Name = name;
        _store.SaveHomes(_homes);
        RebuildHomes();
    }

    private void DeleteHome(HomeViewModel home)
    {
        int count = home.Home.Bridges.Count;
        if (!Confirm(Loc.T("Confirm_DeleteHome_Fmt", home.Name, count)))
            return;

        bool removedSelected = home.Home.Bridges.Any(b => b.IpAddress == _selectedBridge?.IpAddress);
        _homes.Remove(home.Home);
        _store.SaveHomes(_homes);
        RebuildHomes();

        if (removedSelected)
            SelectedBridge = _homes.SelectMany(h => h.Bridges).FirstOrDefault();
    }

    private void RebuildHomes()
    {
        Homes.Clear();
        foreach (Home home in _homes)
            Homes.Add(new HomeViewModel(home, SelectBridgeItem, RenameHome, DeleteHome));
        OnPropertyChanged(nameof(HomeNames));
        UpdateSelectionHighlight();
    }

    private void SelectBridgeItem(BridgeItemViewModel item) => SelectedBridge = item.Bridge;

    private void UpdateSelectionHighlight()
    {
        foreach (HomeViewModel home in Homes)
            foreach (BridgeItemViewModel item in home.Bridges)
                item.IsSelected = ReferenceEquals(item.Bridge, _selectedBridge);
    }

    private async Task ReloadAsync()
    {
        SelectedGroup = null;
        Lights.Clear();
        Groups.Clear();
        Scenes.Clear();
        SelectedRoomLights.Clear();

        if (_client is null)
        {
            StatusMessage = Loc.T("Status_NoBridgeSelected");
            return;
        }

        IsLoading = true;
        StatusMessage = Loc.T("Status_LoadingBridge_Fmt", SelectedBridge?.Name, _client.IpAddress);
        try
        {
            var lights = await _client.GetLightsAsync();
            var groups = await _client.GetGroupsAsync();
            var scenes = await _client.GetScenesAsync();

            foreach (var (id, dto) in lights.OrderBy(l => l.Value.Name))
                Lights.Add(new LightViewModel(id, dto, _client, ReportError, ColorPicker));

            foreach (var (id, dto) in groups
                         .Where(g => IsRoomOrZone(g.Value))
                         .OrderBy(g => g.Value.Name))
            {
                Groups.Add(new GroupViewModel(id, dto, _client, ReportError));
            }

            foreach (var (id, dto) in scenes.OrderBy(s => s.Value.Name))
            {
                string? roomName = !string.IsNullOrEmpty(dto.Group) && groups.TryGetValue(dto.Group, out var g)
                    ? g.Name
                    : null;
                Scenes.Add(new SceneViewModel(id, dto, roomName, _client, ReportError));
            }

            int roomCount = Groups.Count;
            if (Lights.Count > 0)
                Groups.Insert(0, CreateAllGroup());

            SelectedGroup = Groups.FirstOrDefault();
            StatusMessage = Loc.T("Status_BridgeSummary_Fmt", SelectedBridge?.Name, Lights.Count, roomCount, Scenes.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = Loc.T("Status_FailedLoad_Fmt", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SetAllAsync(bool on)
    {
        if (_client is null)
            return;

        try
        {
            // Group 0 is the special "all lights" group on every bridge.
            await _client.SetGroupActionAsync("0", new { on });
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    /// <summary>
    /// Builds a synthetic "All" group backed by bridge group 0 (every light), so it
    /// can appear as the first room chip and control all lights at once.
    /// </summary>
    private GroupViewModel CreateAllGroup()
    {
        int avgBrightness = (int)Math.Round(Lights.Average(l => l.Brightness) / 100.0 * 254.0);
        var dto = new HueGroupDto
        {
            Name = Loc.T("All"),
            Type = "Room",
            Class = Loc.T("EveryRoom"),
            Lights = Lights.Select(l => l.Id).ToList(),
            State = new HueGroupState { AnyOn = Lights.Any(l => l.On), AllOn = Lights.All(l => l.On) },
            Action = new HueState { Brightness = avgBrightness },
        };

        // Group 0 is the special "all lights" group on every bridge.
        return new GroupViewModel("0", dto, _client!, ReportError);
    }

    private void UpdateSelectedRoomLights()
    {
        // Detach sync handlers from the previously selected room and its lights.
        if (_syncGroup is not null)
            _syncGroup.PropertyChanged -= OnGroupPropertyChanged;
        foreach (LightViewModel light in SelectedRoomLights)
            light.PropertyChanged -= OnLightPropertyChanged;

        SelectedRoomLights.Clear();
        _syncGroup = _selectedGroup;
        if (_selectedGroup is null)
            return;

        // Preserve the room's own light ordering from the bridge.
        foreach (string id in _selectedGroup.LightIds)
        {
            LightViewModel? light = Lights.FirstOrDefault(l => l.Id == id);
            if (light is not null)
                SelectedRoomLights.Add(light);
        }

        // Keep the room slider and its light sliders in sync.
        foreach (LightViewModel light in SelectedRoomLights)
            light.PropertyChanged += OnLightPropertyChanged;
        _selectedGroup.PropertyChanged += OnGroupPropertyChanged;
    }

    // Room slider/buttons moved -> mirror onto every light in the room (display only).
    private void OnGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncing || _selectedGroup is null)
            return;

        if (e.PropertyName == nameof(GroupViewModel.Brightness))
        {
            _syncing = true;
            try
            {
                foreach (LightViewModel light in SelectedRoomLights)
                {
                    light.SyncSetBrightness(_selectedGroup.Brightness);
                    light.SyncSetOn(_selectedGroup.Brightness > 0);
                }
            }
            finally { _syncing = false; }
        }
        else if (e.PropertyName == nameof(GroupViewModel.AnyOn))
        {
            _syncing = true;
            try
            {
                foreach (LightViewModel light in SelectedRoomLights)
                    light.SyncSetOn(_selectedGroup.AnyOn);
            }
            finally { _syncing = false; }
        }
    }

    // A single light moved -> update the room slider/indicator to the aggregate (display only).
    private void OnLightPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncing || _selectedGroup is null || SelectedRoomLights.Count == 0)
            return;

        if (e.PropertyName is nameof(LightViewModel.Brightness) or nameof(LightViewModel.On))
        {
            _syncing = true;
            try
            {
                _selectedGroup.SyncSetBrightness(SelectedRoomLights.Average(l => l.Brightness));
                _selectedGroup.SyncSetAnyOn(SelectedRoomLights.Any(l => l.On));
            }
            finally { _syncing = false; }
        }
    }

    // Turns a whole room on/off from the Scenes tab header, matched by room name.
    private void SetRoomAll(string? roomName, bool on)
    {
        if (string.IsNullOrEmpty(roomName))
            return;

        GroupViewModel? room = Groups.FirstOrDefault(g => g.Name == roomName);
        if (room is null)
            return;

        if (on)
            room.TurnOnCommand.Execute(null);
        else
            room.TurnOffCommand.Execute(null);
    }

    // Adds a built-in Hue scene to a room from the Scenes tab header.
    private async Task AddSceneAsync(string? roomName)
    {
        if (_client is null || string.IsNullOrEmpty(roomName))
            return;

        GroupViewModel? room = Groups.FirstOrDefault(g => g.Name == roomName && g.Id != "0");
        if (room is null)
            return;

        ScenePreset? preset = PickScene();
        if (preset is null)
            return;

        try
        {
            await HueScenePresets.CreateAsync(_client, preset, room.Id);
            await ReloadAsync();
            StatusMessage = Loc.T("Status_AddedScene_Fmt", preset.Name, roomName);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    // Deletes a scene from the Scenes tab.
    private async Task DeleteSceneAsync(SceneViewModel? scene)
    {
        if (_client is null || scene is null)
            return;
        if (!Confirm(Loc.T("Confirm_DeleteScene_Fmt", scene.Name)))
            return;

        try
        {
            await _client.DeleteSceneAsync(scene.Id);
            await ReloadAsync();
            StatusMessage = Loc.T("Status_DeletedScene_Fmt", scene.Name);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private static bool IsRoomOrZone(HueGroupDto group)
        => string.Equals(group.Type, "Room", StringComparison.OrdinalIgnoreCase)
           || string.Equals(group.Type, "Zone", StringComparison.OrdinalIgnoreCase);

    private void ReportError(Exception ex)
    {
        void Set() => StatusMessage = Loc.T("Status_Error_Fmt", ex.Message);
        if (Application.Current?.Dispatcher.CheckAccess() ?? true)
            Set();
        else
            Application.Current.Dispatcher.Invoke(Set);
    }
}
