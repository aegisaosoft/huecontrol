// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Security;
using System.Text.Json;
using HueControl.Models;

namespace HueControl.Services;

/// <summary>Raised when pairing requires the physical link button to be pressed first.</summary>
public sealed class LinkButtonNotPressedException : Exception
{
    public LinkButtonNotPressedException()
        : base("Press the link button on the bridge, then try again.") { }
}

/// <summary>The unauthenticated subset a bridge exposes at <c>/api/0/config</c>.</summary>
public sealed record BridgePublicConfig(string BridgeId, string Name, string ModelId);

/// <summary>A device the bridge discovered during a search scan.</summary>
public sealed record NewDevice(string Id, string Name);

/// <summary>Result of polling for newly discovered devices.</summary>
public sealed record NewDevicesScan(string LastScan, IReadOnlyList<NewDevice> Devices);

/// <summary>Thin wrapper over the Philips Hue local REST API (v1) for a single bridge.</summary>
public sealed class HueApiClient
{
    private const string AppName = "huecontrol#wpf";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly string _appKey;

    public string IpAddress { get; }

    public HueApiClient(string ipAddress, string appKey)
        : this(ipAddress, appKey, CreateHttpClient(ipAddress))
    {
    }

    // Test seam: allows a stub HttpMessageHandler to be injected.
    internal HueApiClient(string ipAddress, string appKey, HttpMessageHandler handler)
        : this(ipAddress, appKey, CreateHttpClient(ipAddress, handler))
    {
    }

    private HueApiClient(string ipAddress, string appKey, HttpClient http)
    {
        IpAddress = ipAddress;
        _appKey = appKey;
        _http = http;
    }

