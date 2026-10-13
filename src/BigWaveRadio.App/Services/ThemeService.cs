using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace BigWaveRadio.App.Services;

/// <summary>A selectable highlight colour; <see cref="Color"/> is null for "follow the system accent".</summary>
public sealed record AccentOption(string Key, string DisplayName, Color? Color);

/// <summary>Persisted appearance settings (LocalAppData\BigWaveRadio\settings.json).</summary>
public sealed class AppearanceSettings
{
    public string Theme { get; set; } = "system";   // system | light | dark
    public string Accent { get; set; } = "system";  // system | <accent key>
}

/// <summary>
/// Applies and persists the app's light/dark theme and accent (highlight) colour.
/// Theme is set on the window root so every page follows; accent colours override
/// the framework's SystemAccentColor-derived brushes in per-theme dictionaries,
/// which is what makes buttons, sliders and the play controls recolour live.
/// </summary>
public static class ThemeService
{
    public static AppearanceSettings Settings { get; private set; } = new();

    public static IReadOnlyList<AccentOption> AccentOptions { get; } = new List<AccentOption>
    {
        new("system", "Match system accent", null),
        new("ocean",  "Ocean blue",  Color.FromArgb(255, 0x00, 0x78, 0xD4)),
        new("teal",   "Teal",        Color.FromArgb(255, 0x03, 0x83, 0x87)),
        new("forest", "Forest green",Color.FromArgb(255, 0x10, 0x7C, 0x10)),
        new("gold",   "Gold",        Color.FromArgb(255, 0xC1, 0x9C, 0x00)),
        new("ember",  "Ember orange",Color.FromArgb(255, 0xCA, 0x50, 0x10)),
        new("ruby",   "Ruby red",    Color.FromArgb(255, 0xD1, 0x34, 0x38)),
        new("pink",   "Hot pink",    Color.FromArgb(255, 0xE3, 0x00, 0x8C)),
        new("violet", "Violet",      Color.FromArgb(255, 0x74, 0x4D, 0xA9)),
    };

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BigWaveRadio");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static FrameworkElement? _root;
    private static AppWindow? _appWindow;

    /// <summary>The accent colour currently in effect (the system accent when set to "match system").</summary>
    public static Color CurrentAccentColor { get; private set; } = GetSystemAccent();

    /// <summary>Raised after the effective accent colour changes, so UI can refresh.</summary>
    public static event EventHandler? AccentChanged;

    private static readonly string[] AccentColorKeys =
    {
        "SystemAccentColor",
        "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
        "TextControlSelectionHighlightColor",
    };

    private static readonly string[] AccentBrushKeys =
    {
        // Core accent fills / strokes / text
        "AccentFillColorDefaultBrush",
        "AccentFillColorSecondaryBrush",
        "AccentFillColorTertiaryBrush",
        "AccentStrokeColorDefaultBrush",
        "AccentTextFillColorPrimaryBrush",
        "AccentTextFillColorSecondaryBrush",
        "AccentTextFillColorTertiaryBrush",
        "AccentTextFillColorDisabledBrush",
        "TextOnAccentFillColorPrimaryBrush",
        "TextOnAccentFillColorSecondaryBrush",
        "TextOnAccentFillColorDisabledBrush",
        // Checked toggle buttons / accent buttons
        "AccentButtonBackground",
        "AccentButtonBackgroundPointerOver",
        "AccentButtonBackgroundPressed",
        "AccentButtonBackgroundDisabled",
        "AccentButtonForeground",
        "AccentButtonForegroundPointerOver",
        "AccentButtonForegroundPressed",
        "AccentButtonForegroundDisabled",
        "AccentButtonBorderBrush",
        "AccentButtonBorderBrushPointerOver",
        "AccentButtonBorderBrushPressed",
        "AccentButtonBorderBrushDisabled",
        // Slider value track + thumb
        "SliderTrackValueFill",
        "SliderTrackValueFillPointerOver",
        "SliderTrackValueFillPressed",
        "SliderTrackValueFillDisabled",
        "SliderThumbBackground",
        "SliderThumbBackgroundPointerOver",
        "SliderThumbBackgroundPressed",
        "SliderThumbBackgroundDisabled",
        // RadioButton checked disc + ring
        "RadioButtonOuterEllipseCheckedFill",
        "RadioButtonOuterEllipseCheckedFillPointerOver",
        "RadioButtonOuterEllipseCheckedFillPressed",
        "RadioButtonOuterEllipseCheckedFillDisabled",
        "RadioButtonOuterEllipseCheckedStroke",
        "RadioButtonOuterEllipseCheckedStrokePointerOver",
        "RadioButtonOuterEllipseCheckedStrokePressed",
        // NavigationView selected-item indicator
        "NavigationViewSelectionIndicatorForeground",
    };

