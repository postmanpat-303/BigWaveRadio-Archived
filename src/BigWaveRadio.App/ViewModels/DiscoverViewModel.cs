using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.App.ViewModels;

/// <summary>
/// The Discover page: a mode switch (Trending / Popular / Recently added),
/// the station grid and a row of top tags for exploration.
/// </summary>
public partial class DiscoverViewModel : ObservableObject
{
    public IReadOnlyList<string> Modes { get; } = new[] { "Trending", "Popular", "Recently added" };

    [ObservableProperty]
    private string _selectedMode = "Trending";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasStations;

    public ObservableCollection<StationItem> Stations { get; } = new();

    public ObservableCollection<RadioTag> TopTags { get; } = new();

    public DiscoverViewModel()
    {
        _ = LoadAsync();
        _ = LoadTagsAsync();
    }

    partial void OnSelectedModeChanged(string value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var stations = SelectedMode switch
            {
                "Popular" => await App.Client.GetTopClickedAsync(60),
                "Recently added" => await App.Client.GetRecentlyChangedAsync(60),
                _ => await App.Client.GetTopVotedAsync(60),
            };
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

    private async Task LoadTagsAsync()
    {
        try
        {
            var tags = await App.Client.GetTagsAsync(24);
            TopTags.Clear();
            foreach (var t in tags)
                TopTags.Add(t);
        }
        catch
        {
            // Tags row is a bonus; grid still works without it.
        }
    }
}
