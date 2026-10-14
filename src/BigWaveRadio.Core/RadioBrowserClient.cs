using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.Core;

/// <summary>
/// Thin async client for the community-run radio-browser.info API.
/// https://www.radio-browser.info/
///
/// The API is served by a pool of volunteer mirrors; clients are asked to
/// distribute load, so we fetch the server list once and rotate through it.
/// </summary>
public class RadioBrowserClient
{
    private const string AppName = "BigWaveRadio";
    private const string AppVersion = "1.0.0";
    private static readonly Uri ServerListUri = new("https://all.api.radio-browser.info/json/servers");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly List<string> _mirrors = new()
    {
        "de1.api.radio-browser.info",
        "nl1.api.radio-browser.info",
        "fr1.api.radio-browser.info",
        "at1.api.radio-browser.info",
        "us1.api.radio-browser.info",
    };
    private int _mirrorIndex;
    private DateTime _mirrorListFetchedAt = DateTime.MinValue;

    public RadioBrowserClient(HttpClient? http = null)
    {
        // Explicitly use the managed socket handler: WinUI projects default
        // HttpClient to the native WinHTTP handler, which resolves proxies
        // and name lookups differently and can fail where sockets succeed.
        _http = http ?? new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        });
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppName}/{AppVersion}");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    private string CurrentMirror => $"https://{_mirrors[_mirrorIndex % _mirrors.Count]}";

    private void RotateMirror() => _mirrorIndex = (_mirrorIndex + 1) % _mirrors.Count;

    /// <summary>
    /// Refresh the mirror list from the API (cached for one hour).
    /// Falls back to the built-in list if unreachable.
    /// </summary>
    public async Task RefreshMirrorsAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _mirrorListFetchedAt < TimeSpan.FromHours(1)) return;
        try
        {
            var servers = await _http.GetFromJsonAsync<List<ServerEntry>>(ServerListUri, ct);
            if (servers is { Count: > 0 })
            {
                // The API returns full hostnames ("de1.api.radio-browser.info.");
                // normalise them (and our short fallback names) to bare hosts.
                var hosts = servers
                    .Select(s => s.Name.TrimEnd('.'))
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.EndsWith(".api.radio-browser.info", StringComparison.OrdinalIgnoreCase)
                        ? n
                        : $"{n}.api.radio-browser.info")
                    .ToList();
                if (hosts.Count > 0)
                {
                    _mirrors.Clear();
                    _mirrors.AddRange(hosts);
                    _mirrorIndex = Random.Shared.Next(_mirrors.Count);
                }
            }
        }
        catch
        {
            // Keep the built-in fallback list.
        }
        _mirrorListFetchedAt = DateTime.UtcNow;
    }

    /// <summary>Most-voted stations ("Trending" in-app).</summary>
    public Task<IReadOnlyList<Station>> GetTopVotedAsync(int count = 60, CancellationToken ct = default)
        => GetListAsync($"/json/stations/topvote/{count}", ct);

    /// <summary>Most-clicked stations ("Popular" in-app).</summary>
    public Task<IReadOnlyList<Station>> GetTopClickedAsync(int count = 60, CancellationToken ct = default)
        => GetListAsync($"/json/stations/topclick/{count}", ct);

    /// <summary>Recently added or changed stations.</summary>
    public Task<IReadOnlyList<Station>> GetRecentlyChangedAsync(int count = 60, CancellationToken ct = default)
        => GetListAsync($"/json/stations/lastchange/{count}", ct);

    public Task<IReadOnlyList<Station>> SearchStationsAsync(string query, int limit = 100, int minBitrate = 0, CancellationToken ct = default)
        => GetListAsync($"/json/stations/search?name={Uri.EscapeDataString(query)}&limit={limit}&hidebroken=true{BitrateClause(minBitrate)}", ct);

    public Task<IReadOnlyList<Station>> GetStationsByTagAsync(string tag, int limit = 100, int minBitrate = 0, CancellationToken ct = default)
        => GetListAsync($"/json/stations/bytag/{Uri.EscapeDataString(tag)}?limit={limit}&hidebroken=true&order=votes&reverse=true{BitrateClause(minBitrate)}", ct);

    public Task<IReadOnlyList<Station>> GetStationsByCountryAsync(string country, int limit = 100, int minBitrate = 0, CancellationToken ct = default)
        => GetListAsync($"/json/stations/bycountryexact/{Uri.EscapeDataString(country)}?limit={limit}&hidebroken=true&order=votes&reverse=true{BitrateClause(minBitrate)}", ct);

    /// <summary>radio-browser supports server-side minimum-bitrate filtering on search endpoints.</summary>
    private static string BitrateClause(int minBitrate) => minBitrate > 0 ? $"&bitrateMin={minBitrate}" : string.Empty;

    public Task<IReadOnlyList<RadioTag>> GetTagsAsync(int limit = 200, CancellationToken ct = default)
        => GetListAsync<RadioTag>($"/json/tags?order=stationcount&reverse=true&hidebroken=true&limit={limit}", ct);

    public Task<IReadOnlyList<RadioCountry>> GetCountriesAsync(CancellationToken ct = default)
        => GetListAsync<RadioCountry>("/json/countries?order=stationcount&reverse=true&hidebroken=true", ct);

    /// <summary>
    /// Register a click with the community database (fire-and-forget).
    /// This is how listener counts are contributed back — good etiquette.
    /// </summary>
    public async void RegisterClickAsync(string stationUuid)
    {
        if (string.IsNullOrWhiteSpace(stationUuid)) return;
        try
        {
            await _http.PostAsync($"{CurrentMirror}/json/url/{stationUuid}", null);
        }
        catch
        {
            // Telemetry only — never let it break playback.
        }
    }

    private async Task<IReadOnlyList<Station>> GetListAsync(string path, CancellationToken ct)
        => await GetListAsync<Station>(path, ct);

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string path, CancellationToken ct)
    {
        await RefreshMirrorsAsync(ct);
        Exception? lastError = null;
        for (var attempt = 0; attempt < _mirrors.Count; attempt++)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<T>>($"{CurrentMirror}{path}", JsonOptions, ct);
                return result ?? [];
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                lastError = ex;
                RotateMirror();
            }
        }
        throw new InvalidOperationException("All radio-browser mirrors failed. Check your internet connection.", lastError);
    }

    private sealed class ServerEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    }
}
