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
        Assert.Contains("3 lights", vm.Subtitle);
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
