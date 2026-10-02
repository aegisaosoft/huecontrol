// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.IO;
using System.Text.Json;

namespace HueControl.Services;

/// <summary>
/// App-wide, bridge-independent preferences (e.g. background auto-refresh), persisted to
/// the user's AppData folder. Static like <see cref="ThemeManager"/> so any view model can
/// read the current values and subscribe to <see cref="Changed"/> to react at runtime.
/// </summary>
public static class AppPreferences
{
    /// <summary>Smallest and largest auto-refresh intervals the UI offers, in seconds.</summary>
    public const int MinRefreshSeconds = 3;
    public const int MaxRefreshSeconds = 300;

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HueControl", "preferences.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Raised whenever a preference changes, so live consumers can re-read the values.</summary>
    public static event Action? Changed;

    /// <summary>When true, the app polls the bridge in the background to keep state current.</summary>
    public static bool AutoRefreshEnabled { get; private set; } = true;

    /// <summary>Interval between background refreshes, in seconds.</summary>
    public static int AutoRefreshSeconds { get; private set; } = 8;

    static AppPreferences() => Load();

    /// <summary>Updates the auto-refresh preference, persists it, and notifies subscribers.</summary>
    public static void SetAutoRefresh(bool enabled, int seconds)
    {
        seconds = Math.Clamp(seconds, MinRefreshSeconds, MaxRefreshSeconds);
        if (AutoRefreshEnabled == enabled && AutoRefreshSeconds == seconds)
            return;

        AutoRefreshEnabled = enabled;
        AutoRefreshSeconds = seconds;
        Save();
        Changed?.Invoke();
    }

    private static void Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return;

            var dto = JsonSerializer.Deserialize<PreferencesDto>(File.ReadAllText(FilePath));
            if (dto is null)
                return;

            AutoRefreshEnabled = dto.AutoRefreshEnabled;
            AutoRefreshSeconds = Math.Clamp(dto.AutoRefreshSeconds, MinRefreshSeconds, MaxRefreshSeconds);
        }
        catch
        {
            // Fall back to defaults if the file is missing or unreadable.
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var dto = new PreferencesDto
            {
                AutoRefreshEnabled = AutoRefreshEnabled,
                AutoRefreshSeconds = AutoRefreshSeconds,
            };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Persisting preferences is best-effort.
        }
    }

    private sealed class PreferencesDto
    {
        public bool AutoRefreshEnabled { get; set; } = true;
        public int AutoRefreshSeconds { get; set; } = 8;
    }
}
