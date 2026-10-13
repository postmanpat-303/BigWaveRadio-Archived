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
        if (sender is FrameworkElement element && element.DataContext is RadioCountry country)
        {
            Frame.Navigate(typeof(StationsPage), new StationsPageArgs(
                country.Name,
                () => App.Client.GetStationsByCountryAsync(country.Name)));
        }
    }
}
