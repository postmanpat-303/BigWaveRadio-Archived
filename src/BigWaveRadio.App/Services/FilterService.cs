using System.Text.Json;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.App.Services;

/// <summary>
/// Implemented by pages whose station list honours the bitrate filter, so the
/// main window can ask the visible page to reload when the filter changes.
/// </summary>
public interface IBitrateFilterable
{
    void ReloadForBitrateFilter();
}

/// <summary>
/// App-wide "minimum bitrate" filter for station lists, shown in the nav pane.
/// Persisted next to the appearance settings; changing it raises
/// <see cref="Changed"/> and the visible page reloads its list.
/// </summary>
public static class FilterService
{
    /// <summary>Selectable thresholds; 0 means "any bitrate".</summary>
    public static IReadOnlyList<int> Options { get; } = new[] { 0, 64, 128, 192, 256, 320 };

    public static int MinBitrate { get; private set; }

    /// <summary>Raised (on the UI thread) whenever <see cref="MinBitrate"/> changes.</summary>
    public static event EventHandler? Changed;

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BigWaveRadio");
    private static readonly string FilePath = Path.Combine(Dir, "filters.json");

    public static async Task LoadAsync()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = await File.ReadAllTextAsync(FilePath);
                var loaded = JsonSerializer.Deserialize<FilterSettings>(json);
                if (loaded is not null && Options.Contains(loaded.MinBitrate))
                    MinBitrate = loaded.MinBitrate;
            }
        }
        catch
        {
            // Corrupt or locked file — keep the default "any bitrate".
        }
    }

    public static void SetMinBitrate(int value)
    {
        if (!Options.Contains(value)) value = 0;
        if (MinBitrate == value) return;
        MinBitrate = value;
        Save();
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>True when the station passes the current filter (unknown bitrates fail a set filter).</summary>
    public static bool Matches(Station station) =>
        MinBitrate == 0 || station.Bitrate >= MinBitrate;

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(new FilterSettings { MinBitrate = MinBitrate }));
        }
        catch
        {
            // Persistence is best-effort; the live session always reflects the choice.
        }
    }

    private sealed class FilterSettings
    {
        public int MinBitrate { get; set; }
    }
}
