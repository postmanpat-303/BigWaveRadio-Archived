using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BigWaveRadio.Core.Models;

namespace BigWaveRadio.App.ViewModels;

public partial class TagsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasTags;

    public ObservableCollection<RadioTag> Tags { get; } = new();

    public TagsViewModel() => _ = LoadAsync();

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var tags = await App.Client.GetTagsAsync(300);
            Tags.Clear();
            foreach (var t in tags)
                Tags.Add(t);
            HasTags = Tags.Count > 0;
        }
        catch
        {
            HasTags = false;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
