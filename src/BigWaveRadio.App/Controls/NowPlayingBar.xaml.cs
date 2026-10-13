using BigWaveRadio.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BigWaveRadio.App.Controls;

/// <summary>
/// Bottom bar showing the current station, transport controls, volume,
/// sleep timer and a quick "save to library" toggle.
/// </summary>
public sealed partial class NowPlayingBar : UserControl
{
    public NowPlayingBar()
    {
        InitializeComponent();
        DataContext = PlayerViewModel.Instance;
    }

    private void SleepItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string minutes)
            PlayerViewModel.Instance.SetSleepTimerCommand.Execute(minutes);
    }

    private void SleepOff_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.Instance.CancelSleepTimerCommand.Execute(null);
    }
}
