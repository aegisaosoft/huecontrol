// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Text.Json.Serialization;

namespace HueControl.Models;

// Data transfer objects mirroring the Philips Hue local REST API (v1) datastore.
// The datastore returns dictionaries keyed by string id, so collections are
// modelled as Dictionary<string, T>.

public sealed class HueState
{
    [JsonPropertyName("on")] public bool On { get; set; }
    [JsonPropertyName("bri")] public int Brightness { get; set; }
    [JsonPropertyName("hue")] public int Hue { get; set; }
    [JsonPropertyName("sat")] public int Saturation { get; set; }
    [JsonPropertyName("ct")] public int ColorTemperature { get; set; }
    [JsonPropertyName("xy")] public double[]? Xy { get; set; }
    [JsonPropertyName("colormode")] public string? ColorMode { get; set; }
    [JsonPropertyName("effect")] public string? Effect { get; set; }
    [JsonPropertyName("reachable")] public bool Reachable { get; set; }
}

public sealed class HueLightDto
{
    [JsonPropertyName("state")] public HueState State { get; set; } = new();
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("modelid")] public string? ModelId { get; set; }
    [JsonPropertyName("manufacturername")] public string? Manufacturer { get; set; }
    [JsonPropertyName("productname")] public string? ProductName { get; set; }
    [JsonPropertyName("uniqueid")] public string? UniqueId { get; set; }

    /// <summary>True when the light supports colour (xy) rather than only white/ambiance.</summary>
    public bool SupportsColor =>
        (Type?.Contains("color", StringComparison.OrdinalIgnoreCase) ?? false)
        && (Type?.Contains("temperature", StringComparison.OrdinalIgnoreCase) != true
            || Type.Contains("Extended", StringComparison.OrdinalIgnoreCase));

    /// <summary>True when the light supports colour temperature (ct).</summary>
    public bool SupportsColorTemperature =>
        (Type?.Contains("temperature", StringComparison.OrdinalIgnoreCase) ?? false)
        || (Type?.Contains("Extended", StringComparison.OrdinalIgnoreCase) ?? false);
}

public sealed class HueGroupState
{
    [JsonPropertyName("all_on")] public bool AllOn { get; set; }
    [JsonPropertyName("any_on")] public bool AnyOn { get; set; }
}

public sealed class HueGroupDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("lights")] public List<string> Lights { get; set; } = new();
    [JsonPropertyName("sensors")] public List<string> Sensors { get; set; } = new();
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("class")] public string? Class { get; set; }
    [JsonPropertyName("state")] public HueGroupState State { get; set; } = new();
    [JsonPropertyName("action")] public HueState Action { get; set; } = new();
}

public sealed class HueSceneDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("lights")] public List<string> Lights { get; set; } = new();
    [JsonPropertyName("group")] public string? Group { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
}
