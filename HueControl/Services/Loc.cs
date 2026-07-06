// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace HueControl.Services;

/// <summary>
/// Runtime UI localization. Exposes an indexer so XAML can bind through the
/// <c>{loc:Tr Key}</c> markup extension and refresh live when the language changes.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    /// <summary>Supported languages, in the same order as the translation arrays in <see cref="Strings"/>.</summary>
    public static readonly (string Code, string Name)[] Languages =
    {
        ("en", "English"),
        ("es", "Español"),
        ("pt", "Português"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("ru", "Русский"),
        ("ar", "العربية"),
        ("he", "עברית"),
        ("zh-Hans", "简体中文"),
        ("zh-Hant", "繁體中文"),
    };

    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HueControl", "lang.txt");

    public static Loc Instance { get; } = new();

    private int _idx;

    private Loc()
    {
        _idx = IndexOf(LoadSaved() ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        ApplyCulture();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The active language code (e.g. "en").</summary>
    public string Current => Languages[_idx].Code;

    /// <summary>Layout direction for the active language (RTL for Arabic and Hebrew).</summary>
    public System.Windows.FlowDirection FlowDirection =>
        Current is "ar" or "he"
            ? System.Windows.FlowDirection.RightToLeft
            : System.Windows.FlowDirection.LeftToRight;

    /// <summary>Binding entry point: <c>Instance[key]</c> returns the localized string.</summary>
    public string this[string key] => Strings.Get(key, _idx);

    /// <summary>
    /// Sets the window's layout direction to match the current language and keeps it in sync
    /// when the language changes. Call once from each window's constructor.
    /// </summary>
    public static void ApplyFlowDirection(System.Windows.Window window)
    {
        window.FlowDirection = Instance.FlowDirection;

        void Handler(object? _, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FlowDirection))
                window.FlowDirection = Instance.FlowDirection;
        }

        Instance.PropertyChanged += Handler;
        window.Closed += (_, _) => Instance.PropertyChanged -= Handler;
    }

    /// <summary>Localized string for a key.</summary>
    public static string T(string key) => Instance[key];

    /// <summary>Localized, formatted string for a key with placeholders.</summary>
    public static string T(string key, params object?[] args) => string.Format(Instance[key], args);

    /// <summary>Switches the UI language and refreshes every bound string.</summary>
    public void SetLanguage(string code)
    {
        int i = IndexOf(code);
        if (i == _idx)
            return;

        _idx = i;
        ApplyCulture();
        Save(Languages[_idx].Code);

        // "Item[]" refreshes all indexer bindings created by the Tr markup extension.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowDirection)));
    }

    private void ApplyCulture()
    {
        var culture = CultureInfo.GetCultureInfo(Languages[_idx].Code);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private static int IndexOf(string code)
    {
        int i = Array.FindIndex(Languages, l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
        return i < 0 ? 0 : i;
    }

    private static string? LoadSaved()
    {
        try
        {
            return File.Exists(StatePath) ? File.ReadAllText(StatePath).Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static void Save(string code)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            File.WriteAllText(StatePath, code);
        }
        catch
        {
            // Persisting the language is best-effort; ignore IO failures.
        }
    }
}
