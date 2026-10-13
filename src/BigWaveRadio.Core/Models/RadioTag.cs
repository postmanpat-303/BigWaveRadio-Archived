using System.Text.Json.Serialization;

namespace BigWaveRadio.Core.Models;

public class RadioTag
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("stationcount")] public int StationCount { get; set; }
}
