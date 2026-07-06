// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using HueControl.Models;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>Settings section: manage any Hue device (lights, plugs, accessories) on the bridge.</summary>
public sealed class DevicesSectionViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Func<string, bool> _confirm;

    private string _statusMessage = Loc.T("Status_LoadingDevices");
    private bool _isBusy;

    public DevicesSectionViewModel(HueApiClient client, Func<string, bool> confirm)
    {
        _client = client;
        _confirm = confirm;

        RefreshCommand = new AsyncRelayCommand(_ => LoadDevicesAsync(), _ => !IsBusy);
        SearchCommand = new AsyncRelayCommand(_ => SearchAsync(), _ => !IsBusy);

        DevicesView = CollectionViewSource.GetDefaultView(Devices);
        ApplyGrouping();

        _ = LoadDevicesAsync();
    }

    public string Title => Loc.T("Section_Devices");

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();

    /// <summary>Devices grouped per the selected mode.</summary>
    public ICollectionView DevicesView { get; }

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
                Grouping.Apply(DevicesView, nameof(DeviceViewModel.TypeGroup), nameof(DeviceViewModel.RoomName));
                break;
            case 2: // Flat
                Grouping.Apply(DevicesView);
                break;
            default: // By room
                Grouping.Apply(DevicesView, nameof(DeviceViewModel.RoomName), nameof(DeviceViewModel.TypeGroup));
                break;
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }

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

    private async Task LoadDevicesAsync(ISet<string>? newLightIds = null, ISet<string>? newSensorIds = null)
    {
        IsBusy = true;
        try
        {
            var lights = await _client.GetLightsAsync();
            var sensors = await _client.GetSensorsAsync();
            var groups = await _client.GetGroupsAsync();

            (var lightRoom, var sensorRoom) = BuildRoomMaps(groups);

            Devices.Clear();

            foreach (var (id, dto) in lights.OrderBy(l => l.Value.Name))
            {
                Devices.Add(new DeviceViewModel(
                    id, DeviceKind.Light, dto.Name,
                    dto.ProductName ?? dto.Type ?? dto.ModelId ?? "Light",
                    dto.State.Reachable, newLightIds?.Contains(id) ?? false,
                    lightRoom.GetValueOrDefault(id, Loc.T("Unassigned")),
                    _client, ReportError, DeleteDeviceAsync));
            }

            foreach (var (id, dto) in sensors.Where(s => s.Value.IsPhysical).OrderBy(s => s.Value.Name))
            {
                Devices.Add(new DeviceViewModel(
                    id, DeviceKind.Accessory, dto.Name,
                    dto.ProductName ?? dto.Type ?? "Accessory",
                    reachable: true, newSensorIds?.Contains(id) ?? false,
                    sensorRoom.GetValueOrDefault(id, Loc.T("Unassigned")),
                    _client, ReportError, DeleteDeviceAsync));
            }

            int lightCount = Devices.Count(d => d.Kind == DeviceKind.Light);
            int accCount = Devices.Count(d => d.Kind == DeviceKind.Accessory);
            StatusMessage = Loc.T("Status_DeviceSummary_Fmt", lightCount, accCount);
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

    private async Task SearchAsync()
    {
        IsBusy = true;
        StatusMessage = Loc.T("Status_SearchingNew");
        var newLights = new HashSet<string>();
        var newSensors = new HashSet<string>();
        try
        {
            await _client.StartLightSearchAsync();
            await _client.StartSensorSearchAsync();

            // The bridge scans for ~40s; poll until both scans stop being "active".
            for (int i = 0; i < 16; i++)
            {
                await Task.Delay(3000);

                NewDevicesScan lightScan = await _client.GetNewLightsAsync();
                NewDevicesScan sensorScan = await _client.GetNewSensorsAsync();

                foreach (NewDevice d in lightScan.Devices) newLights.Add(d.Id);
                foreach (NewDevice d in sensorScan.Devices) newSensors.Add(d.Id);

                bool lightsDone = !string.Equals(lightScan.LastScan, "active", StringComparison.OrdinalIgnoreCase);
                bool sensorsDone = !string.Equals(sensorScan.LastScan, "active", StringComparison.OrdinalIgnoreCase);
                StatusMessage = Loc.T("Status_SearchingFound_Fmt", newLights.Count + newSensors.Count);
                if (i >= 2 && lightsDone && sensorsDone)
                    break;
            }

            await LoadDevicesAsync(newLights, newSensors);
            int found = newLights.Count + newSensors.Count;
            StatusMessage = found > 0
                ? Loc.T("Status_FoundNew_Fmt", found)
                : Loc.T("Status_NoNew");
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

    private async Task DeleteDeviceAsync(DeviceViewModel device)
    {
        if (!_confirm(Loc.T("Confirm_DeleteDevice_Fmt", device.Name)))
            return;

        try
        {
            if (device.Kind == DeviceKind.Light)
                await _client.DeleteLightAsync(device.Id);
            else
                await _client.DeleteSensorAsync(device.Id);

            Devices.Remove(device);
            StatusMessage = Loc.T("Status_Deleted_Fmt", device.Name);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    /// <summary>Maps each light/sensor id to the name of the room (or zone) that contains it.</summary>
    internal static (Dictionary<string, string> Lights, Dictionary<string, string> Sensors) BuildRoomMaps(
        Dictionary<string, HueGroupDto> groups)
    {
        var lightRoom = new Dictionary<string, string>();
        var sensorRoom = new Dictionary<string, string>();
        foreach (var (_, g) in groups)
        {
            if (!string.Equals(g.Type, "Room", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(g.Type, "Zone", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (string lid in g.Lights) lightRoom.TryAdd(lid, g.Name);
            foreach (string sid in g.Sensors) sensorRoom.TryAdd(sid, g.Name);
        }
        return (lightRoom, sensorRoom);
    }

    private void ReportError(Exception ex) => StatusMessage = Loc.T("Status_Error_Fmt", ex.Message);
}
