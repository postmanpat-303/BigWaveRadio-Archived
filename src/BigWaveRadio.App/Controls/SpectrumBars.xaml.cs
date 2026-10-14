using BigWaveRadio.App.Services;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BigWaveRadio.App.Controls;

/// <summary>
/// 32-bar realtime audio spectrum. Bars rise from the bottom, coloured with a
/// gradient built from the user's chosen highlight colour. Driven by
/// <see cref="SpectrumService"/> while active; decays to silence otherwise.
/// </summary>
public sealed partial class SpectrumBars : UserControl
{
    private const int Bars = SpectrumService.BandCount;
    private const double TickMs = 33; // ~30 fps

    private readonly Border[] _bar = new Border[Bars];
    private readonly DispatcherQueueTimer? _timer;

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(SpectrumBars),
            new PropertyMetadata(false));

    /// <summary>True while audio is playing and the bars should animate.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public SpectrumBars()
    {
        InitializeComponent();

        var grid = new Grid();
        for (int i = 0; i < Bars; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var bar = new Border
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 2,
                MinWidth = 2,
                CornerRadius = new CornerRadius(2),
            };
            var wrap = new Grid { Margin = new Thickness(0, 0, i < Bars - 1 ? 3 : 0, 0) };
            wrap.Children.Add(bar);
            Grid.SetColumn(wrap, i);
            grid.Children.Add(wrap);
            _bar[i] = bar;
        }
        RootGrid.Children.Add(grid);

        RebuildBrushes();
        ThemeService.AccentChanged += (_, _) => RebuildBrushes();
        ActualThemeChanged += (_, _) => RebuildBrushes();

        _timer = DispatcherQueue.GetForCurrentThread()?.CreateTimer();
        if (_timer is not null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(TickMs);
            _timer.IsRepeating = true;
            _timer.Tick += OnTick;
            _timer.Start();
        }
    }

    private bool _decayed = true;

    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        if (!IsActive)
        {
            // Vanish entirely once, then go idle so dozens of card
            // visualisers don't burn frames doing nothing.
            if (_decayed) return;
            _decayed = true;
            foreach (var bar in _bar)
                bar.Height = 0;
            return;
        }

        _decayed = false;
        var levels = SpectrumService.Instance.Snapshot();
        double h = ActualHeight;
        for (int i = 0; i < Bars; i++)
            _bar[i].Height = Math.Max(2, levels[i] * h);
    }

    /// <summary>Gradient across the bars: accent → softened accent (direction follows the theme).</summary>
    private void RebuildBrushes()
    {
        var accent = ThemeService.CurrentAccentColor;
        bool dark = ActualTheme == ElementTheme.Dark
            || (ActualTheme == ElementTheme.Default && ThemeService.Settings.Theme != "light");

        for (int i = 0; i < Bars; i++)
        {
            double t = (double)i / (Bars - 1);
            var end = Blend(accent, dark ? Colors.White : Colors.Black, dark ? 0.55 : 0.35);
            var color = Blend(accent, end, t);
            _bar[i].Background = new SolidColorBrush(color);
            _bar[i].Opacity = 0.55 + 0.45 * (1 - Math.Abs(t - 0.5) * 2); // brightest in the middle
        }
    }

    private static Color Blend(Color a, Color b, double t) => Color.FromArgb(
        255,
        (byte)(a.R * (1 - t) + b.R * t),
        (byte)(a.G * (1 - t) + b.G * t),
        (byte)(a.B * (1 - t) + b.B * t));
}
