// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>An accessory (switch, motion sensor, button, tap) with battery, activity and, for switches, a room binding.</summary>
public sealed class AccessoryViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Action<Exception> _onError;
    private readonly Func<AccessoryViewModel, Task> _requestDelete;
    private readonly Func<AccessoryViewModel, RoomOption, Task> _requestConfigure;

    private string _name;
    private RoomOption? _selectedRoom;
    private bool _busy;

    public AccessoryViewModel(
        string id,
        string name,
        string typeLabel,
        int? battery,
        string? lastUpdated,
        bool isSwitch,
        string roomName,
        IEnumerable<RoomOption> rooms,
        HueApiClient client,
        Action<Exception> onError,
        Func<AccessoryViewModel, Task> requestDelete,
        Func<AccessoryViewModel, RoomOption, Task> requestConfigure)
    {
        Id = id;
        _name = name;
        TypeLabel = typeLabel;
        RoomName = roomName;
        BatteryText = battery.HasValue ? Loc.T("Battery_Fmt", battery.Value) : Loc.T("NoBattery");
        LastActivity = FormatLastUpdated(lastUpdated);
        IsSwitch = isSwitch;
        Rooms = new ObservableCollection<RoomOption>(rooms);
        _selectedRoom = Rooms.FirstOrDefault();
        _client = client;
        _onError = onError;
        _requestDelete = requestDelete;
        _requestConfigure = requestConfigure;

        RenameCommand = new AsyncRelayCommand(_ => RenameAsync(), _ => !_busy);
        DeleteCommand = new AsyncRelayCommand(_ => _requestDelete(this), _ => !_busy);
        ApplyRoomCommand = new AsyncRelayCommand(_ => ConfigureAsync(), _ => !_busy && SelectedRoom is not null);
    }

    public string Id { get; }
    public string TypeLabel { get; }
    public string BatteryText { get; }
    public string LastActivity { get; }
    public bool IsSwitch { get; }
    public string RoomName { get; }
    public ObservableCollection<RoomOption> Rooms { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public RoomOption? SelectedRoom
    {
        get => _selectedRoom;
        set => SetProperty(ref _selectedRoom, value);
    }

    public string Subtitle => $"{TypeLabel} · {BatteryText} · {Loc.T("LastSeen_Fmt", LastActivity)}";

    public ICommand RenameCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ApplyRoomCommand { get; }

    private Task ConfigureAsync() => SelectedRoom is null ? Task.CompletedTask : _requestConfigure(this, SelectedRoom);

    private static string FormatLastUpdated(string? value)
    {
        if (string.IsNullOrEmpty(value) || value == "none")
            return Loc.T("Never");

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime dt))
        {
            return dt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        }

        return value.Replace('T', ' ');
    }

    private async Task RenameAsync()
    {
        string trimmed = _name.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        _busy = true;
        try
        {
            await _client.RenameSensorAsync(Id, trimmed);
        }
        catch (Exception ex)
        {
            _onError(ex);
        }
        finally
        {
            _busy = false;
        }
    }
}

/// <summary>Settings section: view accessories and bind dimmer switches to a room.</summary>
public sealed class AccessoriesSectionViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Func<string, bool> _confirm;

    private string _statusMessage = Loc.T("Status_LoadingAccessories");
    private bool _isBusy;

    public AccessoriesSectionViewModel(HueApiClient client, Func<string, bool> confirm)
    {
        _client = client;
        _confirm = confirm;

        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(), _ => !IsBusy);

        AccessoriesView = CollectionViewSource.GetDefaultView(Accessories);
        ApplyGrouping();

        _ = LoadAsync();
    }

    public string Title => Loc.T("Section_Accessories");

    public ObservableCollection<AccessoryViewModel> Accessories { get; } = new();

    /// <summary>Accessories grouped per the selected mode.</summary>
    public ICollectionView AccessoriesView { get; }

    public string[] GroupModes { get; } = { Loc.T("Group_ByRoom"), Loc.T("Group_ByType"), Loc.T("Group_Flat") };

    private int _groupModeIndex;
    public int SelectedGroupModeIndex
    {
        get => _groupModeIndex;
        set { if (SetProperty(ref _groupModeIndex, value)) ApplyGrouping(); }
    }

    private void ApplyGrouping()
    {
        switch (_groupModeIndex)
        {
            case 1: // By type
                Grouping.Apply(AccessoriesView, nameof(AccessoryViewModel.TypeLabel), nameof(AccessoryViewModel.RoomName));
                break;
            case 2: // Flat
                Grouping.Apply(AccessoriesView);
                break;
            default: // By room
                Grouping.Apply(AccessoriesView, nameof(AccessoryViewModel.RoomName), nameof(AccessoryViewModel.TypeLabel));
                break;
        }
    }

    public ICommand RefreshCommand { get; }

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

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var sensors = await _client.GetSensorsAsync();
            var groups = await _client.GetGroupsAsync();

            var rooms = groups
                .Where(g => string.Equals(g.Value.Type, "Room", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(g.Value.Type, "Zone", StringComparison.OrdinalIgnoreCase))
                .OrderBy(g => g.Value.Name)
                .Select(g => new RoomOption(g.Key, g.Value.Name))
                .ToList();

            (_, var sensorRoom) = DevicesSectionViewModel.BuildRoomMaps(groups);

            Accessories.Clear();
            foreach (var (id, dto) in sensors.Where(s => s.Value.IsPhysical).OrderBy(s => s.Value.Name))
            {
                bool isSwitch = dto.Type?.Contains("Switch", StringComparison.OrdinalIgnoreCase) ?? false;
                Accessories.Add(new AccessoryViewModel(
                    id, dto.Name, dto.ProductName ?? dto.Type ?? "Accessory",
                    dto.Config.Battery, dto.State.LastUpdated, isSwitch,
                    sensorRoom.GetValueOrDefault(id, Loc.T("Unassigned")), rooms,
                    _client, ReportError, DeleteAsync, ConfigureSwitchAsync));
            }

            StatusMessage = Loc.T("Status_AccessorySummary_Fmt", Accessories.Count);
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

    private async Task ConfigureSwitchAsync(AccessoryViewModel accessory, RoomOption room)
    {
        if (!_confirm(Loc.T("Confirm_SetSwitch_Fmt", accessory.Name, room.Name)))
            return;

        IsBusy = true;
        StatusMessage = Loc.T("Status_Programming_Fmt", accessory.Name);
        try
        {
            await SwitchProgrammer.ConfigureRoomAsync(_client, accessory.Id, room.Id);
            StatusMessage = Loc.T("Status_SwitchSet_Fmt", accessory.Name, room.Name);
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

    private async Task DeleteAsync(AccessoryViewModel accessory)
    {
        if (!_confirm(Loc.T("Confirm_DeleteAccessory_Fmt", accessory.Name)))
            return;

        try
        {
            await _client.DeleteSensorAsync(accessory.Id);
            Accessories.Remove(accessory);
            StatusMessage = Loc.T("Status_Deleted_Fmt", accessory.Name);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void ReportError(Exception ex) => StatusMessage = Loc.T("Status_Error_Fmt", ex.Message);
}
