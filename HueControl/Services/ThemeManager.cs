// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.IO;
using System.Windows;

namespace HueControl.Services;

/// <summary>Swaps the active theme palette (dark/light) at runtime and remembers the choice.</summary>
public static class ThemeManager
{
    public enum AppTheme { Dark, Light }

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HueControl", "theme.txt");

    public static AppTheme Current { get; private set; } = AppTheme.Dark;

    /// <summary>Applies a theme by swapping the first merged resource dictionary.</summary>
    public static void Apply(AppTheme theme)
    {
        var dict = new ResourceDictionary { Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative) };
        var merged = Application.Current.Resources.MergedDictionaries;
        if (merged.Count == 0)
            merged.Add(dict);
        else
            merged[0] = dict;

        Current = theme;
        Save(theme);
    }

    public static void Toggle() => Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);

    public static AppTheme LoadSaved()
    {
        try { return Enum.Parse<AppTheme>(File.ReadAllText(FilePath).Trim()); }
        catch { return AppTheme.Dark; }
    }

    private static void Save(AppTheme theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, theme.ToString());
        }
        catch
        {
            // Persisting the theme preference is best-effort.
        }
    }
}
