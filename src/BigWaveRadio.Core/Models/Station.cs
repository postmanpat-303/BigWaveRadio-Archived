using System.Text.Json.Serialization;

namespace BigWaveRadio.Core.Models;

/// <summary>
/// A radio station as returned by the radio-browser.info API.
/// Property names map 1:1 to the JSON payload.
/// </summary>
public class Station
{
    [JsonPropertyName("changeuuid")] public string ChangeUuid { get; set; } = string.Empty;
    [JsonPropertyName("stationuuid")] public string StationUuid { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    [JsonPropertyName("url_resolved")] public string UrlResolved { get; set; } = string.Empty;
    [JsonPropertyName("homepage")] public string Homepage { get; set; } = string.Empty;
    [JsonPropertyName("favicon")] public string Favicon { get; set; } = string.Empty;
    [JsonPropertyName("tags")] public string Tags { get; set; } = string.Empty;
    [JsonPropertyName("country")] public string Country { get; set; } = string.Empty;
    [JsonPropertyName("countrycode")] public string CountryCode { get; set; } = string.Empty;
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    [JsonPropertyName("language")] public string Language { get; set; } = string.Empty;
    [JsonPropertyName("votes")] public int Votes { get; set; }
    [JsonPropertyName("lastchangetime")] public string LastChangeTime { get; set; } = string.Empty;
    [JsonPropertyName("codec")] public string Codec { get; set; } = string.Empty;
    [JsonPropertyName("bitrate")] public int Bitrate { get; set; }
    [JsonPropertyName("hls")] public int Hls { get; set; }
    [JsonPropertyName("lastcheckok")] public int LastCheckOk { get; set; }
    [JsonPropertyName("clickcount")] public int ClickCount { get; set; }
    [JsonPropertyName("clicktrend")] public int ClickTrend { get; set; }
    [JsonPropertyName("ssl_error")] public int SslError { get; set; }

    /// <summary>The best URL to actually stream from (resolved redirects).</summary>
    [JsonIgnore]
    public string StreamUrl => !string.IsNullOrWhiteSpace(UrlResolved) ? UrlResolved : Url;

    [JsonIgnore]
    public bool IsHealthy => LastCheckOk == 1 && SslError == 0;

    /// <summary>First few tags, split for display.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> TagList =>
        Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(3)
            .ToArray();

    /// <summary>"Country · Language" subtitle line for cards.</summary>
    [JsonIgnore]
    public string MetaLine =>
        string.Join(" · ", new[] { Country, Language }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>"MP3 · 128 kbps · 42 votes" quality line for cards.</summary>
    [JsonIgnore]
    public string QualityLine =>
        string.Join(" · ", new[]
        {
            string.IsNullOrWhiteSpace(Codec) ? "" : Codec.ToUpperInvariant(),
            Bitrate > 0 ? $"{Bitrate} kbps" : "",
            Votes > 0 ? $"{Votes} votes" : "",
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Meta and quality lines merged into one compact card subtitle.</summary>
    [JsonIgnore]
    public string SummaryLine =>
        string.Join(" · ", new[] { MetaLine, QualityLine }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>"12,345 votes · 67,890 clicks" popularity line for the info panel.</summary>
    [JsonIgnore]
    public string PopularityLine =>
        string.Join(" · ", new[]
        {
            Votes > 0 ? $"{Votes:N0} votes" : "",
            ClickCount > 0 ? $"{ClickCount:N0} clicks" : "",
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
