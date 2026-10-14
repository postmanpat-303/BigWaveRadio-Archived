using BigWaveRadio.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace BigWaveRadio.App.Controls;

/// <summary>
/// Ultra-modern tag word cloud. Words are scattered by a stable seeded
/// layout sized by station count; hovering a word glitches its letters
/// (jitter, accent-colour ghost and glyph swaps) while the button's hit
/// boundary stays pinned, so the word remains perfectly clickable.
/// </summary>
public sealed partial class WordCloud : UserControl
{
    private const int MaxWords = 48;
    private const int LayoutSeed = 42;

    private static readonly char[] GlitchGlyphs = { '▚', '▞', '▛', '▟', '▓', '▒', '#', '%', '&' };

    private readonly DispatcherTimer _relayout;
    private List<RadioTag>? _tags;
    private List<CloudWord>? _words;
    private readonly Random _glitchRnd = new();

    public WordCloud()
    {
        InitializeComponent();
        _relayout = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _relayout.Tick += (_, _) =>
        {
            _relayout.Stop();
            Layout();
        };
        Loaded += async (_, _) =>
        {
            try
            {
                Root.SizeChanged += (_, _) =>
                {
                    _relayout.Stop();
                    _relayout.Start();
                };
                if (_tags is null)
                    _tags = await LoadTagsAsync();
                Layout();
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "bwr-cloud-error.log"),
                    $"WordCloud.Loaded: {ex}{Environment.NewLine}");
            }
        };
    }

    /// <summary>Raised when a cloud word is activated; carries the tag name.</summary>
    public event TypedEventHandler<WordCloud, string>? TagClicked;

    private static async Task<List<RadioTag>> LoadTagsAsync()
    {
        try
        {
            var tags = await App.Client.GetTagsAsync(300);
            return tags
                .Where(t => !string.IsNullOrWhiteSpace(t.Name) && t.Name.Length <= 26 && t.StationCount >= 2)
                .OrderByDescending(t => t.StationCount)
                .Take(MaxWords)
                .ToList();
        }
        catch
        {
            return new List<RadioTag>();
        }
    }

    private void Layout()
    {
        if (_tags is null || _tags.Count == 0) return;
        var w = Root.ActualWidth;
        var h = Root.ActualHeight;
        if (w < 80 || h < 80) return;

        Root.Children.Clear();
        StopAllGlitches();

        var rnd = new Random(LayoutSeed); // stable arrangement across resizes
        var counts = _tags.Select(t => Math.Log(Math.Max(1, t.StationCount))).ToList();
        var lo = counts.Min();
        var hi = Math.Max(lo + 0.001, counts.Max());

        // Cloud silhouette: a union of ellipses (wide base, bumps on top).
        // Candidates are sampled inside the union so the whole ensemble
        // reads as one puffy cloud rather than a rectangle of words.
        var margin = 8.0;
        var halfW = (w - margin * 2) / 2;
        var halfH = (h - margin * 2) / 2;
        var cx = margin + halfW;
        var cy = margin + halfH;
        // Sampling weights favour the bumps over the base, so the top reads
        // as humps and the bottom stays flatter - a cartoon-cloud silhouette.
        (double rx, double ry, double ox, double oy, double weight)[] blobs =
        {
            (0.62, 0.34, 0.00, 0.26, 0.12), // wide, low base
            (0.34, 0.50, -0.34, -0.04, 0.20), // left bump
            (0.40, 0.80, 0.00, -0.26, 0.28), // tall centre bump
            (0.32, 0.46, 0.36, -0.04, 0.18), // right bump
            (0.24, 0.34, -0.22, -0.52, 0.11), // small top puff
            (0.22, 0.30, 0.26, -0.46, 0.11), // small top-right puff
        };

        (double x, double y) NextCandidate()
        {
            var pick = rnd.NextDouble() * blobs.Sum(b => b.weight);
            var blob = blobs[0];
            foreach (var b in blobs)
            {
                pick -= b.weight;
                if (pick <= 0) { blob = b; break; }
            }
            var angle = rnd.NextDouble() * Math.PI * 2;
            var radius = Math.Sqrt(rnd.NextDouble()); // uniform over the ellipse
            return (
                cx + (blob.ox + blob.rx * radius * Math.Cos(angle)) * halfW,
                cy + (blob.oy + blob.ry * radius * Math.Sin(angle)) * halfH);
        }

        _words = new List<CloudWord>();
        var placed = new List<Rect>();

        foreach (var (tag, i) in _tags.Select((t, i) => (t, i)))
        {
            var frac = (counts[i] - lo) / (hi - lo);
            var fontSize = 13 + 22 * frac;
            var word = CreateWord(tag, fontSize, frac);
            word.Button.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = word.Button.DesiredSize;

            // Pin the boundary to a fixed size before any glitch runs, so the
            // clickable area can never drift while letters misbehave. The box
            // keeps the button's full measured size, giving the text padding
            // slack so the last glyph is never clipped.
            word.Box.Width = size.Width;
            word.Box.Height = size.Height;

            // Rejection sampling: prefer the in-cloud candidate that overlaps
            // the least; fall back to the least-bad one when the cloud fills.
            var bestX = 0d; var bestY = 0d; var bestScore = double.MaxValue;
            for (var attempt = 0; attempt < 120; attempt++)
            {
                var (px, py) = NextCandidate();
                var x = Math.Clamp(px - size.Width / 2, 0, Math.Max(0, w - size.Width));
                var y = Math.Clamp(py - size.Height / 2, 0, Math.Max(0, h - size.Height));
                var score = OverlapScore(x, y, size.Width, size.Height, placed);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestX = x;
                    bestY = y;
                    if (score == 0 && attempt > 12) break;
                }
            }

            Canvas.SetLeft(word.Button, bestX);
            Canvas.SetTop(word.Button, bestY);
            Root.Children.Add(word.Button);
            placed.Add(new Rect(bestX - 8, bestY - 8, size.Width + 16, size.Height + 16));
            StartFloating(word, rnd);
            _words.Add(word);
        }
    }

    /// <summary>
    /// Gives a word its slow, forever-drifting float: a gentle vertical bob
    /// and a slightly detuned horizontal sway (sine-eased, auto-reversed,
    /// random phase) so the cloud breathes instead of sliding in lockstep.
    /// </summary>
    private static void StartFloating(CloudWord word, Random rnd)
    {
        var sb = new Storyboard { RepeatBehavior = RepeatBehavior.Forever, AutoReverse = true };

        var bob = new DoubleAnimation
        {
            From = 0,
            To = -(2.5 + rnd.NextDouble() * 3.5),
            Duration = new Duration(TimeSpan.FromSeconds(3.5 + rnd.NextDouble() * 3)),
            BeginTime = TimeSpan.FromSeconds(rnd.NextDouble() * 2.5),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        Storyboard.SetTarget(bob, word.Float);
        Storyboard.SetTargetProperty(bob, "Y");
        sb.Children.Add(bob);

        var sway = new DoubleAnimation
        {
            From = 0,
            To = (rnd.NextDouble() < 0.5 ? -1 : 1) * (1.5 + rnd.NextDouble() * 2.5),
            Duration = new Duration(TimeSpan.FromSeconds(4.5 + rnd.NextDouble() * 4)),
            BeginTime = TimeSpan.FromSeconds(rnd.NextDouble() * 2.5),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        Storyboard.SetTarget(sway, word.Float);
        Storyboard.SetTargetProperty(sway, "X");
        sb.Children.Add(sway);

        sb.Begin();
    }

    private CloudWord CreateWord(RadioTag tag, double fontSize, double frac)
    {
        var main = new TextBlock
        {
            Text = tag.Name,
            FontSize = fontSize,
            FontWeight = frac > 0.55 ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
            Opacity = 0.45 + 0.55 * frac,
            IsHitTestVisible = false,
        };
        var jitter = new TranslateTransform();
        main.RenderTransform = jitter;

        var ghost = new TextBlock
        {
            Text = tag.Name,
            FontSize = fontSize,
            FontWeight = main.FontWeight,
            Foreground = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransform = new TranslateTransform(),
        };

        var box = new Grid();
        box.Children.Add(ghost);
        box.Children.Add(main);

        // The button carries two transforms: the static tilt and the slow
        // float. Hit-testing follows RenderTransform, so the clickable area
        // drifts along with the word while it floats.
        var floatTransform = new TranslateTransform();
        var button = new Button
        {
            Content = box,
            Padding = new Thickness(6, 3, 6, 3),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(8),
            RenderTransform = new TransformGroup
            {
                Children =
                {
                    new RotateTransform { Angle = -10 + 20 * frac },
                    floatTransform,
                },
            },
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
        };
        ToolTipService.SetToolTip(button, $"{tag.StationCount:N0} stations");
        AutomationProperties.SetName(button, $"{tag.Name}, {tag.StationCount:N0} stations");

        var word = new CloudWord
        {
            Tag = tag,
            Button = button,
            Box = box,
            Main = main,
            Ghost = ghost,
            Jitter = jitter,
            Float = floatTransform,
        };

        // One handler per word, created once so Start/Stop subscribe and
        // unsubscribe the very same delegate.
        word.TickHandler = (_, _) => GlitchTick(word);
        button.PointerEntered += (_, _) => StartGlitch(word);
        button.PointerExited += (_, _) => StopGlitch(word);
        button.Click += (_, _) => TagClicked?.Invoke(this, word.Tag.Name);
        return word;
    }

    // ---- Glitch engine -----------------------------------------------------

    private void StartGlitch(CloudWord word)
    {
        // Faint accent wash behind the hovered word.
        word.Button.Background = (Brush)Application.Current.Resources["AccentFillColorTertiaryBrush"];

        word.Timer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        if (word.Timer.IsEnabled) return;
        word.Tick = 0;
        word.Timer.Tick += word.TickHandler!;
        word.Timer.Start();
    }

    private void StopGlitch(CloudWord word)
    {
        if (word.Timer is { IsEnabled: true })
        {
            word.Timer.Stop();
            word.Timer.Tick -= word.TickHandler!;
        }
        RestoreGlyph(word);
        word.Jitter.X = 0;
        word.Jitter.Y = 0;
        word.Ghost.Opacity = 0;
        word.Button.ClearValue(Button.BackgroundProperty);
    }

    private void GlitchTick(CloudWord word)
    {
        word.Tick++;

        // Micro jitter of the letters.
        word.Jitter.X = -1.6 + 3.2 * _glitchRnd.NextDouble();
        word.Jitter.Y = -1.2 + 2.4 * _glitchRnd.NextDouble();

        // Accent ghost split behind the text.
        if (word.Ghost.RenderTransform is TranslateTransform gt)
        {
            gt.X = (_glitchRnd.NextDouble() < 0.5 ? -1 : 1) * (2 + 2 * _glitchRnd.NextDouble());
            gt.Y = -1 + 2 * _glitchRnd.NextDouble();
        }
        word.Ghost.Opacity = 0.5;

        // Occasional single-glyph swap: restore the previous one first.
        RestoreGlyph(word);
        if (word.Tick % 2 == 0 && word.Main.Text.Length > 2)
        {
            var chars = word.Main.Text.ToCharArray();
            var idx = _glitchRnd.Next(chars.Length);
            word.SwapIndex = idx;
            word.SwapOriginal = chars[idx];
            chars[idx] = GlitchGlyphs[_glitchRnd.Next(GlitchGlyphs.Length)];
            word.Main.Text = new string(chars);
        }
    }

    private static void RestoreGlyph(CloudWord word)
    {
        if (word.SwapIndex >= 0 && word.SwapIndex < word.Main.Text.Length)
        {
            var chars = word.Main.Text.ToCharArray();
            chars[word.SwapIndex] = word.SwapOriginal;
            word.Main.Text = new string(chars);
            word.SwapIndex = -1;
        }
    }

    private void StopAllGlitches()
    {
        if (_words is null) return;
        foreach (var word in _words)
        {
            if (word.Timer is { IsEnabled: true })
            {
                word.Timer.Stop();
                word.Timer.Tick -= word.TickHandler!;
            }
            RestoreGlyph(word);
            word.Jitter.X = 0;
            word.Jitter.Y = 0;
            word.Ghost.Opacity = 0;
        }
    }

    // ---- Layout helpers ------------------------------------------------------

    private static double OverlapScore(double x, double y, double w, double h, List<Rect> placed)
    {
        var score = 0d;
        var candidate = new Rect(x, y, w, h);
        foreach (var rect in placed)
        {
            var ix = Math.Max(0, Math.Min(candidate.Right, rect.Right) - Math.Max(candidate.Left, rect.Left));
            var iy = Math.Max(0, Math.Min(candidate.Bottom, rect.Bottom) - Math.Max(candidate.Top, rect.Top));
            score += ix * iy;
        }
        return score;
    }

    private sealed class CloudWord
    {
        public required RadioTag Tag { get; init; }
        public required Button Button { get; init; }
        public required Grid Box { get; init; }
        public required TextBlock Main { get; init; }
        public required TextBlock Ghost { get; init; }
        public required TranslateTransform Jitter { get; init; }
        public required TranslateTransform Float { get; init; }
        public DispatcherTimer? Timer { get; set; }
        public EventHandler<object>? TickHandler { get; set; }
        public int Tick { get; set; }
        public int SwapIndex { get; set; } = -1;
        public char SwapOriginal { get; set; }
    }
}
