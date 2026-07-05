// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows;
using HueControl.ViewModels;

namespace HueControl;

/// <summary>Interaction logic for SettingsWindow.xaml.</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
