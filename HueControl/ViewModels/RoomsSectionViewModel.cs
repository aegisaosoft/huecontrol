// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using System.Windows.Input;
using HueControl.Models;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>Settings section: create, rename, delete rooms/zones and edit their light membership.</summary>
public sealed class RoomsSectionViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Func<string, bool> _confirm;

    private string _statusMessage = Loc.T("Status_LoadingRooms");
    private bool _isBusy;
    private string _newRoomName = string.Empty;
    private int _newRoomTypeIndex;

    public RoomsSectionViewModel(HueApiClient client, Func<string, bool> confirm)
    {
        _client = client;
        _confirm = confirm;

        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(), _ => !IsBusy);
        CreateCommand = new AsyncRelayCommand(_ => CreateAsync(), _ => !IsBusy);

        _ = LoadAsync();
    }

    public string Title => Loc.T("Section_Rooms");

    public ObservableCollection<RoomItemViewModel> Rooms { get; } = new();
    public string[] Types { get; } = { Loc.T("RoomType_Room"), Loc.T("RoomType_Zone") };

    /// <summary>Bridge group type for the currently selected new-room type.</summary>
    private string ApiType => _newRoomTypeIndex == 1 ? "Zone" : "Room";

    public ICommand RefreshCommand { get; }
    public ICommand CreateCommand { get; }

    public string NewRoomName
    {
        get => _newRoomName;
        set => SetProperty(ref _newRoomName, value);
    }

    public int NewRoomTypeIndex
    {
        get => _newRoomTypeIndex;
        set => SetProperty(ref _newRoomTypeIndex, value);
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

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var lights = await _client.GetLightsAsync();
            var groups = await _client.GetGroupsAsync();

            var allLights = lights.OrderBy(l => l.Value.Name)
                .Select(l => (Id: l.Key, l.Value.Name))
                .ToList();

            Rooms.Clear();
            foreach (var (id, dto) in groups
                         .Where(g => string.Equals(g.Value.Type, "Room", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(g.Value.Type, "Zone", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(g => g.Value.Name))
            {
                var members = dto.Lights.ToHashSet();
                var memberships = allLights.Select(l =>
                    new LightMembershipViewModel(l.Id, l.Name, members.Contains(l.Id)));

                Rooms.Add(new RoomItemViewModel(
                    id, dto.Name, dto.Type ?? "Room", memberships,
                    _client, ReportError, DeleteRoomAsync, m => StatusMessage = m));
            }

            StatusMessage = Loc.T("Status_RoomsSummary_Fmt", Rooms.Count);
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

    private async Task CreateAsync()
    {
        string name = NewRoomName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            StatusMessage = Loc.T("Status_EnterName");
            return;
        }

        IsBusy = true;
        try
        {
            // Created empty; assign lights via each room's light list afterwards.
            await _client.CreateGroupAsync(name, ApiType, Array.Empty<string>());
            string typeWord = Types[_newRoomTypeIndex].ToLowerInvariant();
            NewRoomName = string.Empty;
            await LoadAsync();
            StatusMessage = Loc.T("Status_CreatedRoom_Fmt", typeWord, name);
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

    private async Task DeleteRoomAsync(RoomItemViewModel room)
    {
        string typeWord = (string.Equals(room.TypeLabel, "Zone", StringComparison.OrdinalIgnoreCase)
            ? Loc.T("RoomType_Zone") : Loc.T("RoomType_Room")).ToLowerInvariant();
        if (!_confirm(Loc.T("Confirm_DeleteRoom_Fmt", room.Name, typeWord)))
            return;

        try
        {
            await _client.DeleteGroupAsync(room.Id);
            Rooms.Remove(room);
            StatusMessage = Loc.T("Status_Deleted_Fmt", room.Name);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void ReportError(Exception ex) => StatusMessage = Loc.T("Status_Error_Fmt", ex.Message);
}
