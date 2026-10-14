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

        // Live search: run 450 ms after the user stops typing.
        _debounce = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.CreateTimer()!;
        if (_debounce is not null)
        {
            _debounce.Interval = TimeSpan.FromMilliseconds(450);
            _debounce.IsRepeating = false;
            _debounce.Tick += (_, _) =>
            {
                _debounce.Stop();
                var q = QueryBox.Text?.Trim() ?? string.Empty;
                if (q.Length >= 2) _ = VM.SearchAsync(q);
                else if (q.Length == 0) VM.Reset();
            };
        }
    }

    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer? _debounce;

    private void QueryBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        _debounce?.Stop();
        _debounce?.Start();
    }

    public void ReloadForBitrateFilter() => VM.Reload();

    /// <summary>Run a search from anywhere in the app (e.g. the title-bar search box).</summary>
    public void RunSearch(string query)
    {
        QueryBox.Text = query;
        _ = VM.SearchAsync(query);
    }

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
