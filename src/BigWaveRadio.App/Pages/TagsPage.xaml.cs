using BigWaveRadio.App.Services;
using BigWaveRadio.App.ViewModels;
using BigWaveRadio.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BigWaveRadio.App.Pages;

public partial class TagsPage : Page
{
    public TagsViewModel VM { get; } = new();

    public TagsPage()
    {
        InitializeComponent();
    }

    private void Tag_Click(object sender, RoutedEventArgs e)
    {
        // ItemsRepeater templates get no DataContext — the item rides in Tag.
        if (sender is FrameworkElement { Tag: RadioTag tag })
        {
            Frame.Navigate(typeof(StationsPage), new StationsPageArgs(
                $"#{tag.Name}",
                () => App.Client.GetStationsByTagAsync(tag.Name, minBitrate: FilterService.MinBitrate)));
        }
    }
}
