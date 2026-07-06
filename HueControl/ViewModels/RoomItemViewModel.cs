// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using System.Windows.Input;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>A light with a checkbox indicating membership in a room/zone.</summary>
public sealed class LightMembershipViewModel : ObservableObject
{
    private bool _isMember;

    public LightMembershipViewModel(string id, string name, bool isMember)
    {
        Id = id;
        Name = name;
        _isMember = isMember;
    }

    public string Id { get; }
    public string Name { get; }

    public bool IsMember
    {
        get => _isMember;
        set => SetProperty(ref _isMember, value);
    }
}

/// <summary>A room or zone shown in Settings → Rooms &amp; Zones.</summary>
public sealed class RoomItemViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Action<Exception> _onError;
    private readonly Func<RoomItemViewModel, Task> _requestDelete;
    private readonly Action<string> _report;

    private string _name;
    private bool _busy;

    public RoomItemViewModel(
        string id,
        string name,
        string typeLabel,
        IEnumerable<LightMembershipViewModel> lights,
        HueApiClient client,
        Action<Exception> onError,
        Func<RoomItemViewModel, Task> requestDelete,
        Action<string> report)
    {
        Id = id;
        _name = name;
        TypeLabel = typeLabel;
        _client = client;
        _onError = onError;
        _requestDelete = requestDelete;
        _report = report;
        Lights = new ObservableCollection<LightMembershipViewModel>(lights);

        RenameCommand = new AsyncRelayCommand(_ => RenameAsync(), _ => !_busy);
        ApplyLightsCommand = new AsyncRelayCommand(_ => ApplyLightsAsync(), _ => !_busy);
        DeleteCommand = new AsyncRelayCommand(_ => _requestDelete(this), _ => !_busy);
    }

    public string Id { get; }
    public string TypeLabel { get; }
    public ObservableCollection<LightMembershipViewModel> Lights { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string TypeWord => string.Equals(TypeLabel, "Zone", StringComparison.OrdinalIgnoreCase)
        ? Loc.T("RoomType_Zone") : Loc.T("RoomType_Room");

    public string Subtitle => $"{TypeWord} · {Loc.T("NLights_Fmt", Lights.Count(l => l.IsMember))}";

    public ICommand RenameCommand { get; }
    public ICommand ApplyLightsCommand { get; }
    public ICommand DeleteCommand { get; }

    private async Task RenameAsync()
    {
        string trimmed = _name.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        _busy = true;
        try
        {
            await _client.RenameGroupAsync(Id, trimmed);
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

    private async Task ApplyLightsAsync()
    {
        string[] memberIds = Lights.Where(l => l.IsMember).Select(l => l.Id).ToArray();
        if (memberIds.Length == 0)
        {
            _report(Loc.T("Status_AtLeastOneLight"));
            return;
        }

        _busy = true;
        try
        {
            await _client.SetGroupLightsAsync(Id, memberIds);
            OnPropertyChanged(nameof(Subtitle));
            _report(Loc.T("Status_UpdatedLights_Fmt", _name));
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
