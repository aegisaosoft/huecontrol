// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>
/// Settings section "Main": app-wide preferences that are not tied to a specific bridge,
/// such as background auto-refresh. Changes are persisted immediately via
/// <see cref="AppPreferences"/> and picked up live by the main window's poll timer.
/// </summary>
public sealed class MainSectionViewModel : ObservableObject
{
    private bool _autoRefreshEnabled;
    private int _refreshSeconds;

    public MainSectionViewModel()
    {
        _autoRefreshEnabled = AppPreferences.AutoRefreshEnabled;
        _refreshSeconds = AppPreferences.AutoRefreshSeconds;
    }

    public string Title => Loc.T("Section_Main");

    /// <summary>When off, the app never polls the bridge on its own — only manual Refresh updates state.</summary>
    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set
        {
            if (SetProperty(ref _autoRefreshEnabled, value))
                Persist();
        }
    }

    public int RefreshSeconds
    {
        get => _refreshSeconds;
        set
        {
            if (SetProperty(ref _refreshSeconds, value))
            {
                OnPropertyChanged(nameof(RefreshLabel));
                Persist();
            }
        }
    }

    /// <summary>Localized caption under the interval slider, e.g. "Check every 9 seconds".</summary>
    public string RefreshLabel => Loc.T("AutoRefresh_Every_Fmt", _refreshSeconds);

    private void Persist() => AppPreferences.SetAutoRefresh(_autoRefreshEnabled, _refreshSeconds);
}
