// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>Container for the Settings window: hosts the section list and the active section.</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private object _selectedSection;

    public SettingsViewModel(string bridgeName, HueApiClient client, Func<string, bool> confirm)
    {
        BridgeName = bridgeName;
        Sections = new ObservableCollection<object>
        {
            new MainSectionViewModel(),
            new DevicesSectionViewModel(client, confirm),
            new AccessoriesSectionViewModel(client, confirm),
            new RoomsSectionViewModel(client, confirm),
            new ScenesSectionViewModel(client, confirm),
        };
        _selectedSection = Sections[0];
    }

    public string BridgeName { get; }

    /// <summary>Localized window header, e.g. "Settings — Hue Bridge Pro".</summary>
    public string Header => Loc.T("Settings_Header", BridgeName);

    public ObservableCollection<object> Sections { get; }

    public object SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }
}
