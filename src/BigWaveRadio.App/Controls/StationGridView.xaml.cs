using System.Collections;
using BigWaveRadio.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace BigWaveRadio.App.Controls;

/// <summary>
/// Adaptive, wrap-around grid of station cards. Fires <see cref="PlayRequested"/>
/// when a card is activated; the library star is handled internally.
/// </summary>
public sealed partial class StationGridView : UserControl
{
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
        if (sender is FrameworkElement element && element.DataContext is StationItem item)
            PlayRequested?.Invoke(this, item);
    }

    private void Star_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is StationItem item)
            item.ToggleLibrary();
    }
}
