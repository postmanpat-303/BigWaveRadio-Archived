using System.Text.Json.Serialization;

namespace BigWaveRadio.Core.Models;

public class RadioCountry
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("iso_3166_1")] public string IsoCode { get; set; } = string.Empty;
    [JsonPropertyName("stationcount")] public int StationCount { get; set; }
}
