// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows;
using System.Windows.Input;

namespace HueControl;

/// <summary>A small themed prompt for a single line of text (e.g. a home name).</summary>
public partial class InputDialog : Window
{
    public InputDialog(string title, string prompt, string initial = "")
    {
        InitializeComponent();
        Services.Loc.ApplyFlowDirection(this);
        TitleText.Text = title;
        PromptText.Text = prompt;
        Input.Text = initial;
        Loaded += (_, _) => { Input.Focus(); Input.SelectAll(); };
    }

    /// <summary>The entered text, valid only when <see cref="Window.DialogResult"/> is true.</summary>
    public string Value => Input.Text.Trim();

    /// <summary>Shows the dialog and returns the trimmed text, or null if cancelled/empty.</summary>
    public static string? Ask(Window owner, string title, string prompt, string initial = "")
    {
        var dialog = new InputDialog(title, prompt, initial) { Owner = owner };
        return dialog.ShowDialog() == true && dialog.Value.Length > 0 ? dialog.Value : null;
    }

    private void OnInputKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnOk(sender, e);
        else if (e.Key == Key.Escape)
            OnCancel(sender, e);
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
