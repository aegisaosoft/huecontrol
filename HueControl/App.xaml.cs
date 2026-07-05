// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows;
using HueControl.Services;

namespace HueControl;

/// <summary>Interaction logic for App.xaml.</summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.Apply(ThemeManager.LoadSaved());
    }
}
