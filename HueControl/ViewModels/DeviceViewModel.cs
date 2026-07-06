// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Input;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

public enum DeviceKind { Light, Accessory }

/// <summary>A single Hue device (light/plug or accessory) shown in Settings → Devices.</summary>
public sealed class DeviceViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Action<Exception> _onError;
    private readonly Func<DeviceViewModel, Task> _requestDelete;

    private string _name;
    private bool _busy;

    public DeviceViewModel(
        string id,
        DeviceKind kind,
        string name,
        string typeLabel,
        bool reachable,
        bool isNew,
        string roomName,
        HueApiClient client,
        Action<Exception> onError,
        Func<DeviceViewModel, Task> requestDelete)
    {
        Id = id;
        Kind = kind;
        _name = name;
        TypeLabel = typeLabel;
        Reachable = reachable;
        IsNew = isNew;
        RoomName = roomName;
        _client = client;
        _onError = onError;
        _requestDelete = requestDelete;

        RenameCommand = new AsyncRelayCommand(_ => RenameAsync(), _ => !_busy);
        DeleteCommand = new AsyncRelayCommand(_ => _requestDelete(this), _ => !_busy);
    }

    public string Id { get; }
    public DeviceKind Kind { get; }
    public string TypeLabel { get; }
    public bool Reachable { get; }
    public bool IsNew { get; }
    public string RoomName { get; }

    public string KindLabel => Kind == DeviceKind.Light ? Loc.T("Kind_Light") : Loc.T("Kind_Accessory");
    public string TypeGroup => Kind == DeviceKind.Light ? Loc.T("Type_Lights") : Loc.T("Section_Accessories");
    public string Subtitle => Reachable ? $"{KindLabel} · {TypeLabel}" : $"{KindLabel} · {TypeLabel} · {Loc.T("Unreachable")}";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public ICommand RenameCommand { get; }
    public ICommand DeleteCommand { get; }

    private async Task RenameAsync()
    {
        string trimmed = _name.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        _busy = true;
        try
        {
            if (Kind == DeviceKind.Light)
                await _client.RenameLightAsync(Id, trimmed);
            else
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
