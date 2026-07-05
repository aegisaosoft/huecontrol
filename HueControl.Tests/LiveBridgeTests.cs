// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using HueControl.Services;
using Xunit.Abstractions;

namespace HueControl.Tests;

/// <summary>
/// Opt-in tests that talk to a real Hue bridge on the LAN. Enable with
/// <c>HUE_LIVE=1</c>; override the target with <c>HUE_BRIDGE_IP</c>
/// (defaults to the Bridge Pro at 192.168.1.121). These do not require the
/// link button and never modify bridge state.
/// </summary>
public class LiveBridgeTests
{
    private readonly ITestOutputHelper _output;

    public LiveBridgeTests(ITestOutputHelper output) => _output = output;

    private static string BridgeIp =>
        Environment.GetEnvironmentVariable("HUE_BRIDGE_IP") ?? "192.168.1.121";

    [LiveFact]
    public async Task PublicConfig_IsReachable_AndIdentifiesBridge()
    {
        BridgePublicConfig config = await HueApiClient.GetPublicConfigAsync(BridgeIp);

        _output.WriteLine($"Bridge at {BridgeIp}: name='{config.Name}', id='{config.BridgeId}', model='{config.ModelId}'");

        Assert.False(string.IsNullOrWhiteSpace(config.BridgeId));
        Assert.False(string.IsNullOrWhiteSpace(config.ModelId));
    }

    [LiveFact]
    public async Task Pair_WithoutLinkButton_ReportsLinkButtonNotPressed()
    {
        // Without a fresh link-button press the bridge must reject pairing with
        // error type 101, which the client surfaces as this exception.
        await Assert.ThrowsAsync<LinkButtonNotPressedException>(
            () => HueApiClient.PairAsync(BridgeIp));
    }
}
