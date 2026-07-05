// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

namespace HueControl.Models;

/// <summary>A home groups one or more Hue bridges (a house can have several bridges).</summary>
public sealed class Home
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "My Home";
    public List<SavedBridge> Bridges { get; set; } = new();
}

/// <summary>Result of the add-bridge dialog: the paired bridge and the home to place it in.</summary>
public sealed record AddBridgeResult(SavedBridge Bridge, string HomeName);
