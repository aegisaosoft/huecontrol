// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Input;
using HueControl.Models;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>Represents a Hue room or zone (a group) with on/off and brightness control.</summary>
public sealed class GroupViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Action<Exception> _onError;

    private readonly Debouncer _brightnessDebouncer = new();
    private bool _suppress;
    private bool _anyOn;
    private double _brightness;

    public GroupViewModel(string id, HueGroupDto dto, HueApiClient client, Action<Exception> onError)
    {
        Id = id;
        _client = client;
        _onError = onError;
        Name = dto.Name;
        RoomClass = dto.Class ?? dto.Type ?? "Group";
        LightCount = dto.Lights.Count;
        LightIds = dto.Lights.ToList();

        TurnOnCommand = new RelayCommand(() => SetAll(true));
        TurnOffCommand = new RelayCommand(() => SetAll(false));

        ApplyState(dto);
    }

    public string Id { get; }
    public string Name { get; }
    public string RoomClass { get; }
    public int LightCount { get; }

    /// <summary>Ids of the lights that belong to this room, in bridge order.</summary>
    public IReadOnlyList<string> LightIds { get; }

    public ICommand TurnOnCommand { get; }
    public ICommand TurnOffCommand { get; }

    public string Subtitle => $"{RoomClass} · {LightCount} lights";

    public bool AnyOn
    {
        get => _anyOn;
        private set => SetProperty(ref _anyOn, value);
    }

    public double Brightness
    {
        get => _brightness;
        set
        {
            if (!SetProperty(ref _brightness, value) || _suppress)
                return;

            // Debounce so dragging the room slider sends only the settled value.
            int bri = (int)Math.Round(Math.Clamp(value, 0, 100) / 100.0 * 254.0);
            bri = Math.Clamp(bri, 1, 254);
            object body = value <= 0 ? new { on = false } : new { on = true, bri };
            _brightnessDebouncer.Run(90, () => SendAsync(body));
        }
    }

    /// <summary>Updates the displayed brightness from light sync, without sending to the bridge.</summary>
    public void SyncSetBrightness(double value)
    {
        _suppress = true;
        try { Brightness = value; }
        finally { _suppress = false; }
    }

    /// <summary>Updates the displayed on indicator from light sync, without sending to the bridge.</summary>
    public void SyncSetAnyOn(bool on) => AnyOn = on;

    public void ApplyState(HueGroupDto dto)
    {
        _suppress = true;
        try
        {
            AnyOn = dto.State.AnyOn;
            Brightness = Math.Round(dto.Action.Brightness / 254.0 * 100.0);
        }
        finally
        {
            _suppress = false;
        }
    }

    private void SetAll(bool on)
    {
        AnyOn = on; // Optimistic update so the room indicator reacts immediately.
        _ = SendAsync(new { on });
    }

    private async Task SendAsync(object body)
    {
        try
        {
            await _client.SetGroupActionAsync(Id, body);
        }
        catch (Exception ex)
        {
            _onError(ex);
        }
    }
}
