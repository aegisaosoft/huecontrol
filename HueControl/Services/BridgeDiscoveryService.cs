// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Net.Http;
using System.Net.Http.Json;
using HueControl.Models;

namespace HueControl.Services;

/// <summary>Discovers Hue bridges on the local network via the Philips N-UPnP endpoint.</summary>
public sealed class BridgeDiscoveryService
{
    private const string DiscoveryUrl = "https://discovery.meethue.com/";

    private sealed class DiscoveryEntry
    {
        public string id { get; set; } = string.Empty;
        public string internalipaddress { get; set; } = string.Empty;
        public int port { get; set; } = 443;
    }

    public async Task<IReadOnlyList<DiscoveredBridge>> DiscoverAsync(CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            var entries = await http.GetFromJsonAsync<List<DiscoveryEntry>>(DiscoveryUrl, ct)
                          ?? new List<DiscoveryEntry>();

            return entries
                .Where(e => !string.IsNullOrWhiteSpace(e.internalipaddress))
                .Select(e => new DiscoveredBridge
                {
                    Id = e.id,
                    IpAddress = e.internalipaddress,
                    Port = e.port == 0 ? 443 : e.port,
                })
                .ToList();
        }
        catch
        {
            // Cloud discovery can be blocked or offline; the user can still add a bridge by IP.
            return Array.Empty<DiscoveredBridge>();
        }
    }
}
