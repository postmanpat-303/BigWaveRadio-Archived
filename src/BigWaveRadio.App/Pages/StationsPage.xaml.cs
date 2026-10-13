using BigWaveRadio.App.Controls;
using BigWaveRadio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BigWaveRadio.App.Pages;

/// <summary>
/// Generic station list used for "stations by tag" and "stations by country".
/// Receives a <see cref="StationsPageArgs"/> through navigation.
/// </summary>
public partial class StationsPage : Page
{
    public StationsViewModel VM { get; } = new();

    public StationsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is StationsPageArgs args)
        {
            TitleText.Text = args.Title;
            _ = VM.LoadAsync(args.Loader);
        }
    }

    private void StationGrid_PlayRequested(StationGridView sender, StationItem item)
    {
        PlayerViewModel.Instance.Play(item.Station);
    }
}
