using CommunityToolkit.Mvvm.ComponentModel;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.App.ViewModels;

/// <summary>
/// UI wrapper around a <see cref="Station"/> that tracks its
/// saved-to-library state and keeps it in sync with the library service.
/// </summary>
public partial class StationItem : ObservableObject
{
    public Station Station { get; }

    [ObservableProperty]
    private bool _isInLibrary;

    /// <summary>True while this station is the one currently playing.</summary>
    public bool IsPlaying =>
        PlayerViewModel.Instance.CurrentStation?.StationUuid == Station.StationUuid
        && PlayerViewModel.Instance.IsPlaying;

    public StationItem(Station station)
    {
        Station = station;
        IsInLibrary = App.Library.Contains(station.StationUuid);
        App.Library.Changed += OnLibraryChanged;
        PlayerViewModel.PlaybackChanged += OnPlaybackChanged;
    }

    public void ToggleLibrary() => App.Library.Toggle(Station);

    private void OnPlaybackChanged(object? sender, EventArgs e)
        => OnPropertyChanged(nameof(IsPlaying));

    private void OnLibraryChanged(object? sender, EventArgs e)
    {
        var value = App.Library.Contains(Station.StationUuid);
        if (value != IsInLibrary)
            IsInLibrary = value;
    }
}
