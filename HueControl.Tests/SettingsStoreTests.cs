// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.IO;
using HueControl.Models;
using HueControl.Services;

namespace HueControl.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _dir;

    public SettingsStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "HueControlTests_" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Load_EmptyDirectory_ReturnsEmptyList()
    {
        var store = new SettingsStore(_dir);
        Assert.Empty(store.Load());
    }

    [Fact]
    public void SaveThenLoad_RoundTripsBridges()
    {
        var store = new SettingsStore(_dir);
        var bridges = new List<SavedBridge>
        {
            new() { Id = "b1", Name = "Bridge Pro", IpAddress = "192.168.1.121", AppKey = "key1" },
            new() { Id = "b2", Name = "Old Bridge", IpAddress = "192.168.1.130", AppKey = "key2" },
        };

        store.Save(bridges);
        var loaded = new SettingsStore(_dir).Load();

        Assert.Equal(2, loaded.Count);
        Assert.Equal("Bridge Pro", loaded[0].Name);
        Assert.Equal("192.168.1.130", loaded[1].IpAddress);
        Assert.Equal("key2", loaded[1].AppKey);
    }

    [Fact]
    public void LoadHomes_MigratesLegacyBridgesIntoMyHome()
    {
        var store = new SettingsStore(_dir);
        store.Save(new List<SavedBridge>
        {
            new() { Name = "Bridge Pro", IpAddress = "192.168.1.121", AppKey = "k1" },
        });

        var homes = store.LoadHomes();

        var home = Assert.Single(homes);
        Assert.Equal("My Home", home.Name);
        var bridge = Assert.Single(home.Bridges);
        Assert.Equal("192.168.1.121", bridge.IpAddress);
    }

    [Fact]
    public void SaveHomesThenLoad_RoundTrips()
    {
        var store = new SettingsStore(_dir);
        var homes = new List<Home>
        {
            new() { Name = "House", Bridges = { new SavedBridge { Name = "A", IpAddress = "10.0.0.1", AppKey = "x" } } },
            new() { Name = "Cabin", Bridges = { new SavedBridge { Name = "B", IpAddress = "10.0.0.2", AppKey = "y" } } },
        };

        store.SaveHomes(homes);
        var loaded = new SettingsStore(_dir).LoadHomes();

        Assert.Equal(2, loaded.Count);
        Assert.Equal("House", loaded[0].Name);
        Assert.Equal("10.0.0.2", loaded[1].Bridges[0].IpAddress);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
