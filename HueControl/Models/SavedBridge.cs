// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

namespace HueControl.Models;

/// <summary>A bridge the user has paired with, persisted between sessions.</summary>
public sealed class SavedBridge
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Hue Bridge";
    public string IpAddress { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
}

/// <summary>A bridge discovered on the network but not necessarily paired yet.</summary>
public sealed class DiscoveredBridge
{
    public string Id { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 443;

    public override string ToString() => $"{IpAddress}  ({Id})";
}
