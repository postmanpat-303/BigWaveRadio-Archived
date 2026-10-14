using BigWaveRadio.App.Services;
using BigWaveRadio.App.ViewModels;
using BigWaveRadio.Core;
using Microsoft.UI.Xaml;

namespace BigWaveRadio.App;

/// <summary>
/// Application entry point. Holds the shared services used across the app:
/// the radio-browser.info client, the local library store and the player.
/// </summary>
public partial class App : Application
{
    /// <summary>Shared radio-browser.info API client.</summary>
    public static RadioBrowserClient Client { get; } = new();

    /// <summary>The user's "My Library" station collection (local JSON).</summary>
    public static LibraryService Library { get; } = new();

    private Window? _window;

    public App()
    {
        InitializeComponent();

        // Log any unhandled XAML/managed exception before the process dies,
        // so a crash report can be diagnosed from %TEMP%\bwr-error.log.
        UnhandledException += (_, e) =>
        {
            try
            {
                e.Handled = true;
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "bwr-error.log"),
                    $"{DateTime.Now:u}  Unhandled exception:{Environment.NewLine}{e.Exception}{Environment.NewLine}");
            }
            catch { /* logging must never make things worse */ }
        };

        // Keep the now-playing "save to library" star in sync.
        Library.Changed += (_, _) => PlayerViewModel.Instance.RefreshLibraryState();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await ThemeService.LoadAsync();
        _window = new MainWindow();
        _window.Activate();
        await Library.LoadAsync();
    }
}
