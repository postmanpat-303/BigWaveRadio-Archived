using BigWaveRadio.App.Controls;
using BigWaveRadio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BigWaveRadio.App.Pages;

public partial class SearchPage : Page
{
    public SearchViewModel VM { get; } = new();

    public SearchPage()
    {
        InitializeComponent();
    }

    private void QueryBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _ = VM.SearchAsync(args.QueryText);
    }

    private void StationGrid_PlayRequested(StationGridView sender, StationItem item)
    {
        PlayerViewModel.Instance.Play(item.Station);
    }
}
