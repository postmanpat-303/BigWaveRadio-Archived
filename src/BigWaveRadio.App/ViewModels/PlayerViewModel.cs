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

    [ObservableProperty]
    private double _volume = 0.8;

    [ObservableProperty]
    private bool _sleepTimerActive;

    [ObservableProperty]
    private string _sleepRemainingText = string.Empty;

    public bool HasStation => CurrentStation is not null;

    private readonly DispatcherQueueTimer? _sleepTimer;
    private TimeSpan _sleepRemaining;

    private PlayerViewModel()
    {
        Player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
        Player.MediaFailed += OnMediaFailed;
        Player.Volume = Volume;
        _sleepTimer = DispatcherQueue.GetForCurrentThread()?.CreateTimer();
        if (_sleepTimer is not null)
        {
            _sleepTimer.Interval = TimeSpan.FromSeconds(1);
            _sleepTimer.IsRepeating = true;
            _sleepTimer.Tick += OnSleepTick;
        }
    }

    public void Play(Station station)
    {
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

    private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        IsPlaying = sender.PlaybackState is MediaPlaybackState.Playing
            or MediaPlaybackState.Buffering
            or MediaPlaybackState.Opening;
        Status = sender.PlaybackState switch
        {
            MediaPlaybackState.Playing => "Playing",
            MediaPlaybackState.Buffering => "Buffering…",
            MediaPlaybackState.Opening => "Connecting…",
            MediaPlaybackState.Paused => "Paused",
            _ => HasStation ? "Stopped" : "Ready",
        };
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        IsPlaying = false;
        var detail = args.ExtendedErrorCode is { } ex ? $" ({ex.HResult:X8}: {ex.Message})" : "";
        Status = $"Playback error: {args.Error} {args.ErrorMessage}{detail}".Trim();
    }
}