    private static HttpClient CreateHttpClient(string ipAddress, HttpMessageHandler? handler = null)
    {
        // Newer bridges (e.g. Bridge Pro) redirect to HTTPS and serve the local API
        // over TLS with a self-signed certificate (Hue Bridge Root CA). The bridge is
        // a LAN device the user explicitly targets by IP, so we:
        //   * talk HTTPS directly (avoids the HTTP->HTTPS redirect),
        //   * accept the bridge's self-signed certificate,
        //   * bypass any system/corporate proxy for LAN traffic.
        handler ??= new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            // The bridge is a low-powered device: keep a small warm connection pool
            // instead of opening a new (TLS-handshaked) connection per command.
            MaxConnectionsPerServer = 4,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            },
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri($"https://{ipAddress}/"),
            Timeout = TimeSpan.FromSeconds(10),
        };
        return client;
    }

    /// <summary>
    /// Requests a new application key from the bridge. The user must have pressed the
    /// link button within the last ~30 seconds, otherwise a
    /// <see cref="LinkButtonNotPressedException"/> is thrown.
    /// </summary>
    public static Task<string> PairAsync(string ipAddress, CancellationToken ct = default)
        => PairAsync(CreateHttpClient(ipAddress), ct);

    // Test seam for pairing.
    internal static Task<string> PairAsync(string ipAddress, HttpMessageHandler handler, CancellationToken ct = default)
        => PairAsync(CreateHttpClient(ipAddress, handler), ct);

    private static async Task<string> PairAsync(HttpClient http, CancellationToken ct)
    {
        using (http)
        {
            var payload = new { devicetype = AppName, generateclientkey = true };
            using var response = await http.PostAsJsonAsync("api", payload, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            JsonElement first = doc.RootElement[0];

            if (first.TryGetProperty("success", out JsonElement success))
                return success.GetProperty("username").GetString()!;

            if (first.TryGetProperty("error", out JsonElement error)
                && error.GetProperty("type").GetInt32() == 101)
            {
                throw new LinkButtonNotPressedException();
            }

            string message = first.TryGetProperty("error", out JsonElement e)
                ? e.GetProperty("description").GetString() ?? "Unknown error"
                : "Unexpected pairing response";
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Reads the public config (<c>/api/0/config</c>) without an app key. Useful to
    /// confirm reachability and identify a bridge before pairing.
    /// </summary>
    public static async Task<BridgePublicConfig> GetPublicConfigAsync(string ipAddress, CancellationToken ct = default)
    {
        using HttpClient http = CreateHttpClient(ipAddress);
        using var response = await http.GetAsync("api/0/config", ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        JsonElement r = doc.RootElement;

        string Read(string name) => r.TryGetProperty(name, out JsonElement e) ? e.GetString() ?? string.Empty : string.Empty;
        return new BridgePublicConfig(Read("bridgeid"), Read("name"), Read("modelid"));
    }

    public async Task<string> GetBridgeNameAsync(CancellationToken ct = default)
    {
        try
        {
            using var doc = await GetJsonAsync($"api/{_appKey}/config", ct);
            return doc.RootElement.TryGetProperty("name", out JsonElement name)
                ? name.GetString() ?? "Hue Bridge"
                : "Hue Bridge";
        }
        catch
        {
            return "Hue Bridge";
        }
    }

    public async Task<Dictionary<string, HueLightDto>> GetLightsAsync(CancellationToken ct = default)
        => await GetDictionaryAsync<HueLightDto>($"api/{_appKey}/lights", ct);

    public async Task<Dictionary<string, HueGroupDto>> GetGroupsAsync(CancellationToken ct = default)
        => await GetDictionaryAsync<HueGroupDto>($"api/{_appKey}/groups", ct);

    public async Task<Dictionary<string, HueSceneDto>> GetScenesAsync(CancellationToken ct = default)
        => await GetDictionaryAsync<HueSceneDto>($"api/{_appKey}/scenes", ct);

    public async Task<Dictionary<string, HueSensorDto>> GetSensorsAsync(CancellationToken ct = default)
        => await GetDictionaryAsync<HueSensorDto>($"api/{_appKey}/sensors", ct);

    // ---- Device discovery / management ----

    /// <summary>Starts a scan for new lights and plugs. Optionally targets specific serials.</summary>
    public Task StartLightSearchAsync(IEnumerable<string>? serials = null, CancellationToken ct = default)
    {
        object? body = serials is null ? null : new { deviceid = serials.ToArray() };
        return PostAsync($"api/{_appKey}/lights", body, ct);
    }

    /// <summary>Starts a scan for new accessories (sensors, switches, buttons).</summary>
    public Task StartSensorSearchAsync(CancellationToken ct = default)
        => PostAsync($"api/{_appKey}/sensors", null, ct);

    public Task<NewDevicesScan> GetNewLightsAsync(CancellationToken ct = default)
        => GetNewScanAsync($"api/{_appKey}/lights/new", ct);

    public Task<NewDevicesScan> GetNewSensorsAsync(CancellationToken ct = default)
        => GetNewScanAsync($"api/{_appKey}/sensors/new", ct);

    public Task DeleteLightAsync(string id, CancellationToken ct = default)
        => DeleteAsync($"api/{_appKey}/lights/{id}", ct);

    public Task DeleteSensorAsync(string id, CancellationToken ct = default)
        => DeleteAsync($"api/{_appKey}/sensors/{id}", ct);

    public Task RenameLightAsync(string id, string name, CancellationToken ct = default)
        => PutAsync($"api/{_appKey}/lights/{id}", new { name }, ct);

    public Task RenameSensorAsync(string id, string name, CancellationToken ct = default)
        => PutAsync($"api/{_appKey}/sensors/{id}", new { name }, ct);

    // ---- Scene management ----

    /// <summary>Creates a scene that snapshots the current state of a room's lights.</summary>
    public Task CreateGroupSceneAsync(string name, string groupId, CancellationToken ct = default)
        => PostAsync($"api/{_appKey}/scenes", new { name, group = groupId, type = "GroupScene", recycle = false }, ct);

    public Task RenameSceneAsync(string id, string name, CancellationToken ct = default)
        => PutAsync($"api/{_appKey}/scenes/{id}", new { name }, ct);

    public Task DeleteSceneAsync(string id, CancellationToken ct = default)
        => DeleteAsync($"api/{_appKey}/scenes/{id}", ct);

    /// <summary>Creates a scene and returns its new id (from the success response).</summary>
    public async Task<string> CreateSceneAsync(object body, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/{_appKey}/scenes", body, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement[0].GetProperty("success").GetProperty("id").GetString()!;
    }

    /// <summary>Sets the stored state of one light inside a scene (does not change the live light).</summary>
    public Task SetSceneLightStateAsync(string sceneId, string lightId, object body, CancellationToken ct = default)
        => PutAsync($"api/{_appKey}/scenes/{sceneId}/lightstates/{lightId}", body, ct);

    // ---- Room / zone management ----

    /// <summary>Creates a room or zone. Rooms carry a class; zones do not.</summary>
    public Task CreateGroupAsync(string name, string type, IEnumerable<string> lights, CancellationToken ct = default)
    {
        string[] lightIds = lights.ToArray();
        object body = string.Equals(type, "Zone", StringComparison.OrdinalIgnoreCase)
            ? new { name, type = "Zone", lights = lightIds }
            : new { name, type = "Room", @class = "Other", lights = lightIds };
        return PostAsync($"api/{_appKey}/groups", body, ct);
    }

    public Task RenameGroupAsync(string id, string name, CancellationToken ct = default)
        => PutAsync($"api/{_appKey}/groups/{id}", new { name }, ct);

    public Task SetGroupLightsAsync(string id, IEnumerable<string> lights, CancellationToken ct = default)
        => PutAsync($"api/{_appKey}/groups/{id}", new { lights = lights.ToArray() }, ct);

    public Task DeleteGroupAsync(string id, CancellationToken ct = default)
        => DeleteAsync($"api/{_appKey}/groups/{id}", ct);

    // ---- Rules (used to program switch buttons) ----

    public async Task<Dictionary<string, HueRuleDto>> GetRulesAsync(CancellationToken ct = default)
        => await GetDictionaryAsync<HueRuleDto>($"api/{_appKey}/rules", ct);

    public Task CreateRuleAsync(object body, CancellationToken ct = default)
        => PostAsync($"api/{_appKey}/rules", body, ct);

    public Task DeleteRuleAsync(string id, CancellationToken ct = default)
        => DeleteAsync($"api/{_appKey}/rules/{id}", ct);

    private async Task<NewDevicesScan> GetNewScanAsync(string path, CancellationToken ct)
    {
        using var doc = await GetJsonAsync(path, ct);
        string lastScan = "none";
        var devices = new List<NewDevice>();
        foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
        {
            if (prop.NameEquals("lastscan"))
            {
                lastScan = prop.Value.GetString() ?? "none";
                continue;
            }
            string name = prop.Value.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? prop.Name : prop.Name;
            devices.Add(new NewDevice(prop.Name, name));
        }
        return new NewDevicesScan(lastScan, devices);
    }

    public Task SetLightStateAsync(string lightId, object body, CancellationToken ct = default)
        => PutAsync($"api/{_appKey}/lights/{lightId}/state", body, ct);

    public Task SetGroupActionAsync(string groupId, object body, CancellationToken ct = default)
        => PutAsync($"api/{_appKey}/groups/{groupId}/action", body, ct);

    /// <summary>Recalls a scene. Light scenes with no group are applied to group 0 (all lights).</summary>
    public Task ActivateSceneAsync(string sceneId, string? groupId, CancellationToken ct = default)
        => SetGroupActionAsync(string.IsNullOrEmpty(groupId) ? "0" : groupId, new { scene = sceneId }, ct);

    private async Task<Dictionary<string, T>> GetDictionaryAsync<T>(string path, CancellationToken ct)
    {
        using var doc = await GetJsonAsync(path, ct);

        // An expired/invalid app key returns a JSON array with an error element.
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            throw new InvalidOperationException("Bridge rejected the app key. Re-pair this bridge.");

        return JsonSerializer.Deserialize<Dictionary<string, T>>(doc.RootElement.GetRawText(), JsonOptions)
               ?? new Dictionary<string, T>();
    }

    private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken ct)
    {
        using var response = await _http.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }

    private async Task PutAsync(string path, object body, CancellationToken ct)
    {
        using var response = await _http.PutAsJsonAsync(path, body, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task PostAsync(string path, object? body, CancellationToken ct)
    {
        using HttpResponseMessage response = body is null
            ? await _http.PostAsync(path, content: null, ct)
            : await _http.PostAsJsonAsync(path, body, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteAsync(string path, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(path, ct);
        response.EnsureSuccessStatusCode();
    }
}
