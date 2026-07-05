// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.IO;
using System.Text.Json;
using HueControl.Models;

namespace HueControl.Services;

/// <summary>Persists homes (each with its bridges) to the user's AppData folder.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _bridgesPath;
    private readonly string _homesPath;

    public SettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HueControl"))
    {
    }

    // Test seam: lets tests point the store at an isolated directory.
    internal SettingsStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _bridgesPath = Path.Combine(directory, "bridges.json");
        _homesPath = Path.Combine(directory, "homes.json");
    }

    /// <summary>Loads homes, migrating a legacy flat bridge list into a default home if needed.</summary>
    public List<Home> LoadHomes()
    {
        try
        {
            if (File.Exists(_homesPath))
            {
                return JsonSerializer.Deserialize<List<Home>>(File.ReadAllText(_homesPath)) ?? new List<Home>();
            }

            // Migrate the old bridges.json into a single "My Home".
            List<SavedBridge> legacy = Load();
            if (legacy.Count > 0)
            {
                var home = new Home { Name = "My Home", Bridges = legacy };
                SaveHomes(new[] { home });
                return new List<Home> { home };
            }

            return new List<Home>();
        }
        catch
        {
            return new List<Home>();
        }
    }

    public void SaveHomes(IEnumerable<Home> homes)
        => File.WriteAllText(_homesPath, JsonSerializer.Serialize(homes, JsonOptions));

    // ---- Legacy flat bridge list (kept for migration and tests) ----

    public List<SavedBridge> Load()
    {
        try
        {
            if (!File.Exists(_bridgesPath))
                return new List<SavedBridge>();

            string json = File.ReadAllText(_bridgesPath);
            return JsonSerializer.Deserialize<List<SavedBridge>>(json) ?? new List<SavedBridge>();
        }
        catch
        {
            return new List<SavedBridge>();
        }
    }

    public void Save(IEnumerable<SavedBridge> bridges)
        => File.WriteAllText(_bridgesPath, JsonSerializer.Serialize(bridges, JsonOptions));
}
