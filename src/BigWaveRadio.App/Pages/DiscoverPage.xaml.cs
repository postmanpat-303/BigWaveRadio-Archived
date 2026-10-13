using BigWaveRadio.App.Controls;
using BigWaveRadio.App.ViewModels;
using BigWaveRadio.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BigWaveRadio.App.Pages;

public partial class DiscoverPage : Page
{
    public DiscoverViewModel VM { get; } = new();

    public DiscoverPage()
    {
        InitializeComponent();
        ModeSelector.ItemsSource = VM.Modes;
    }

    private void StationGrid_PlayRequested(StationGridView sender, StationItem item)
    {
        PlayerViewModel.Instance.Play(item.Station);
    }

    private void Tag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: RadioTag tag })
        {
            Frame.Navigate(typeof(StationsPage), new StationsPageArgs(
                $"#{tag.Name}",
                () => App.Client.GetStationsByTagAsync(tag.Name)));
        }
    }
}
