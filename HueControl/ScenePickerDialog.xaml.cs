// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows;
using HueControl.Services;

namespace HueControl;

/// <summary>A small themed dialog to pick a built-in Hue scene to add to a room.</summary>
public partial class ScenePickerDialog : Window
{
    public ScenePickerDialog()
    {
        InitializeComponent();
        Presets.ItemsSource = HueScenePresets.All;
        Presets.SelectedItem = HueScenePresets.All.FirstOrDefault();
    }

    /// <summary>The chosen preset, valid only when <see cref="Window.DialogResult"/> is true.</summary>
    public ScenePreset? Selected => Presets.SelectedItem as ScenePreset;

    /// <summary>Shows the dialog and returns the chosen preset, or null if cancelled.</summary>
    public static ScenePreset? Pick(Window owner)
    {
        var dialog = new ScenePickerDialog { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.Selected : null;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
