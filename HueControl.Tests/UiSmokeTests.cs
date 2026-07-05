// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Threading;
using System.Windows;

namespace HueControl.Tests;

/// <summary>
/// Instantiates every window on an STA thread to prove the XAML parses and all
/// StaticResource references resolve — the regression guard for missing resources.
/// </summary>
public class UiSmokeTests
{
    [Fact]
    public void MainWindow_And_AddBridgeWindow_Construct_WithoutXamlErrors()
    {
        Exception? captured = RunOnStaThread(() =>
        {
            EnsureApplicationResources();
            _ = new HueControl.MainWindow();
            _ = new HueControl.AddBridgeWindow(System.Array.Empty<string>());
            _ = new HueControl.InputDialog("Title", "Prompt", "initial");

            var handler = new StubHttpMessageHandler((_, _) => (System.Net.HttpStatusCode.OK, "{}"));
            var client = new HueControl.Services.HueApiClient("1.2.3.4", "key", handler);
            var settings = new HueControl.ViewModels.SettingsViewModel("Test bridge", client, _ => false);
            _ = new HueControl.SettingsWindow(settings);
        });

        Assert.Null(captured);
    }

    private static void EnsureApplicationResources()
    {
        if (Application.Current is null)
        {
            var app = new HueControl.App();
            app.InitializeComponent(); // merges App.xaml resources into Application.Current.
        }
    }

    private static Exception? RunOnStaThread(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        })
        {
            IsBackground = true,
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(30));
        return captured;
    }
}
