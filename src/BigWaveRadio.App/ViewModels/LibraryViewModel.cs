using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.App.ViewModels;

/// <summary>
/// Backs "My Library": the quick-reference list of pinned stations.
/// </summary>
public partial class LibraryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _hasStations;

    public ObservableCollection<StationItem> Stations { get; } = new();

    public LibraryViewModel()
    {
        App.Library.Changed += (_, _) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        Stations.Clear();
        foreach (var station in App.Library.Stations)
            Stations.Add(new StationItem(station));
        HasStations = Stations.Count > 0;
    }
}
