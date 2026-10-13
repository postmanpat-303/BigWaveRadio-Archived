using System.Text.Json;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.Core;

/// <summary>
/// "My Library": quick-reference list of stations the user pinned.
/// Persisted as a small JSON file in %LocalAppData%/BigWaveRadio.
/// Stations are only referenced/cached locally — streams always come
/// live from the station's own server.
/// </summary>
public class LibraryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly Dictionary<string, Station> _stations = new();

    public event EventHandler? Changed;

    public string StoragePath => _filePath;

    public LibraryService(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BigWaveRadio", "library.json");
    }

    public IReadOnlyCollection<Station> Stations => _stations.Values.ToArray();

    public bool Contains(string stationUuid) => _stations.ContainsKey(stationUuid);

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = await File.ReadAllTextAsync(_filePath);
            var list = JsonSerializer.Deserialize<List<Station>>(json, JsonOptions) ?? [];
            _stations.Clear();
            foreach (var s in list)
                if (!string.IsNullOrWhiteSpace(s.StationUuid))
                    _stations[s.StationUuid] = s;
        }
        catch
        {
            // Corrupt file: start with an empty library rather than crash.
            _stations.Clear();
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Add(Station station)
    {
        if (string.IsNullOrWhiteSpace(station.StationUuid) || _stations.ContainsKey(station.StationUuid))
            return false;
        _stations[station.StationUuid] = station;
        _ = SaveAsync();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Remove(string stationUuid)
    {
        if (!_stations.Remove(stationUuid)) return false;
        _ = SaveAsync();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Toggle(Station station) =>
        Contains(station.StationUuid) ? Remove(station.StationUuid) : Add(station);

    private async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var json = JsonSerializer.Serialize(_stations.Values.ToList(), JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
            // Best-effort persistence.
        }
    }
}
