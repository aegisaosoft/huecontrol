// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Media;

namespace HueControl.Services;

/// <summary>A built-in Hue scene: a name plus the stored state for the i-th light in a room.</summary>
public sealed class ScenePreset
{
    public ScenePreset(string name, Func<int, object> stateFor)
    {
        Name = name;
        StateFor = stateFor;
    }

    public string Name { get; }

    /// <summary>Returns the light-state body for light index <paramref name="i"/> in the room.</summary>
    public Func<int, object> StateFor { get; }

    /// <summary>Gradient preview for this scene.</summary>
    public Brush Thumbnail => SceneThumbnails.ForName(Name);

    public override string ToString() => Name;
}

/// <summary>The standard Hue scenes users can add to a room, and the logic to create them.</summary>
public static class HueScenePresets
{
    // Colour-temperature light recipes (exact Hue values), same for every light.
    private static object Ct(int ct, int bri) => new { on = true, bri, ct };

    // A colour from a palette, cycled across the room's lights.
    private static object Xy((double X, double Y) c, int bri) => new { on = true, bri, xy = new[] { c.X, c.Y } };

    private static readonly (double X, double Y)[] SavannaSunset =
        { (0.5928, 0.3475), (0.6009, 0.3479), (0.5346, 0.4137), (0.4576, 0.4210) };
    private static readonly (double X, double Y)[] TropicalTwilight =
        { (0.4136, 0.1937), (0.5133, 0.3269), (0.1783, 0.2417), (0.3187, 0.1568) };
    private static readonly (double X, double Y)[] ArcticAurora =
        { (0.1611, 0.3550), (0.1712, 0.4577), (0.2137, 0.2657), (0.1626, 0.4180) };
    private static readonly (double X, double Y)[] SpringBlossom =
        { (0.3852, 0.2669), (0.4152, 0.3229), (0.3399, 0.2140), (0.4574, 0.3419) };

    public static IReadOnlyList<ScenePreset> All { get; } = new[]
    {
        new ScenePreset("Relax", _ => Ct(447, 144)),
        new ScenePreset("Read", _ => Ct(346, 240)),
        new ScenePreset("Concentrate", _ => Ct(233, 254)),
        new ScenePreset("Energize", _ => Ct(156, 254)),
        new ScenePreset("Nightlight", _ => Ct(500, 1)),
        new ScenePreset("Bright", _ => Ct(367, 254)),
        new ScenePreset("Dimmed", _ => Ct(367, 77)),
        new ScenePreset("Savanna sunset", i => Xy(SavannaSunset[i % SavannaSunset.Length], 200)),
        new ScenePreset("Tropical twilight", i => Xy(TropicalTwilight[i % TropicalTwilight.Length], 200)),
        new ScenePreset("Arctic aurora", i => Xy(ArcticAurora[i % ArcticAurora.Length], 200)),
        new ScenePreset("Spring blossom", i => Xy(SpringBlossom[i % SpringBlossom.Length], 200)),
    };

    /// <summary>Creates the preset as a new GroupScene in the room and stores its per-light state.</summary>
    public static async Task CreateAsync(HueApiClient client, ScenePreset preset, string groupId, CancellationToken ct = default)
    {
        var groups = await client.GetGroupsAsync(ct);
        if (!groups.TryGetValue(groupId, out var group))
            return;

        string sceneId = await client.CreateSceneAsync(
            new { name = preset.Name, group = groupId, type = "GroupScene", recycle = false }, ct);

        for (int i = 0; i < group.Lights.Count; i++)
            await client.SetSceneLightStateAsync(sceneId, group.Lights[i], preset.StateFor(i), ct);
    }
}
