// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Text.Json.Serialization;

namespace HueControl.Models;

/// <summary>A Hue accessory (motion sensor, dimmer switch, button, tap) from /sensors.</summary>
public sealed class HueSensorDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("modelid")] public string? ModelId { get; set; }
    [JsonPropertyName("manufacturername")] public string? Manufacturer { get; set; }
    [JsonPropertyName("productname")] public string? ProductName { get; set; }
    [JsonPropertyName("uniqueid")] public string? UniqueId { get; set; }
    [JsonPropertyName("state")] public HueSensorState State { get; set; } = new();
    [JsonPropertyName("config")] public HueSensorConfig Config { get; set; } = new();

    /// <summary>
    /// True for real Zigbee accessories (ZLL*/ZGP*) rather than the bridge's virtual
    /// CLIP sensors (daylight, status helpers) that should not be shown as devices.
    /// </summary>
    public bool IsPhysical =>
        Type is not null &&
        (Type.StartsWith("ZLL", StringComparison.OrdinalIgnoreCase) ||
         Type.StartsWith("ZGP", StringComparison.OrdinalIgnoreCase));
}

public sealed class HueSensorState
{
    [JsonPropertyName("lastupdated")] public string? LastUpdated { get; set; }
}

public sealed class HueSensorConfig
{
    [JsonPropertyName("battery")] public int? Battery { get; set; }
    [JsonPropertyName("reachable")] public bool? Reachable { get; set; }
}
