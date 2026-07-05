// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using HueControl.Models;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>A room the user can add a scene to.</summary>
public sealed record RoomOption(string Id, string Name);

/// <summary>Settings section: add built-in Hue scenes to a room, delete scenes.</summary>
public sealed class ScenesSectionViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Func<string, bool> _confirm;

    private string _statusMessage = "Loading scenes…";
    private bool _isBusy;
    private RoomOption? _selectedRoom;
    private ScenePreset? _selectedPreset;

    public ScenesSectionViewModel(HueApiClient client, Func<string, bool> confirm)
    {
        _client = client;
        _confirm = confirm;
        _selectedPreset = Presets.FirstOrDefault();

        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(), _ => !IsBusy);
        AddCommand = new AsyncRelayCommand(_ => AddAsync(), _ => !IsBusy);

        ScenesView = CollectionViewSource.GetDefaultView(Scenes);
        ApplyGrouping();

        _ = LoadAsync();
    }

    public string Title => "Scenes";

    public ObservableCollection<SceneItemViewModel> Scenes { get; } = new();
    public ObservableCollection<RoomOption> Rooms { get; } = new();

    /// <summary>The built-in Hue scenes to choose from.</summary>
    public IReadOnlyList<ScenePreset> Presets { get; } = HueScenePresets.All;

    public ICollectionView ScenesView { get; }

    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }

    public string[] GroupModes { get; } = { "By room", "Flat" };

    private string _groupMode = "By room";
    public string SelectedGroupMode
    {
        get => _groupMode;
        set { if (SetProperty(ref _groupMode, value)) ApplyGrouping(); }
    }

    public RoomOption? SelectedRoom
    {
        get => _selectedRoom;
        set => SetProperty(ref _selectedRoom, value);
    }

    public ScenePreset? SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    private void ApplyGrouping()
    {
        if (_groupMode == "Flat")
            Grouping.Apply(ScenesView);
        else
            Grouping.Apply(ScenesView, nameof(SceneItemViewModel.RoomName));
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var groups = await _client.GetGroupsAsync();
            var scenes = await _client.GetScenesAsync();

            Rooms.Clear();
            foreach (var (id, dto) in groups
                         .Where(g => string.Equals(g.Value.Type, "Room", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(g.Value.Type, "Zone", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(g => g.Value.Name))
            {
                Rooms.Add(new RoomOption(id, dto.Name));
            }
            SelectedRoom ??= Rooms.FirstOrDefault();

            Scenes.Clear();
            foreach (var (id, dto) in scenes.OrderBy(s => s.Value.Name))
            {
                string room = !string.IsNullOrEmpty(dto.Group) && groups.TryGetValue(dto.Group!, out var g)
                    ? g.Name
                    : "Custom";
                Scenes.Add(new SceneItemViewModel(id, dto.Name, room, DeleteSceneAsync));
            }

            StatusMessage = $"{Scenes.Count} scenes.";
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddAsync()
    {
        if (SelectedPreset is null)
        {
            StatusMessage = "Pick a Hue scene.";
            return;
        }
        if (SelectedRoom is null)
        {
            StatusMessage = "Pick a room.";
            return;
        }

        IsBusy = true;
        try
        {
            await HueScenePresets.CreateAsync(_client, SelectedPreset, SelectedRoom.Id);
            await LoadAsync();
            StatusMessage = $"Added \"{SelectedPreset.Name}\" to {SelectedRoom.Name}.";
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteSceneAsync(SceneItemViewModel scene)
    {
        if (!_confirm($"Delete scene \"{scene.Name}\"?"))
            return;

        try
        {
            await _client.DeleteSceneAsync(scene.Id);
            Scenes.Remove(scene);
            StatusMessage = $"Deleted scene \"{scene.Name}\".";
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void ReportError(Exception ex) => StatusMessage = $"Error: {ex.Message}";
}