    public static async Task LoadAsync()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = await File.ReadAllTextAsync(FilePath);
                var loaded = JsonSerializer.Deserialize<AppearanceSettings>(json);
                if (loaded is not null)
                    Settings = loaded;
            }
        }
        catch
        {
            // Corrupt or locked settings file — start from defaults.
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(Settings);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Persistence is best-effort; the live session always reflects the choice.
        }
    }

    /// <summary>Wire the service to a window (called once at startup, before the window is shown).</summary>
    public static void Attach(FrameworkElement root, AppWindow appWindow)
    {
        _root = root;
        _appWindow = appWindow;
        ApplyTheme();
        ApplyAccent();
    }

    public static void SetTheme(string theme)
    {
        if (Settings.Theme == theme) return;
        Settings.Theme = theme;
        Save();
        ApplyTheme();
    }

    public static void SetAccent(string key)
    {
        if (Settings.Accent == key) return;
        Settings.Accent = key;
        Save();
        ApplyAccent();
    }

    private static ElementTheme ResolveTheme() => Settings.Theme switch
    {
        "light" => ElementTheme.Light,
        "dark" => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    private static void ApplyTheme()
    {
        if (_root is null) return;

        _root.RequestedTheme = ResolveTheme();

        if (_appWindow is null) return;
        var dark = Settings.Theme switch
        {
            "dark" => true,
            "light" => false,
            _ => IsSystemDark(),
        };
        var titleBar = _appWindow.TitleBar;
        titleBar.BackgroundColor = Colors.Transparent;
        titleBar.InactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ForegroundColor = dark ? Colors.White : Colors.Black;
        titleBar.InactiveForegroundColor = dark ? Colors.DarkGray : Colors.Gray;
        titleBar.ButtonForegroundColor = dark ? Colors.White : Colors.Black;
        titleBar.ButtonHoverBackgroundColor = dark ? Color.FromArgb(255, 45, 45, 45) : Color.FromArgb(255, 225, 225, 225);
        titleBar.ButtonHoverForegroundColor = dark ? Colors.White : Colors.Black;
        titleBar.ButtonPressedBackgroundColor = dark ? Color.FromArgb(255, 65, 65, 65) : Color.FromArgb(255, 205, 205, 205);
        titleBar.ButtonPressedForegroundColor = dark ? Colors.White : Colors.Black;
    }

    private static bool IsSystemDark()
    {
        try
        {
            var ui = new UISettings();
            var bg = ui.GetColorValue(UIColorType.Background);
            return (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) < 128;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyAccent()
    {
        var option = AccentOptions.FirstOrDefault(a => a.Key == Settings.Accent) ?? AccentOptions[0];

        if (option.Color is not { } accent)
        {
            // "Match system" — drop every override so the framework colours return.
            foreach (var key in AccentColorKeys)
                Application.Current.Resources.Remove(key);
            foreach (var key in AccentBrushKeys)
                Application.Current.Resources.Remove(key);
            CurrentAccentColor = GetSystemAccent();
            AccentChanged?.Invoke(null, EventArgs.Empty);
            return;
        }

        var light1 = Blend(accent, Colors.White, 0.30);
        var light2 = Blend(accent, Colors.White, 0.55);
        var light3 = Blend(accent, Colors.White, 0.75);
        var dark1 = Blend(accent, Colors.Black, 0.15);
        var dark2 = Blend(accent, Colors.Black, 0.30);
        var dark3 = Blend(accent, Colors.Black, 0.45);
        var onAccent = IsLight(accent) ? Colors.Black : Colors.White;

        // Colour keys feed anything still bound to SystemAccentColor variants.
        var colorValues = new Dictionary<string, Color>
        {
            ["SystemAccentColor"] = accent,
            ["SystemAccentColorLight1"] = light1,
            ["SystemAccentColorLight2"] = light2,
            ["SystemAccentColorLight3"] = light3,
            ["SystemAccentColorDark1"] = dark1,
            ["SystemAccentColorDark2"] = dark2,
            ["SystemAccentColorDark3"] = dark3,
            ["TextControlSelectionHighlightColor"] = accent,
        };
        foreach (var (key, value) in colorValues)
            Application.Current.Resources[key] = value;

        // Brush keys as flat app-level overrides. Runtime-added ThemeDictionaries
        // entries do NOT invalidate live ThemeResource lookups in WinUI 3, but
        // flat Application.Resources entries do. Controls resolve their accent
        // colours through many different themed keys (core accent brushes,
        // AccentButton, Slider, RadioButton, NavigationView), so the whole set
        // is overridden here with per-state shades of the chosen colour.
        var disabled = Brush(Blend(accent, Colors.Gray, 0.65));
        var brushes = new Dictionary<string, SolidColorBrush>
        {
            ["AccentFillColorDefaultBrush"] = Brush(accent),
            ["AccentFillColorSecondaryBrush"] = Brush(dark1),
            ["AccentFillColorTertiaryBrush"] = Brush(dark2),
            ["AccentStrokeColorDefaultBrush"] = Brush(dark2),
            ["AccentTextFillColorPrimaryBrush"] = Brush(dark2),
            ["AccentTextFillColorSecondaryBrush"] = Brush(dark1),
            ["AccentTextFillColorTertiaryBrush"] = Brush(accent),
            ["AccentTextFillColorDisabledBrush"] = disabled,
            ["TextOnAccentFillColorPrimaryBrush"] = Brush(onAccent),
            ["TextOnAccentFillColorSecondaryBrush"] = Brush(onAccent, 0.70),
            ["TextOnAccentFillColorDisabledBrush"] = Brush(Blend(onAccent, Colors.Gray, 0.55)),

            ["AccentButtonBackground"] = Brush(accent),
            ["AccentButtonBackgroundPointerOver"] = Brush(light1),
            ["AccentButtonBackgroundPressed"] = Brush(dark1),
            ["AccentButtonBackgroundDisabled"] = disabled,
            ["AccentButtonForeground"] = Brush(onAccent),
            ["AccentButtonForegroundPointerOver"] = Brush(onAccent),
            ["AccentButtonForegroundPressed"] = Brush(onAccent),
            ["AccentButtonForegroundDisabled"] = Brush(onAccent, 0.60),
            ["AccentButtonBorderBrush"] = Brush(dark2),
            ["AccentButtonBorderBrushPointerOver"] = Brush(dark2),
            ["AccentButtonBorderBrushPressed"] = Brush(dark2),
            ["AccentButtonBorderBrushDisabled"] = disabled,

            ["SliderTrackValueFill"] = Brush(accent),
            ["SliderTrackValueFillPointerOver"] = Brush(light1),
            ["SliderTrackValueFillPressed"] = Brush(dark1),
            ["SliderTrackValueFillDisabled"] = disabled,
            ["SliderThumbBackground"] = Brush(accent),
            ["SliderThumbBackgroundPointerOver"] = Brush(light1),
            ["SliderThumbBackgroundPressed"] = Brush(dark1),
            ["SliderThumbBackgroundDisabled"] = disabled,

            ["RadioButtonOuterEllipseCheckedFill"] = Brush(accent),
            ["RadioButtonOuterEllipseCheckedFillPointerOver"] = Brush(light1),
            ["RadioButtonOuterEllipseCheckedFillPressed"] = Brush(dark1),
            ["RadioButtonOuterEllipseCheckedFillDisabled"] = disabled,
            ["RadioButtonOuterEllipseCheckedStroke"] = Brush(accent),
            ["RadioButtonOuterEllipseCheckedStrokePointerOver"] = Brush(light1),
            ["RadioButtonOuterEllipseCheckedStrokePressed"] = Brush(dark1),

            ["NavigationViewSelectionIndicatorForeground"] = Brush(accent),
        };
        foreach (var (key, value) in brushes)
            Application.Current.Resources[key] = value;

        CurrentAccentColor = accent;
        AccentChanged?.Invoke(null, EventArgs.Empty);
    }

    private static Color GetSystemAccent()
    {
        try
        {
            return new UISettings().GetColorValue(UIColorType.Accent);
        }
        catch
        {
            return Color.FromArgb(255, 0x00, 0x78, 0xD4);
        }
    }

    private static bool IsLight(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) > 140;

    private static Color Blend(Color a, Color b, double ratio) => Color.FromArgb(
        255,
        (byte)(a.R * (1 - ratio) + b.R * ratio),
        (byte)(a.G * (1 - ratio) + b.G * ratio),
        (byte)(a.B * (1 - ratio) + b.B * ratio));

    private static SolidColorBrush Brush(Color c, double opacity = 1.0) =>
        new(Color.FromArgb((byte)(255 * opacity), c.R, c.G, c.B));
}
