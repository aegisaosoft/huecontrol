// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows;
using HueControl.Models;
using HueControl.ViewModels;

namespace HueControl;

/// <summary>Interaction logic for AddBridgeWindow.xaml.</summary>
public partial class AddBridgeWindow : Window
{
    private readonly AddBridgeViewModel _viewModel;

    public AddBridgeWindow(IEnumerable<string> homeNames)
    {
        InitializeComponent();
        _viewModel = new AddBridgeViewModel(homeNames);
        _viewModel.PairingSucceeded += OnPairingSucceeded;
        DataContext = _viewModel;
    }

    /// <summary>The paired bridge and target home, valid only when <see cref="Window.DialogResult"/> is true.</summary>
    public AddBridgeResult? Result =>
        _viewModel.Result is null ? null : new AddBridgeResult(_viewModel.Result, _viewModel.HomeName);

    private void OnPairingSucceeded(object? sender, EventArgs e)
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
