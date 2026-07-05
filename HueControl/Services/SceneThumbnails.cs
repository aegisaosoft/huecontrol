// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Media;

namespace HueControl.Services;

/// <summary>
/// Builds our own gradient thumbnail for a scene from its colours (keyed by the standard
/// Hue scene names). These are original renders — not Signify's proprietary scene photos.
/// </summary>
public static class SceneThumbnails
{
    private static Color C(string hex) => (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)!;

    private static readonly Dictionary<string, Color[]> Palettes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Relax"] = new[] { C("#FF9E5C"), C("#FFB877") },
        ["Read"] = new[] { C("#FFE0BE"), C("#FFEFD8") },
        ["Concentrate"] = new[] { C("#E8F0FF"), C("#CFE0FF") },
        ["Energize"] = new[] { C("#DCEBFF"), C("#A9CCFF") },
        ["Nightlight"] = new[] { C("#7A4A12"), C("#E8A44E") },
        ["Bright"] = new[] { C("#FFF3D8"), C("#FFFFFF") },
        ["Dimmed"] = new[] { C("#8A6A3A"), C("#C89A5A") },
        ["Savanna sunset"] = new[] { C("#FF6A3D"), C("#FFA24B"), C("#FFD27A") },
        ["Tropical twilight"] = new[] { C("#6A3FD6"), C("#E14B9B"), C("#2FB6C0") },
        ["Arctic aurora"] = new[] { C("#2FC0A0"), C("#3FD0E0"), C("#4A6BFF") },
        ["Spring blossom"] = new[] { C("#FF9EC4"), C("#FFC4D6"), C("#FFE0EC") },
        ["Blossom"] = new[] { C("#FF7EB0"), C("#FFB0D0") },
        ["Amber bloom"] = new[] { C("#FF8A3D"), C("#FFB24B") },
        ["Nighttime"] = new[] { C("#1A2A6C"), C("#3A4A8C") },
        ["Dreamy dusk"] = new[] { C("#6A3FD6"), C("#B06BFF") },
        ["Pensive"] = new[] { C("#8A7AB0"), C("#B0A0D0") },
        ["Baby's breath"] = new[] { C("#FFF0F5"), C("#FFE0EC") },
        ["Sunset"] = new[] { C("#FF7A3D"), C("#FFB24B") },
        ["Ski"] = new[] { C("#CFE8FF"), C("#8FBEE8") },
        ["Beach"] = new[] { C("#FFE9B0"), C("#7FD0E0") },
    };

    private static readonly Color[] Default = { C("#7C4DFF"), C("#B06BFF") };

    /// <summary>A frozen diagonal gradient brush for the scene with the given name.</summary>
    public static Brush ForName(string name)
    {
        Color[] colors = MatchPalette(name);
        var brush = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(1, 1) };
        for (int i = 0; i < colors.Length; i++)
        {
            double offset = colors.Length == 1 ? 0 : (double)i / (colors.Length - 1);
            brush.GradientStops.Add(new GradientStop(colors[i], offset));
        }
        brush.Freeze();
        return brush;
    }

    private static Color[] MatchPalette(string name)
    {
        if (Palettes.TryGetValue(name, out Color[]? exact))
            return exact;

        // Loose match: a scene may be "Savanna sunset 2" or "Relax on".
        foreach (var (key, colors) in Palettes)
        {
            if (name.StartsWith(key, StringComparison.OrdinalIgnoreCase)
                || name.Contains(key, StringComparison.OrdinalIgnoreCase))
                return colors;
        }
        return Default;
    }
}
