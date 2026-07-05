// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HueControl.Models;
using HueControl.ViewModels;
using WpfColor = System.Windows.Media.Color;
using WinFormsColorDialog = System.Windows.Forms.ColorDialog;
using DialogResultWinForms = System.Windows.Forms.DialogResult;

namespace HueControl;

/// <summary>Interaction logic for MainWindow.xaml.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        // The view supplies UI-only services the view model cannot own directly.
        _viewModel.ColorPicker = PickColor;
        _viewModel.AddBridgeDialog = ShowAddBridgeDialog;
        _viewModel.AskName = (title, initial) => InputDialog.Ask(this, title, "Name", initial);
        _viewModel.PickScene = () => ScenePickerDialog.Pick(this);
        _viewModel.Confirm = message =>
            MessageBox.Show(this, message, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes;

        DataContext = _viewModel;

        StateChanged += (_, _) => UpdateMaxRestoreIcon();
        UpdateMaxRestoreIcon();
    }

    private void UpdateMaxRestoreIcon()
    {
        bool maximized = WindowState == WindowState.Maximized;
        MaxIcon.Visibility = maximized ? Visibility.Collapsed : Visibility.Visible;
        RestoreIcon.Visibility = maximized ? Visibility.Visible : Visibility.Collapsed;
    }

    private WpfColor? PickColor(WpfColor initial)
    {
        using var dialog = new WinFormsColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(initial.R, initial.G, initial.B),
        };

        if (dialog.ShowDialog() != DialogResultWinForms.OK)
            return null;

        return WpfColor.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
    }

    private AddBridgeResult? ShowAddBridgeDialog()
    {
        var window = new AddBridgeWindow(_viewModel.HomeNames) { Owner = this };
        return window.ShowDialog() == true ? window.Result : null;
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximizeRestore(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnToggleTheme(object sender, RoutedEventArgs e) => Services.ThemeManager.Toggle();

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        ViewModels.SettingsViewModel? settings = _viewModel.CreateSettings(message =>
            MessageBox.Show(this, message, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes);

        if (settings is null)
        {
            MessageBox.Show(this, "Add and select a bridge first.", "Settings",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        new SettingsWindow(settings) { Owner = this }.ShowDialog();

        // Device changes made in Settings should show up in the main view.
        _viewModel.RefreshCommand.Execute(null);
    }

    // ===== Left bridges flyout =====

    private bool _menuOpen;

    private void OnToggleMenu(object sender, RoutedEventArgs e)
    {
        if (_menuOpen) CloseMenu();
        else OpenMenu();
    }

    private void OnCloseMenu(object sender, MouseButtonEventArgs e) => CloseMenu();

    private void OpenMenu()
    {
        _menuOpen = true;
        Scrim.Visibility = Visibility.Visible;
        AnimateScrim(1.0);
        AnimateFlyout(0);
    }

    private void CloseMenu()
    {
        if (!_menuOpen)
            return;

        _menuOpen = false;
        AnimateScrim(0.0);
        AnimateFlyout(-300, () => Scrim.Visibility = Visibility.Collapsed);
    }

    private void AnimateFlyout(double toX, Action? completed = null)
    {
        var anim = new DoubleAnimation(toX, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        if (completed is not null)
            anim.Completed += (_, _) => completed();
        FlyoutTransform.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    private void AnimateScrim(double toOpacity)
        => Scrim.BeginAnimation(OpacityProperty, new DoubleAnimation(toOpacity, TimeSpan.FromMilliseconds(180)));
}
