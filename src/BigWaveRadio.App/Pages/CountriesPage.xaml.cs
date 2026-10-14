using BigWaveRadio.App.Services;
using BigWaveRadio.App.ViewModels;
using BigWaveRadio.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BigWaveRadio.App.Pages;

public partial class CountriesPage : Page
{
    public CountriesViewModel VM { get; } = new();

    public CountriesPage()
    {
        InitializeComponent();
    }

    private void Country_Click(object sender, RoutedEventArgs e)
    {
        // ItemsRepeater templates get no DataContext — the item rides in Tag.
        if (sender is FrameworkElement { Tag: RadioCountry country })
        {
            Frame.Navigate(typeof(StationsPage), new StationsPageArgs(
                country.Name,
                () => App.Client.GetStationsByCountryAsync(country.Name, minBitrate: FilterService.MinBitrate)));
        }
    }
}
