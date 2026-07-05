// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Net;
using System.Net.Http;
using HueControl.Services;

namespace HueControl.Tests;

public class SwitchProgrammerTests
{
    [Fact]
    public async Task ConfigureRoom_DeletesOnlyThisSwitchsRules_AndCreatesFourRules()
    {
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            string path = req.RequestUri!.PathAndQuery;
            if (req.Method == HttpMethod.Get && path.EndsWith("/scenes"))
            {
                return (HttpStatusCode.OK,
                    "{\"sA\":{\"name\":\"Bright\",\"type\":\"GroupScene\",\"group\":\"7\"}," +
                    "\"sB\":{\"name\":\"Relax\",\"type\":\"GroupScene\",\"group\":\"7\"}," +
                    "\"sC\":{\"name\":\"Other\",\"type\":\"GroupScene\",\"group\":\"9\"}}");
            }
            if (req.Method == HttpMethod.Get && path.EndsWith("/rules"))
            {
                return (HttpStatusCode.OK,
                    "{\"r1\":{\"name\":\"old on\",\"conditions\":[{\"address\":\"/sensors/3/state/buttonevent\"}]}," +
                    "\"r2\":{\"name\":\"other switch\",\"conditions\":[{\"address\":\"/sensors/9/state/buttonevent\"}]}}");
            }
            return (HttpStatusCode.OK, "[{\"success\":{}}]");
        });
        var client = new HueApiClient("1.2.3.4", "key", handler);

        await SwitchProgrammer.ConfigureRoomAsync(client, "3", "7");

        // Only the rule belonging to sensor 3 is deleted (sensor 9 untouched).
        var deletes = handler.Requests.Where(r => r.Method == HttpMethod.Delete).ToList();
        Assert.Single(deletes);
        Assert.Equal("/api/key/rules/r1", deletes[0].Path);

        // Four button rules are created.
        var creates = handler.Requests.Where(r => r.Method == HttpMethod.Post && r.Path == "/api/key/rules").ToList();
        Assert.Equal(4, creates.Count);

        // ON (1002) recalls the room's first scene (Bright = sA), OFF (4002) turns off.
        Assert.Contains(creates, c => c.Body.Contains("\"1002\"") && c.Body.Contains("\"scene\":\"sA\""));
        Assert.Contains(creates, c => c.Body.Contains("\"4002\"") && c.Body.Contains("\"on\":false"));
        Assert.Contains(creates, c => c.Body.Contains("\"2002\"") && c.Body.Contains("bri_inc"));
        Assert.Contains(creates, c => c.Body.Contains("\"3002\""));
    }

    [Fact]
    public async Task ConfigureRoom_NoScenes_UsesPlainOn()
    {
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            string path = req.RequestUri!.PathAndQuery;
            if (req.Method == HttpMethod.Get && (path.EndsWith("/scenes") || path.EndsWith("/rules")))
                return (HttpStatusCode.OK, "{}");
            return (HttpStatusCode.OK, "[{\"success\":{}}]");
        });
        var client = new HueApiClient("1.2.3.4", "key", handler);

        await SwitchProgrammer.ConfigureRoomAsync(client, "5", "8");

        var creates = handler.Requests.Where(r => r.Method == HttpMethod.Post).ToList();
        Assert.Equal(4, creates.Count);
        Assert.Contains(creates, c => c.Body.Contains("\"1002\"") && c.Body.Contains("\"on\":true"));
    }
}
