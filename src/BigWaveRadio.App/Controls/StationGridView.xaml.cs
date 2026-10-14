using System.Collections;
using BigWaveRadio.App.Services;
using BigWaveRadio.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace BigWaveRadio.App.Controls;

/// <summary>
/// Adaptive, wrap-around grid of station cards. Fires <see cref="PlayRequested"/>
/// when a card is activated; the library star is handled internally.
/// </summary>
public sealed partial class StationGridView : UserControl
{
    /// <summary>Shared hover tint used when a custom accent colour is active (~15% opacity).</summary>
    private static readonly SolidColorBrush AccentHoverBrush = new(Colors.Transparent);

    static StationGridView()
    {
        // One subscription for every card: recolour the shared tint live.
        ThemeService.AccentChanged += (_, _) => UpdateAccentHoverBrush();
        UpdateAccentHoverBrush();
    }

    private static void UpdateAccentHoverBrush()
    {
        var c = ThemeService.Settings.Accent == "system"
            ? Colors.Transparent
            : ThemeService.CurrentAccentColor;
        AccentHoverBrush.Color = Color.FromArgb(0x26, c.R, c.G, c.B);
    }
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items),
        typeof(IList),
        typeof(StationGridView),
        new PropertyMetadata(Array.Empty<object>(), OnStateChanged));

    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading),
        typeof(bool),
        typeof(StationGridView),
        new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(
        nameof(EmptyText),
        typeof(string),
        typeof(StationGridView),
        new PropertyMetadata("Nothing here yet."));

    public static readonly DependencyProperty ShowEmptyStateProperty = DependencyProperty.Register(
        nameof(ShowEmptyState),
        typeof(bool),
        typeof(StationGridView),
        new PropertyMetadata(false));

    public IList Items
    {
        get => (IList)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    /// <summary>True when the load finished with no items — drives the empty-state message.</summary>
    public bool ShowEmptyState
    {
        get => (bool)GetValue(ShowEmptyStateProperty);
        private set => SetValue(ShowEmptyStateProperty, value);
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (StationGridView)d;
        view.ShowEmptyState = !view.IsLoading && view.Items.Count == 0;
    }

    public event TypedEventHandler<StationGridView, StationItem>? PlayRequested;

    public StationGridView()
    {
        InitializeComponent();
    }

    private void Card_Click(object sender, RoutedEventArgs e)
    {
        // ItemsRepeater does not set DataContext on data-template roots,
        // so the StationItem is carried in Tag instead.
        if (sender is FrameworkElement { Tag: StationItem item })
        {
            // Hand the player the whole visible list so Previous/Next can walk it.
            ViewModels.PlayerViewModel.Instance.SetQueue(Items);
            PlayRequested?.Invoke(this, item);
        }
    }

    private void Star_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: StationItem item })
            item.ToggleLibrary();
    }

    private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not DependencyObject root) return;
        if (FindHoverFill(root) is not { } hover) return;

        // Custom accent: tint with the chosen colour. "Match system": keep the
        // theme-resource grey that was set in XAML (ClearValue restores it).
        if (ThemeService.Settings.Accent == "system")
            hover.ClearValue(Border.BackgroundProperty);
        else
            hover.Background = AccentHoverBrush;
        hover.Opacity = 1;
    }

    private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is DependencyObject root && FindHoverFill(root) is { } hover)
            hover.Opacity = 0;
    }

    private static Border? FindHoverFill(DependencyObject node)
    {
        var current = node;
        while (current is not null)
        {
            if (SearchDown(current) is { } found)
                return found;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;

        static Border? SearchDown(DependencyObject n)
        {
            var count = VisualTreeHelper.GetChildrenCount(n);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(n, i);
                if (child is Border { Name: "HoverFill" } match)
                    return match;
                if (SearchDown(child) is { } found)
                    return found;
            }
            return null;
        }
    }
}
