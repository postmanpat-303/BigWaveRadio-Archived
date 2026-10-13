using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace BigWaveRadio.App.Controls;

/// <summary>
/// Ambient background: faint musical glyphs drift upward and dissolve.
/// Runs only while loaded and visible; cheap enough to leave running.
/// </summary>
public sealed partial class FloatingNotes : UserControl
{
    private static readonly string[] Glyphs = { "♪", "♫", "♩", "♬", "♭", "♯" };

    private const int MaxNotes = 16;

    private readonly DispatcherTimer _spawner;
    private readonly Random _rnd = new();
    private int _active;
    private bool _loaded;

    public FloatingNotes()
    {
        InitializeComponent();
        _spawner = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
        _spawner.Tick += (_, _) => SpawnNote();
        Loaded += (_, _) =>
        {
            try
            {
                _loaded = true;
                // Seed a few notes so the field is alive immediately.
                for (var i = 0; i < 4; i++) SpawnNote();
                _spawner.Start();
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "bwr-cloud-error.log"),
                    $"FloatingNotes.Loaded: {ex}{Environment.NewLine}");
            }
        };
        Unloaded += (_, _) =>
        {
            _loaded = false;
            _spawner.Stop();
        };
    }

    private void SpawnNote()
    {
        try
        {
            if (!_loaded || _active >= MaxNotes) return;
        var w = Field.ActualWidth;
        var h = Field.ActualHeight;
        if (w < 50 || h < 50) return;

        _active++;

        var note = new TextBlock
        {
            Text = Glyphs[_rnd.Next(Glyphs.Length)],
            FontSize = 12 + _rnd.NextDouble() * 16,
            Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransform = new RotateTransform { Angle = -14 + _rnd.NextDouble() * 28 },
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
        };

        var x = _rnd.NextDouble() * Math.Max(1, w - 30);
        var startY = h + 30;
        var rise = 10 + _rnd.NextDouble() * 6;          // seconds to cross the field
        var peak = 0.08 + _rnd.NextDouble() * 0.10;    // faint but perceptible
        var sway = (_rnd.NextDouble() - 0.5) * 60;

        Canvas.SetLeft(note, x);
        Canvas.SetTop(note, startY);
        Field.Children.Add(note);

        var sb = new Storyboard();

        var riseAnim = new DoubleAnimation
        {
            From = startY,
            To = -50,
            Duration = new Duration(TimeSpan.FromSeconds(rise)),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(riseAnim, note);
        Storyboard.SetTargetProperty(riseAnim, "(Canvas.Top)");
        sb.Children.Add(riseAnim);

        var swayAnim = new DoubleAnimation
        {
            From = x,
            To = x + sway,
            Duration = new Duration(TimeSpan.FromSeconds(rise)),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        Storyboard.SetTarget(swayAnim, note);
        Storyboard.SetTargetProperty(swayAnim, "(Canvas.Left)");
        sb.Children.Add(swayAnim);

        // One keyframed Opacity animation: fade in, hold faint, dissolve at the
        // top. (A storyboard may hold only one animation per property/element.)
        var fade = new DoubleAnimationUsingKeyFrames();
        fade.KeyFrames.Add(new LinearDoubleKeyFrame { Value = 0, KeyTime = TimeSpan.Zero });
        fade.KeyFrames.Add(new LinearDoubleKeyFrame { Value = peak, KeyTime = TimeSpan.FromSeconds(1.2) });
        fade.KeyFrames.Add(new LinearDoubleKeyFrame { Value = peak, KeyTime = TimeSpan.FromSeconds(Math.Max(1.3, rise - 2.5)) });
        fade.KeyFrames.Add(new LinearDoubleKeyFrame { Value = 0, KeyTime = TimeSpan.FromSeconds(rise) });
        Storyboard.SetTarget(fade, note);
        Storyboard.SetTargetProperty(fade, "Opacity");
        sb.Children.Add(fade);

        sb.Completed += (_, _) =>
        {
            Field.Children.Remove(note);
            _active--;
        };
        sb.Begin();
        }
        catch (Exception ex)
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "bwr-cloud-error.log"),
                $"FloatingNotes.SpawnNote: {ex}{Environment.NewLine}");
        }
    }
}
