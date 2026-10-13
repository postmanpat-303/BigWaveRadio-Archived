using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.App.ViewModels;

public partial class CountriesViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasCountries;

    public ObservableCollection<RadioCountry> Countries { get; } = new();

    public CountriesViewModel() => _ = LoadAsync();

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var countries = await App.Client.GetCountriesAsync();
            Countries.Clear();
            foreach (var c in countries)
                Countries.Add(c);
            HasCountries = Countries.Count > 0;
        }
        catch
        {
            HasCountries = false;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
