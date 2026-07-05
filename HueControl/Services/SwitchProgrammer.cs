// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using HueControl.Models;

namespace HueControl.Services;

/// <summary>
/// Programs a Hue dimmer switch to control a room: ON recalls the room's first scene
/// (its default), OFF turns the room off, and the dim buttons adjust brightness.
/// Only rules belonging to the given switch are touched.
/// </summary>
public static class SwitchProgrammer
{
    public static async Task ConfigureRoomAsync(
        HueApiClient client, string sensorId, string groupId, CancellationToken ct = default)
    {
        // 1. The room's first scene becomes the ON action's default.
        var scenes = await client.GetScenesAsync(ct);
        string? firstScene = scenes
            .Where(s => string.Equals(s.Value.Type, "GroupScene", StringComparison.OrdinalIgnoreCase)
                     && s.Value.Group == groupId)
            .OrderBy(s => s.Value.Name)
            .Select(s => s.Key)
            .FirstOrDefault();

        // 2. Remove this switch's existing rules so ours take over cleanly.
        string prefix = $"/sensors/{sensorId}/";
        var rules = await client.GetRulesAsync(ct);
        foreach (var (ruleId, rule) in rules)
        {
            if (rule.Conditions.Any(c => c.Address is not null && c.Address.StartsWith(prefix, StringComparison.Ordinal)))
                await client.DeleteRuleAsync(ruleId, ct);
        }

        // 3. Create fresh button rules (short-press events).
        object onBody = firstScene is not null ? new { scene = firstScene } : (object)new { on = true };
        await CreateButtonRuleAsync(client, sensorId, groupId, "1002", onBody, "on", ct);
        await CreateButtonRuleAsync(client, sensorId, groupId, "4002", new { on = false }, "off", ct);
        await CreateButtonRuleAsync(client, sensorId, groupId, "2002", new { bri_inc = 30, transitiontime = 9 }, "up", ct);
        await CreateButtonRuleAsync(client, sensorId, groupId, "3002", new { bri_inc = -30, transitiontime = 9 }, "dn", ct);
    }

    private static Task CreateButtonRuleAsync(
        HueApiClient client, string sensorId, string groupId, string buttonEvent, object body, string tag, CancellationToken ct)
    {
        var rule = new
        {
            name = $"HC s{sensorId} {tag}", // Hue rule names are limited to 32 chars.
            conditions = new object[]
            {
                new { address = $"/sensors/{sensorId}/state/buttonevent", @operator = "eq", value = buttonEvent },
                new { address = $"/sensors/{sensorId}/state/lastupdated", @operator = "dx" },
            },
            actions = new object[]
            {
                new { address = $"/groups/{groupId}/action", method = "PUT", body },
            },
        };
        return client.CreateRuleAsync(rule, ct);
    }
}
