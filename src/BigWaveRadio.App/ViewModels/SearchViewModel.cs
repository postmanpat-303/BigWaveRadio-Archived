using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.App.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasSearched;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private string _lastQuery = string.Empty;

    /// <summary>Return to the idle tag-cloud state, dropping any results.</summary>
    public void Reset()
    {
        Stations.Clear();
        HasSearched = false;
    }

    public ObservableCollection<StationItem> Stations { get; } = new();

    public async Task SearchAsync(string query)
    {
        query = query.Trim();
        if (query.Length == 0) return;

        LastQuery = query;
        IsLoading = true;
        HasSearched = true;
        try
        {
            var stations = await App.Client.SearchStationsAsync(query);
            Stations.Clear();
            foreach (var s in stations)
                Stations.Add(new StationItem(s));
            HasResults = Stations.Count > 0;
        }
        catch
        {
            Stations.Clear();
            HasResults = false;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
