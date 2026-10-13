using BigWaveRadio.App.Pages;
using BigWaveRadio.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.IO;

namespace BigWaveRadio.App;

public partial class MainWindow : Window
{
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

        // The MediaPlayerElement only exists to host the shared MediaPlayer
        // that PlayerViewModel drives.
        HiddenPlayer.SetMediaPlayer(PlayerViewModel.Instance.Player);

        // Select the default page AFTER the tree is built. Setting
        // IsSelected in XAML would fire SelectionChanged mid-parse, before
        // ContentFrame exists, and crash the XAML parser.
        NavView.SelectedItem = DiscoverItem;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
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
