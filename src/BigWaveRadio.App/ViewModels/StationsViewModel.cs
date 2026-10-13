using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.App.ViewModels;

/// <summary>Navigation argument for the generic station-list page.</summary>
/// <param name="Title">Page heading.</param>
/// <param name="Loader">How to fetch the stations for this view.</param>
public record StationsPageArgs(string Title, Func<Task<IReadOnlyList<Station>>> Loader);

/// <summary>
/// Backs the generic station list page (stations by tag / by country).
/// </summary>
public partial class StationsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasStations;

    public ObservableCollection<StationItem> Stations { get; } = new();

    public async Task LoadAsync(Func<Task<IReadOnlyList<Station>>> loader)
    {
        IsLoading = true;
        try
        {
            var stations = await loader();
            Stations.Clear();
            foreach (var s in stations)
                Stations.Add(new StationItem(s));
            HasStations = Stations.Count > 0;
        }
        catch
        {
            HasStations = false;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
