using BigWaveRadio.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;
using System.IO;
using System.Net.Http;

namespace BigWaveRadio.App.Controls;

/// <summary>
/// Right-side panel showing the playing station's details: logo, name,
/// country/language, codec/bitrate, tags, popularity and homepage link.
/// </summary>
public sealed partial class NowPlayingPanel : UserControl
{
    private static readonly HttpClient FaviconHttp = new(new SocketsHttpHandler())
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    private int _faviconRequest;

    public NowPlayingPanel()
    {
        InitializeComponent();
        DataContext = PlayerViewModel.Instance;
        PlayerViewModel.Instance.PropertyChanged += OnPlayerPropertyChanged;
        UpdateStationDetails();
    }

    /// <summary>Raised when the user closes the panel.</summary>
    public event RoutedEventHandler? CloseRequested;

    private void Close_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, e);

    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PlayerViewModel.CurrentStation))
            UpdateStationDetails();
    }

    /// <summary>Fill the panel for the newly selected station (favicon, homepage link).</summary>
    private async void UpdateStationDetails()
    {
        var station = PlayerViewModel.Instance.CurrentStation;
        if (station is null) return;

        if (Uri.TryCreate(station.Homepage, UriKind.Absolute, out var home) &&
            (home.Scheme == Uri.UriSchemeHttp || home.Scheme == Uri.UriSchemeHttps))
        {
            HomepageLink.NavigateUri = home;
            HomepageLink.Visibility = Visibility.Visible;
        }
        else
        {
            HomepageLink.Visibility = Visibility.Collapsed;
        }

        // Load the station's own logo; dead favicon links are very common in
        // the community database, so any failure keeps the app logo instead.
        var request = ++_faviconRequest;
        PanelFavicon.Source = new BitmapImage(new Uri("ms-appx:///Assets/bigwave-logo-src.png"));
        if (!Uri.TryCreate(station.Favicon, UriKind.Absolute, out var favicon) ||
            (favicon.Scheme != Uri.UriSchemeHttp && favicon.Scheme != Uri.UriSchemeHttps))
            return;

        try
        {
            var bytes = await FaviconHttp.GetByteArrayAsync(favicon);
            if (request != _faviconRequest) return; // a newer station won the race
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.SetSource(stream.AsRandomAccessStream());
            PanelFavicon.Source = image;
        }
        catch
        {
            // Keep the placeholder logo.
        }
    }
}
