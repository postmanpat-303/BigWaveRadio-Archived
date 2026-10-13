<p align="center">
  <img src="src/BigWaveRadio.App/Assets/bigwave-logo-src.png" width="140" alt="BigWave Radio logo" />
</p>

<h1 align="center">BigWave Radio</h1>

<p align="center">
  A <strong>Windows 11 native</strong> internet radio player, powered by the community-run
  <a href="https://www.radio-browser.info/">radio-browser.info</a> station directory.
</p>

![BigWave Radio on Windows 11](docs/screenshot.png)

## What it does

- **Discover** — trending, most popular and recently added stations, one tap away
- **Search** — full-text search across the entire community directory
- **Browse** — by tag or by country, with live station counts
- **My Library** — star any station for quick reference; saved locally on your PC
  (`%LocalAppData%\BigWaveRadio\library.json`). Stations are always **streamed live**,
  never downloaded
- **Now playing bar** — play/pause/stop, volume, **sleep timer** (15/30/60 min),
  one-tap save-to-library
- **Community friendly** — load is spread across radio-browser mirrors and every
  play reports a click back to the directory

## Windows 11 native

- Mica backdrop, rounded corners and an in-app title bar with the app icon
- Station grid **reflows automatically** as you resize — from full screen down to
  a narrow column; the navigation pane collapses on small windows
- Scales cleanly across monitors and DPI settings

## Tech

| Layer | Choice |
|---|---|
| UI | WinUI 3 (Windows App SDK 1.8), unpackaged, self-contained |
| Framework | .NET 8, `net8.0-windows10.0.19041.0` |
| MVVM | CommunityToolkit.Mvvm |
| Directory | radio-browser.info public JSON API (mirror-rotating client) |
| Playback | `Windows.Media.Playback.MediaPlayer` |

```
src/
  BigWaveRadio.Core/   radio-browser API client, models, library service
  BigWaveRadio.App/    WinUI 3 app (pages, view models, controls, assets)
docs/
  screenshot.png       app on Windows 11
```

The app icon and title-bar logo are generated from the master logo in
`src/BigWaveRadio.App/Assets/bigwave-logo-src.png` — regenerate with
`python make_icon.py` after updating it.

## Build & run

```powershell
dotnet restore
dotnet build
dotnet run --project src/BigWaveRadio.App
```

Or open `BigWaveRadio.slnx` in Visual Studio 2022 (Windows App SDK workload) and press F5.

The app is unpackaged (no MSIX installer) and the Windows App SDK runtime is
self-contained, so the output folder can be zipped and copied to any Windows 10
1809+ / Windows 11 machine as-is.

## Notes

- Station streams are served directly by each broadcaster; availability and
  quality depend on them.
- radio-browser.info is a volunteer-run, user-contributed directory — the client
  rotates mirrors and sends a proper user agent to be a good citizen.

## Roadmap ideas

- Station logos / favicons in cards
- Recently played history
- Global media-key (play/pause) support
- Taskbar jump-list of favorite stations
- GitHub Actions CI build
