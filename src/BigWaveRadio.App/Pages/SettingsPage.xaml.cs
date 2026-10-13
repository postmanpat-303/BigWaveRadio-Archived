using BigWaveRadio.App.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace BigWaveRadio.App.Pages;

/// <summary>Appearance settings: light/dark theme and accent (highlight) colour.</summary>
public sealed partial class SettingsPage : Page
{
    private readonly Dictionary<string, Border> _swatchRings = new();

    public SettingsPage()
    {
        InitializeComponent();

        // Reflect the saved theme without firing the Checked handlers.
        var theme = ThemeService.Settings.Theme;
        ThemeSystemRadio.IsChecked = theme == "system";
        ThemeLightRadio.IsChecked = theme == "light";
        ThemeDarkRadio.IsChecked = theme == "dark";

        BuildAccentSwatches();
        ThemeService.AccentChanged += (_, _) => HighlightSelectedAccent();
    }

    private void BuildAccentSwatches()
    {
        AccentPanel.Children.Clear();
        _swatchRings.Clear();

        foreach (var option in ThemeService.AccentOptions)
        {
            var ring = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                BorderThickness = new Thickness(3),
                Padding = new Thickness(3),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Child = new Ellipse
                {
                    Fill = option.Color is { } c
                        ? new SolidColorBrush(c)
                        : SystemAccentIconFill(),
                },
            };
            ToolTipService.SetToolTip(ring, option.DisplayName);

            var button = new Button
            {
                Tag = option.Key,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                MinWidth = 0,
                MinHeight = 0,
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(22),
                Content = ring,
            };
            button.Click += AccentSwatch_Click;

            _swatchRings[option.Key] = ring;
            AccentPanel.Children.Add(button);
        }

        HighlightSelectedAccent();
    }

    /// <summary>Swatch face for "match system": a diagonal blend across three accent hues.</summary>
    private static Brush SystemAccentIconFill() => new LinearGradientBrush
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(255, 0x00, 0x78, 0xD4), Offset = 0.0 },
            new GradientStop { Color = Color.FromArgb(255, 0x67, 0x8B, 0xD8), Offset = 0.5 },
            new GradientStop { Color = Color.FromArgb(255, 0x9B, 0x4F, 0x96), Offset = 1.0 },
        },
    };

    private void AccentSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string key })
        {
            ThemeService.SetAccent(key);
            HighlightSelectedAccent();
        }
    }

    private void HighlightSelectedAccent()
    {
        var selected = ThemeService.Settings.Accent;
        var ringBrush = new SolidColorBrush(ThemeService.CurrentAccentColor);
        foreach (var (key, ring) in _swatchRings)
        {
            // The ring follows the live accent colour, so it recolours the moment
            // a new highlight is chosen — instant feedback on the swatch itself.
            ring.BorderBrush = key == selected ? ringBrush : new SolidColorBrush(Colors.Transparent);
        }
    }

    private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string theme })
            ThemeService.SetTheme(theme);
    }
}
