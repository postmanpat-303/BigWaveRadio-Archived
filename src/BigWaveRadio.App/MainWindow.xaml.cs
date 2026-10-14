using BigWaveRadio.App.Pages;
using BigWaveRadio.App.Services;
using BigWaveRadio.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.IO;

namespace BigWaveRadio.App;

public partial class MainWindow : Window
{
    private bool _panelOpen = true;

    public MainWindow()
    {
        InitializeComponent();

        Title = "BigWave Radio";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new MicaBackdrop();

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 520;
            presenter.PreferredMinimumHeight = 480;
        }

        try
        {
            AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "bigwave.ico"));
        }
        catch
        {
            // Icon is cosmetic — never block startup on it.
        }

        // The MediaPlayer is used standalone (audio-only) — attaching it to a
        // 0-size collapsed MediaPlayerElement tears down its video bridge and
        // silently stops playback.

        // Apply the saved theme and accent before the window is shown so there
        // is no light-to-dark flash on launch.
        ThemeService.Attach((FrameworkElement)Content, AppWindow);

        // Restore the saved bitrate filter without re-triggering a reload.
        foreach (var item in BitrateFilterCombo.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && int.TryParse(tag, out var value) && value == FilterService.MinBitrate)
            {
                BitrateFilterCombo.SelectedItem = item;
                break;
            }
        }

        // Keep the info panel in step with the player state.
        PlayerViewModel.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PlayerViewModel.HasStation) or nameof(PlayerViewModel.CurrentStation))
                UpdatePanelVisibility();
        };
        UpdatePanelVisibility();

        // Select the default page AFTER the tree is built. Setting
        // IsSelected in XAML would fire SelectionChanged mid-parse, before
        // ContentFrame exists, and crash the XAML parser.
        NavView.SelectedItem = DiscoverItem;

        // Debounce for the live title-bar search: fire 450 ms after the last keystroke.
        _searchDebounce = DispatcherQueue.GetForCurrentThread()?.CreateTimer()!;
        if (_searchDebounce is not null)
        {
            _searchDebounce.Interval = TimeSpan.FromMilliseconds(450);
            _searchDebounce.IsRepeating = false;
            _searchDebounce.Tick += OnSearchDebounceTick;
        }
    }

    // ---- Bitrate filter (nav pane) -----------------------------------------

    private void BitrateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BitrateFilterCombo.SelectedItem is not ComboBoxItem { Tag: string tag }) return;
        if (!int.TryParse(tag, out var min)) return;
        FilterService.SetMinBitrate(min);
        if (ContentFrame.Content is IBitrateFilterable page)
            page.ReloadForBitrateFilter();
    }

    // ---- Now-playing info panel ---------------------------------------------

    private void UpdatePanelVisibility()
    {
        InfoPanel.Visibility = PlayerViewModel.Instance.HasStation && _panelOpen
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void InfoToggle_Click(object sender, RoutedEventArgs e)
    {
        _panelOpen = InfoToggle.IsChecked == true;
        UpdatePanelVisibility();
    }

    private void InfoPanel_CloseRequested(object sender, RoutedEventArgs e)
    {
        _panelOpen = false;
        InfoToggle.IsChecked = false;
        UpdatePanelVisibility();
    }

    // ---- Global title-bar search ----------------------------------------------

    private readonly DispatcherQueueTimer _searchDebounce;

    private void GlobalSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.QueryText?.Trim() ?? string.Empty;
        if (query.Length == 0) return;
        _searchDebounce.Stop();
        RunGlobalSearch(query);
    }

    private void GlobalSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Live search: restart the debounce clock on every keystroke.
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private void OnSearchDebounceTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        var query = GlobalSearchBox.Text?.Trim() ?? string.Empty;
        if (query.Length >= 2)
            RunGlobalSearch(query);
        else if (query.Length == 0 && ContentFrame.Content is SearchPage page)
            page.VM.Reset(); // cleared the box → back to the tag cloud
    }

    private void RunGlobalSearch(string query)
    {
        // Select the Search nav item; SelectionChanged navigates there unless
        // we are already on it. Navigate is synchronous, so by this point the
        // frame holds the page we want to run the search on.
        NavView.SelectedItem = SearchItem;

        if (ContentFrame.Content is SearchPage page)
            page.RunSearch(query);
    }

    // ---- Navigation -----------------------------------------------------------

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            if (ContentFrame.SourcePageType != typeof(SettingsPage))
                ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is not NavigationViewItem item) return;

        var page = item.Tag switch
        {
            "search" => typeof(SearchPage),
            "library" => typeof(LibraryPage),
            "tags" => typeof(TagsPage),
            "countries" => typeof(CountriesPage),
            _ => typeof(DiscoverPage),
        };

        if (ContentFrame.SourcePageType != page)
            ContentFrame.Navigate(page);
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (ContentFrame.CanGoBack)
            ContentFrame.GoBack();
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        NavView.IsBackEnabled = ContentFrame.CanGoBack;
    }
}
