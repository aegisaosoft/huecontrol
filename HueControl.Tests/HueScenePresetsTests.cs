// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Net;
using System.Net.Http;
using HueControl.Services;

namespace HueControl.Tests;

public class HueScenePresetsTests
{
    [Fact]
    public async Task CreateAsync_CreatesGroupScene_AndStoresPerLightState()
    {
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            string path = req.RequestUri!.PathAndQuery;
            if (req.Method == HttpMethod.Get && path.EndsWith("/groups"))
                return (HttpStatusCode.OK, "{\"7\":{\"name\":\"Office\",\"type\":\"Room\",\"lights\":[\"14\",\"15\"]}}");
            if (req.Method == HttpMethod.Post && path.EndsWith("/scenes"))
                return (HttpStatusCode.OK, "[{\"success\":{\"id\":\"newscene\"}}]");
            return (HttpStatusCode.OK, "[{\"success\":{}}]"); // PUT lightstates
        });
        var client = new HueApiClient("1.2.3.4", "key", handler);
        ScenePreset relax = HueScenePresets.All.First(p => p.Name == "Relax");

        await HueScenePresets.CreateAsync(client, relax, "7");

        var post = handler.Requests.Single(r => r.Method == HttpMethod.Post);
        Assert.Equal("/api/key/scenes", post.Path);
        Assert.Contains("\"group\":\"7\"", post.Body);
        Assert.Contains("\"type\":\"GroupScene\"", post.Body);

        // One stored light-state per room light, with the recipe's colour temperature.
        var puts = handler.Requests.Where(r => r.Method == HttpMethod.Put).ToList();
        Assert.Equal(2, puts.Count);
        Assert.Equal("/api/key/scenes/newscene/lightstates/14", puts[0].Path);
        Assert.Equal("/api/key/scenes/newscene/lightstates/15", puts[1].Path);
        Assert.Contains("\"ct\":447", puts[0].Body);
    }

    [Fact]
    public void Presets_IncludeStandardHueScenes()
    {
        var names = HueScenePresets.All.Select(p => p.Name).ToList();
        Assert.Contains("Relax", names);
        Assert.Contains("Concentrate", names);
        Assert.Contains("Energize", names);
        Assert.Contains("Savanna sunset", names);
    }
}
