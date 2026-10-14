using BigWaveRadio.App.Controls;
using BigWaveRadio.App.Services;
using BigWaveRadio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BigWaveRadio.App.Pages;

public partial class SearchPage : Page, IBitrateFilterable
{
    public SearchViewModel VM { get; } = new();

    public SearchPage()
    {
        InitializeComponent();
    }

    public void ReloadForBitrateFilter() => VM.Reload();

    private void QueryBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _ = VM.SearchAsync(args.QueryText);
    }

    private void Cloud_TagClicked(WordCloud sender, string tagName)
    {
        QueryBox.Text = tagName;
        _ = VM.SearchAsync(tagName);
    }

    private void BackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        VM.Reset();
    }

    private void StationGrid_PlayRequested(StationGridView sender, StationItem item)
    {
        PlayerViewModel.Instance.Play(item.Station);
    }
}
