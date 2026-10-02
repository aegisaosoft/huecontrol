// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace HueControl.Tests;

/// <summary>
/// The Store name (reserved in Partner Center) must be what the user sees in the app;
/// a mismatch is a certification flag. Guards the manifest and MainWindow from drifting.
/// </summary>
public class StoreBrandingTests
{
    private const string StoreName = "Light Control for Hue";

    [Fact]
    public void Manifest_DisplayNames_Match_StoreName()
    {
        XDocument manifest = XDocument.Load(Path.Combine(RepoRoot(), "StoreAssets", "Package.appxmanifest"));
        XNamespace foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XNamespace uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";

        Assert.Equal(StoreName, manifest.Descendants(foundation + "DisplayName").Single().Value);
        Assert.Equal(StoreName, (string?)manifest.Descendants(uap + "VisualElements").Single().Attribute("DisplayName"));
    }

    [Fact]
    public void MainWindow_Title_Matches_StoreName()
    {
        XDocument xaml = XDocument.Load(Path.Combine(RepoRoot(), "HueControl", "MainWindow.xaml"));

        Assert.Equal(StoreName, (string?)xaml.Root!.Attribute("Title"));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HueControl.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("HueControl.slnx not found above the test output.");
    }
}
