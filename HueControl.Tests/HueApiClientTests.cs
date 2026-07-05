// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Net;
using System.Net.Http;
using HueControl.Services;

namespace HueControl.Tests;

public class HueApiClientTests
{
    private const string Key = "testkey";

    [Fact]
    public async Task Pair_ReturnsUsername_OnSuccess()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            (HttpStatusCode.OK, "[{\"success\":{\"username\":\"abc123\"}}]"));

        string key = await HueApiClient.PairAsync("1.2.3.4", handler);

        Assert.Equal("abc123", key);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/api", handler.Requests[0].Path);
    }

    [Fact]
    public async Task Pair_Throws_WhenLinkButtonNotPressed()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            (HttpStatusCode.OK, "[{\"error\":{\"type\":101,\"description\":\"link button not pressed\"}}]"));

        await Assert.ThrowsAsync<LinkButtonNotPressedException>(
            () => HueApiClient.PairAsync("1.2.3.4", handler));
    }

    [Fact]
    public async Task GetLights_ParsesDictionary()
    {
        const string json = """
        {
          "2": { "state": { "on": true, "bri": 200, "reachable": true },
                 "type": "Extended color light", "name": "Right Lamp",
                 "modelid": "LCT007", "uniqueid": "00:17:88:01:10:4d:27:b8-0b" },
          "27": { "state": { "on": false, "bri": 100, "reachable": true },
                  "type": "Color temperature light", "name": "Office lamp 1",
                  "modelid": "LTA012", "uniqueid": "00:17:88:01:0e:f7:37:7c-0b" }
        }
        """;
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, json));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        var lights = await client.GetLightsAsync();

        Assert.Equal(2, lights.Count);
        Assert.Equal("Right Lamp", lights["2"].Name);
        Assert.True(lights["2"].State.On);
        Assert.True(lights["2"].SupportsColor);
        Assert.False(lights["27"].SupportsColor);
        Assert.Equal($"/api/{Key}/lights", handler.Requests[0].Path);
    }

    [Fact]
    public async Task GetLights_Throws_OnErrorArray()
    {
        // An invalid/expired key returns an array with an "error" element.
        var handler = new StubHttpMessageHandler((_, _) =>
            (HttpStatusCode.OK, "[{\"error\":{\"type\":1,\"description\":\"unauthorized user\"}}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetLightsAsync());
    }

    [Fact]
    public async Task ActivateScene_NoGroup_PutsToGroupZero()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.ActivateSceneAsync("scene-xyz", null);

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, req.Method);
        Assert.Equal($"/api/{Key}/groups/0/action", req.Path);
        Assert.Contains("\"scene\":\"scene-xyz\"", req.Body);
    }

    [Fact]
    public async Task ActivateScene_WithGroup_PutsToThatGroup()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.ActivateSceneAsync("s1", "7");

        var req = Assert.Single(handler.Requests);
        Assert.Equal($"/api/{Key}/groups/7/action", req.Path);
    }

    [Fact]
    public async Task StartLightSearch_PostsToLights()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{\"success\":{}}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.StartLightSearchAsync();

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal($"/api/{Key}/lights", req.Path);
    }

    [Fact]
    public async Task GetNewLights_ParsesLastScanAndDevices()
    {
        const string json = "{\"lastscan\":\"active\",\"7\":{\"name\":\"Hue lamp 7\"}}";
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, json));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        NewDevicesScan scan = await client.GetNewLightsAsync();

        Assert.Equal("active", scan.LastScan);
        var device = Assert.Single(scan.Devices);
        Assert.Equal("7", device.Id);
        Assert.Equal("Hue lamp 7", device.Name);
        Assert.Equal($"/api/{Key}/lights/new", handler.Requests[0].Path);
    }

    [Fact]
    public async Task DeleteSensor_DeletesSensorPath()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{\"success\":\"...\"}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.DeleteSensorAsync("3");

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, req.Method);
        Assert.Equal($"/api/{Key}/sensors/3", req.Path);
    }

    [Fact]
    public async Task RenameLight_PutsNameToLightPath()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{\"success\":{}}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.RenameLightAsync("5", "Desk lamp");

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, req.Method);
        Assert.Equal($"/api/{Key}/lights/5", req.Path);
        Assert.Contains("\"name\":\"Desk lamp\"", req.Body);
    }

    [Fact]
    public async Task CreateGroupScene_PostsSceneWithGroup()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{\"success\":{\"id\":\"s99\"}}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.CreateGroupSceneAsync("Movie", "7");

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal($"/api/{Key}/scenes", req.Path);
        Assert.Contains("\"name\":\"Movie\"", req.Body);
        Assert.Contains("\"group\":\"7\"", req.Body);
        Assert.Contains("\"type\":\"GroupScene\"", req.Body);
    }

    [Fact]
    public async Task CreateGroup_Room_PostsRoomWithClass()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{\"success\":{\"id\":\"9\"}}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.CreateGroupAsync("Den", "Room", new[] { "2", "3" });

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal($"/api/{Key}/groups", req.Path);
        Assert.Contains("\"type\":\"Room\"", req.Body);
        Assert.Contains("\"class\":\"Other\"", req.Body);
    }

    [Fact]
    public async Task SetGroupLights_PutsLightsToGroup()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{\"success\":{}}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.SetGroupLightsAsync("7", new[] { "14", "15" });

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, req.Method);
        Assert.Equal($"/api/{Key}/groups/7", req.Path);
        Assert.Contains("\"lights\":[\"14\",\"15\"]", req.Body);
    }

    [Fact]
    public async Task DeleteScene_DeletesScenePath()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{\"success\":\"...\"}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.DeleteSceneAsync("abc");

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, req.Method);
        Assert.Equal($"/api/{Key}/scenes/abc", req.Path);
    }

    [Fact]
    public async Task SetLightState_PutsToLightStatePath()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{}]"));
        var client = new HueApiClient("1.2.3.4", Key, handler);

        await client.SetLightStateAsync("5", new { on = true, bri = 128 });

        var req = Assert.Single(handler.Requests);
        Assert.Equal($"/api/{Key}/lights/5/state", req.Path);
        Assert.Contains("\"bri\":128", req.Body);
    }
}
