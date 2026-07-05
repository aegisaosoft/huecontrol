// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Text.Json.Serialization;

namespace HueControl.Models;

/// <summary>A bridge automation rule (used to map switch button presses to actions).</summary>
public sealed class HueRuleDto
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("conditions")] public List<HueRuleCondition> Conditions { get; set; } = new();
}

public sealed class HueRuleCondition
{
    [JsonPropertyName("address")] public string? Address { get; set; }
}
