// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Net;
using System.Net.Http;
using System.Windows.Media;
using HueControl.Models;
using HueControl.Services;
using HueControl.ViewModels;

namespace HueControl.Tests;

public class ViewModelTests
{
    private static (HueApiClient Client, StubHttpMessageHandler Handler) MakeClient()
    {
        var handler = new StubHttpMessageHandler((_, _) => (HttpStatusCode.OK, "[{}]"));
        return (new HueApiClient("1.2.3.4", "key", handler), handler);
    }

    private static async Task WaitForRequests(StubHttpMessageHandler handler, int count)
    {
        for (int i = 0; i < 100 && handler.Requests.Count < count; i++)
            await Task.Delay(20);
    }

    [Fact]
    public void Light_ApplyState_MapsBridgeStateToUi()
    {
        var (client, _) = MakeClient();
        var dto = new HueLightDto
        {
            Name = "Right Lamp",
            Type = "Extended color light",
            ModelId = "LCT007",
            UniqueId = "u-1",
            State = new HueState { On = false, Brightness = 254, Reachable = true },
        };

        var vm = new LightViewModel("2", dto, client, _ => { }, _ => null);

        Assert.False(vm.On);
        Assert.True(vm.Reachable);
        Assert.True(vm.SupportsColor);
        Assert.InRange(vm.Brightness, 99, 100);
    }

    [Fact]
    public async Task Light_TogglingOn_SendsOnStateToBridge()
    {
        var (client, handler) = MakeClient();
        var dto = new HueLightDto
        {
            Name = "Left Lamp",
            Type = "Extended color light",
            State = new HueState { On = false, Brightness = 254, Reachable = true },
        };
        var vm = new LightViewModel("3", dto, client, _ => { }, _ => null);

        vm.On = true;
        await WaitForRequests(handler, 1);

        var req = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, req.Method);
        Assert.Equal("/api/key/lights/3/state", req.Path);
        Assert.Contains("\"on\":true", req.Body);
    }

    [Fact]
    public async Task Light_BrightnessChange_MapsToBridgeRange()
    {
        var (client, handler) = MakeClient();
        var dto = new HueLightDto
        {
            Name = "Vent 1",
            Type = "Extended color light",
            State = new HueState { On = true, Brightness = 254, Reachable = true },
        };
        var vm = new LightViewModel("11", dto, client, _ => { }, _ => null);

        vm.Brightness = 50;
        await WaitForRequests(handler, 1);

        var req = Assert.Single(handler.Requests);
        // 50% of 254 ≈ 127.
        Assert.Contains("\"bri\":127", req.Body);
    }

    [Fact]
    public async Task Light_SyncSetBrightness_UpdatesDisplayWithoutSending()
    {
        var (client, handler) = MakeClient();
        var dto = new HueLightDto
        {
            Name = "Kitchen lamp 1",
            Type = "Extended color light",
            State = new HueState { On = true, Brightness = 254, Reachable = true },
        };
        var vm = new LightViewModel("24", dto, client, _ => { }, _ => null);

        vm.SyncSetBrightness(40);
        await Task.Delay(150); // longer than the write debounce window

        Assert.InRange(vm.Brightness, 39, 41);
        Assert.Empty(handler.Requests); // room sync must not hit the bridge
    }

    [Fact]
    public void Group_ApplyState_MapsRoomState()
    {
        var (client, _) = MakeClient();
        var dto = new HueGroupDto
        {
            Name = "Living room",
            Type = "Room",
            Class = "Living room",
            Lights = new List<string> { "2", "3", "11" },
            State = new HueGroupState { AnyOn = true, AllOn = false },
            Action = new HueState { Brightness = 127 },
        };

        var vm = new GroupViewModel("85", dto, client, _ => { });

        Assert.True(vm.AnyOn);
        Assert.InRange(vm.Brightness, 49, 51);
        // Subtitle is localized ("{class} · {n} lights"); assert on the language-independent parts.
        Assert.Contains("Living room", vm.Subtitle);
        Assert.Contains("3", vm.Subtitle);
    }

    [Fact]
    public void Light_Unreachable_ReportsOfflineAndStatusText()
    {
        var (client, _) = MakeClient();
        var dto = new HueLightDto
        {
            Name = "Vent 1",
            Type = "Extended color light",
            ModelId = "LCT007",
            State = new HueState { On = true, Brightness = 162, Reachable = false },
        };

        var vm = new LightViewModel("6", dto, client, _ => { }, _ => null);

        Assert.False(vm.Reachable);
        Assert.True(vm.IsOffline);
        Assert.Equal(Loc.T("Offline_NoPower"), vm.StatusText);
    }

    [Fact]
    public void Light_ApplyState_RaisesIsOfflineWhenReachabilityChanges()
    {
        var (client, _) = MakeClient();
        var dto = new HueLightDto
        {
            Name = "Right",
            Type = "Extended color light",
            State = new HueState { On = true, Brightness = 254, Reachable = true },
        };
        var vm = new LightViewModel("10", dto, client, _ => { }, _ => null);

        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        vm.ApplyState(new HueLightDto
        {
            Name = "Right",
            Type = "Extended color light",
            State = new HueState { On = true, Brightness = 254, Reachable = false },
        });

        Assert.True(vm.IsOffline);
        Assert.Contains(nameof(LightViewModel.IsOffline), raised);
        Assert.Contains(nameof(LightViewModel.StatusText), raised);
    }

    [Fact]
    public void Group_UpdateOfflineCount_CountsOnlyOwnUnreachableLights()
    {
        var (client, _) = MakeClient();

        LightViewModel Light(string id, bool reachable) => new(
            id,
            new HueLightDto
            {
                Name = $"Light {id}",
                Type = "Extended color light",
                State = new HueState { On = true, Brightness = 254, Reachable = reachable },
            },
            client, _ => { }, _ => null);

        var allLights = new[]
        {
            Light("6", reachable: false),  // living room, offline
            Light("7", reachable: false),  // living room, offline
            Light("8", reachable: true),   // living room, online
            Light("24", reachable: false), // a different room, must be ignored
        };

        var dto = new HueGroupDto
        {
            Name = "Living room",
            Type = "Room",
            Class = "Living room",
            Lights = new List<string> { "6", "7", "8" },
            State = new HueGroupState { AnyOn = true, AllOn = true },
            Action = new HueState { Brightness = 162 },
        };
        var vm = new GroupViewModel("81", dto, client, _ => { });

        vm.UpdateOfflineCount(allLights);

        Assert.Equal(2, vm.OfflineCount);
        Assert.True(vm.HasOffline);
        Assert.Contains(Loc.T("NOffline_Fmt", 2), vm.Subtitle);
    }

    [Fact]
    public async Task Group_TurnOn_SendsActionToBridge()
    {
        var (client, handler) = MakeClient();
        var dto = new HueGroupDto { Name = "Kitchen", Type = "Room", Lights = new List<string> { "24" } };
        var vm = new GroupViewModel("84", dto, client, _ => { });

        vm.TurnOnCommand.Execute(null);
        await WaitForRequests(handler, 1);

        var req = Assert.Single(handler.Requests);
        Assert.Equal("/api/key/groups/84/action", req.Path);
        Assert.Contains("\"on\":true", req.Body);
    }
}
