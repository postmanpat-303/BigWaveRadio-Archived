using BigWaveRadio.App.Controls;
using BigWaveRadio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BigWaveRadio.App.Pages;

public partial class LibraryPage : Page
{
    public LibraryViewModel VM { get; } = new();

    public LibraryPage()
    {
        InitializeComponent();
    }

    private void StationGrid_PlayRequested(StationGridView sender, StationItem item)
    {
        PlayerViewModel.Instance.Play(item.Station);
    }
}
