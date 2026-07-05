// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.IO;
using System.Text.Json;
using HueControl.Models;

namespace HueControl.Tests;

public class HueModelTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    // Path to the real backup captured during migration; the test degrades to a
    // no-op when it is not present on this machine.
    private const string BackupPath = @"C:\aegis-ao\HUE\hue_old_bridge_backup.json";

    [Fact]
    public void LightDto_ReportsColorCapabilities()
    {
        const string json = """
        { "state": { "on": false, "reachable": true },
          "type": "Extended color light", "name": "X", "modelid": "LCT016" }
        """;
        var dto = JsonSerializer.Deserialize<HueLightDto>(json, Options)!;

        Assert.True(dto.SupportsColor);
        Assert.True(dto.SupportsColorTemperature);
    }

    [Fact]
    public void AmbianceLightDto_IsNotColor()
    {
        const string json = """
        { "state": { "on": true, "reachable": true },
          "type": "Color temperature light", "name": "Y", "modelid": "LTA012" }
        """;
        var dto = JsonSerializer.Deserialize<HueLightDto>(json, Options)!;

        Assert.False(dto.SupportsColor);
        Assert.True(dto.SupportsColorTemperature);
    }

    [Fact]
    public void RealBackup_ParsesExpectedTopology()
    {
        if (!File.Exists(BackupPath))
            return; // Fixture not available on this machine; skip silently.

        using var doc = JsonDocument.Parse(File.ReadAllText(BackupPath));
        JsonElement root = doc.RootElement;

        var lights = JsonSerializer.Deserialize<Dictionary<string, HueLightDto>>(
            root.GetProperty("lights").GetRawText(), Options)!;
        var groups = JsonSerializer.Deserialize<Dictionary<string, HueGroupDto>>(
            root.GetProperty("groups").GetRawText(), Options)!;
        var scenes = JsonSerializer.Deserialize<Dictionary<string, HueSceneDto>>(
            root.GetProperty("scenes").GetRawText(), Options)!;

        int rooms = groups.Values.Count(g =>
            string.Equals(g.Type, "Room", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(25, lights.Count);
        Assert.Equal(7, rooms);
        Assert.True(scenes.Count > 100);

        // Every light in the backup carries a stable Zigbee uniqueid used for remapping.
        Assert.All(lights.Values, l => Assert.False(string.IsNullOrWhiteSpace(l.UniqueId)));
    }
}
