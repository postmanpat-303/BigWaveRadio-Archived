using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BigWaveRadio.Core.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace BigWaveRadio.App.ViewModels;

/// <summary>
/// Wraps the shared <see cref="MediaPlayer"/> and exposes playback state to
/// the UI (now-playing bar). Single instance, created on the UI thread.
/// </summary>
public partial class PlayerViewModel : ObservableObject
{
    public static PlayerViewModel Instance { get; } = new();

    /// <summary>Raised when the current station or the playing state changes.</summary>
    public static event EventHandler? PlaybackChanged;

    public MediaPlayer Player { get; } = new() { AutoPlay = false };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStation))]
    private Station? _currentStation;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _currentStationInLibrary;

    /// <summary>True when there is a station before the current one in the queue.</summary>
    [ObservableProperty]
    private bool _canGoPrevious;

    /// <summary>True when there is a station after the current one in the queue.</summary>
    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private double _volume = 0.8;

    [ObservableProperty]
    private bool _sleepTimerActive;

    [ObservableProperty]
    private string _sleepRemainingText = string.Empty;

    public bool HasStation => CurrentStation is not null;

    /// <summary>
    /// The station list the current pick came from (Discover grid, search results…).
    /// Previous/Next walk this list. Reset to just the current station when a
    /// pick is not part of the list.
    /// </summary>
    private List<Station> _queue = new();
    private int _queueIndex = -1;

    private readonly DispatcherQueueTimer? _sleepTimer;
    private readonly DispatcherQueue? _dispatcher;
    private TimeSpan _sleepRemaining;

    private PlayerViewModel()
    {
        Player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
        Player.MediaFailed += OnMediaFailed;
        Player.Volume = Volume;
        // MediaPlayer events arrive on a worker thread; state must be applied
        // on the UI thread or the now-playing bar never updates.
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _sleepTimer = _dispatcher?.CreateTimer();
        if (_sleepTimer is not null)
        {
            _sleepTimer.Interval = TimeSpan.FromSeconds(1);
            _sleepTimer.IsRepeating = true;
            _sleepTimer.Tick += OnSleepTick;
        }
    }

    /// <summary>
    /// Registers the list a play request came from, so Previous/Next can walk it.
    /// Called by the station grid before it raises <see cref="PlayRequested"/>.
    /// </summary>
    public void SetQueue(System.Collections.IList items)
    {
        _queue = items.OfType<StationItem>().Select(i => i.Station).ToList();
        SyncQueueIndex();
    }

    private void SyncQueueIndex()
    {
        if (CurrentStation is null)
        {
            _queueIndex = -1;
        }
        else
        {
            _queueIndex = _queue.FindIndex(s => s.StationUuid == CurrentStation.StationUuid);
            if (_queueIndex < 0)
            {
                // Current station is not from this list — treat it as a one-off.
                _queue = new List<Station> { CurrentStation };
                _queueIndex = 0;
            }
        }
        CanGoPrevious = _queueIndex > 0;
        CanGoNext = _queueIndex >= 0 && _queueIndex < _queue.Count - 1;
    }

    [RelayCommand]
    private void PreviousStation()
    {
        if (!CanGoPrevious) return;
        Play(_queue[_queueIndex - 1], fromQueue: true);
    }

    [RelayCommand]
    private void NextStation()
    {
        if (!CanGoNext) return;
        Play(_queue[_queueIndex + 1], fromQueue: true);
    }

    public void Play(Station station)
    {
        Play(station, fromQueue: false);
    }

    private void Play(Station station, bool fromQueue)
    {
        if (!fromQueue || _queueIndex < 0 || _queueIndex >= _queue.Count ||
            _queue[_queueIndex].StationUuid != station.StationUuid)
        {
            var idx = _queue.FindIndex(s => s.StationUuid == station.StationUuid);
            if (idx >= 0)
            {
                _queueIndex = idx;
            }
            else
            {
                _queue = new List<Station> { station };
                _queueIndex = 0;
            }
        }
        CanGoPrevious = _queueIndex > 0;
        CanGoNext = _queueIndex < _queue.Count - 1;

        CurrentStation = station;
        RefreshLibraryState();
        try
        {
            Status = "Connecting…";
            Player.Source = MediaSource.CreateFromUri(new Uri(station.StreamUrl));
            Player.Play();

            // Community etiquette: report the click back to radio-browser.info.
            App.Client.RegisterClickAsync(station.StationUuid);
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            Status = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (CurrentStation is null) return;
        if (IsPlaying)
        {
            Player.Pause();
        }
        else
        {
            if (Player.Source is null)
                Player.Source = MediaSource.CreateFromUri(new Uri(CurrentStation.StreamUrl));
            Player.Play();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        Player.Pause();
        Player.Source = null;
        IsPlaying = false;
        Status = "Stopped";
    }

    [RelayCommand]
    private void ToggleCurrentLibrary()
    {
        if (CurrentStation is null) return;
        App.Library.Toggle(CurrentStation);
        RefreshLibraryState();
    }

    public void RefreshLibraryState()
    {
        CurrentStationInLibrary = CurrentStation is not null && App.Library.Contains(CurrentStation.StationUuid);
    }

    [RelayCommand]
    private void SetSleepTimer(string? minutes)
    {
        if (!int.TryParse(minutes, out var m) || m <= 0 || _sleepTimer is null) return;
        _sleepRemaining = TimeSpan.FromMinutes(m);
        SleepTimerActive = true;
        _sleepTimer.Start();
        UpdateSleepText();
    }

    [RelayCommand]
    private void CancelSleepTimer()
    {
        _sleepTimer?.Stop();
        SleepTimerActive = false;
        SleepRemainingText = string.Empty;
    }

    private void OnSleepTick(DispatcherQueueTimer sender, object args)
    {
        _sleepRemaining = _sleepRemaining.Subtract(TimeSpan.FromSeconds(1));
        if (_sleepRemaining <= TimeSpan.Zero)
        {
            CancelSleepTimer();
            Stop();
            return;
        }
        UpdateSleepText();
    }

    private void UpdateSleepText()
    {
        SleepRemainingText = $"Sleep {_sleepRemaining:mm\\:ss}";
    }

    partial void OnVolumeChanged(double value)
    {
        Player.Volume = Math.Clamp(value, 0.0, 1.0);
    }

    partial void OnIsPlayingChanged(bool value)
    {
        // Drive the realtime spectrum analyser from actual playback state.
        if (value) Services.SpectrumService.Instance.Start();
        else Services.SpectrumService.Instance.Stop();
        PlaybackChanged?.Invoke(null, EventArgs.Empty);
    }

    partial void OnCurrentStationChanged(Station? value)
    {
        PlaybackChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>MediaPlayer callbacks arrive off-UI; re-raise the state there so bindings refresh.</summary>
    private void OnUiThread(Action action)
    {
        if (_dispatcher is null || _dispatcher.HasThreadAccess) action();
        else _dispatcher.TryEnqueue(() => action());
    }

    private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        OnUiThread(() =>
        {
            IsPlaying = sender.PlaybackState is MediaPlaybackState.Playing
                or MediaPlaybackState.Buffering
                or MediaPlaybackState.Opening;
            Status = sender.PlaybackState switch
            {
                MediaPlaybackState.Playing => "Connected",
                MediaPlaybackState.Buffering => "Buffering…",
                MediaPlaybackState.Opening => "Connecting…",
                MediaPlaybackState.Paused => "Paused",
                _ => HasStation ? "Stopped" : "Ready",
            };
        });
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        OnUiThread(() =>
        {
            IsPlaying = false;
            var detail = args.ExtendedErrorCode is { } ex ? $" ({ex.HResult:X8}: {ex.Message})" : "";
            Status = $"Playback error: {args.Error} {args.ErrorMessage}{detail}".Trim();
        });
    }
}
