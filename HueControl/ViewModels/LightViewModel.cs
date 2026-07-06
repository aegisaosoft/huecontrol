// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Input;
using System.Windows.Media;
using HueControl.Models;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>Represents a single Hue light and pushes user changes back to the bridge.</summary>
public sealed class LightViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Action<Exception> _onError;
    private readonly Func<Color, Color?> _pickColor;

    private readonly Debouncer _brightnessDebouncer = new();
    private bool _suppress;
    private bool _on;
    private double _brightness;
    private bool _reachable;
    private Brush _colorBrush = Brushes.Gray;

    public LightViewModel(
        string id,
        HueLightDto dto,
        HueApiClient client,
        Action<Exception> onError,
        Func<Color, Color?> pickColor)
    {
        Id = id;
        _client = client;
        _onError = onError;
        _pickColor = pickColor;
        Name = dto.Name;
        ModelId = dto.ModelId ?? string.Empty;
        UniqueId = dto.UniqueId ?? string.Empty;
        SupportsColor = dto.SupportsColor;
        SupportsColorTemperature = dto.SupportsColorTemperature;

        ToggleCommand = new RelayCommand(() => On = !On);
        PickColorCommand = new RelayCommand(PickColor, () => Reachable && SupportsColor);

        ApplyState(dto);
    }

    public string Id { get; }
    public string Name { get; }
    public string ModelId { get; }
    public string UniqueId { get; }
    public bool SupportsColor { get; }
    public bool SupportsColorTemperature { get; }

    public ICommand ToggleCommand { get; }
    public ICommand PickColorCommand { get; }

    public bool On
    {
        get => _on;
        set
        {
            if (!SetProperty(ref _on, value) || _suppress)
                return;
            _ = SendAsync(new { on = value });
        }
    }

    /// <summary>Brightness on a 0..100 scale for the UI; mapped to the bridge 1..254 range.</summary>
    public double Brightness
    {
        get => _brightness;
        set
        {
            if (!SetProperty(ref _brightness, value) || _suppress)
                return;

            // Debounce so dragging the slider sends only the settled value, not one
            // request per pixel (which the bridge cannot keep up with).
            int bri = (int)Math.Round(Math.Clamp(value, 0, 100) / 100.0 * 254.0);
            bri = Math.Clamp(bri, 1, 254);
            object body = value <= 0 ? new { on = false } : new { on = true, bri };
            _brightnessDebouncer.Run(90, () => SendAsync(body));
        }
    }

    public bool Reachable
    {
        get => _reachable;
        private set => SetProperty(ref _reachable, value);
    }

    public Brush ColorBrush
    {
        get => _colorBrush;
        private set => SetProperty(ref _colorBrush, value);
    }

    public string StatusText => Reachable ? $"{ModelId}" : Loc.T("Unreachable");

    /// <summary>Updates the displayed brightness from room sync, without sending to the bridge.</summary>
    public void SyncSetBrightness(double value)
    {
        _suppress = true;
        try { Brightness = value; }
        finally { _suppress = false; }
    }

    /// <summary>Updates the displayed on/off state from room sync, without sending to the bridge.</summary>
    public void SyncSetOn(bool on)
    {
        _suppress = true;
        try { On = on; }
        finally { _suppress = false; }
    }

    /// <summary>Refreshes the view model from a fresh datastore snapshot without echoing writes.</summary>
    public void ApplyState(HueLightDto dto)
    {
        _suppress = true;
        try
        {
            On = dto.State.On;
            Brightness = Math.Round(dto.State.Brightness / 254.0 * 100.0);
            Reachable = dto.State.Reachable;
            ColorBrush = new SolidColorBrush(ResolveColor(dto));
            OnPropertyChanged(nameof(StatusText));
        }
        finally
        {
            _suppress = false;
        }
    }

    private static Color ResolveColor(HueLightDto dto)
    {
        HueState s = dto.State;
        if (string.Equals(s.ColorMode, "xy", StringComparison.OrdinalIgnoreCase)
            && s.Xy is { Length: 2 })
        {
            return ColorMath.XyToRgb(s.Xy[0], s.Xy[1], Math.Max(s.Brightness, 40));
        }

        if (s.ColorTemperature > 0)
            return ColorMath.ColorTemperatureToRgb(s.ColorTemperature);

        return Color.FromRgb(0xFF, 0xE4, 0xB5);
    }

    private void PickColor()
    {
        Color initial = (_colorBrush as SolidColorBrush)?.Color ?? Colors.White;
        Color? picked = _pickColor(initial);
        if (picked is not { } color)
            return;

        (double x, double y) = ColorMath.RgbToXy(color);
        ColorBrush = new SolidColorBrush(color);
        _ = SendAsync(new { on = true, xy = new[] { x, y } });
    }

    private async Task SendAsync(object body)
    {
        try
        {
            await _client.SetLightStateAsync(Id, body);
        }
        catch (Exception ex)
        {
            _onError(ex);
        }
    }
}
