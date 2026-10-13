# BigWave Radio

A Windows 11 native internet radio player, powered by the community-run
[radio-browser.info](https://www.radio-browser.info/) station directory.

Stream any station live — nothing is downloaded or stored. Tap the star on a
station to pin it to **My Library** for quick reference (saved locally in
`%LocalAppData%\BigWaveRadio\library.json`).

## Features

- **Discover** — trending, most popular and recently added stations
- **Search** — full-text search across the entire directory
- **Browse** — by tag or by country
- **My Library** — quick-reference list of pinned stations (local only)
- **Now playing bar** — play/pause/stop, volume, save-to-library, **sleep timer**
- **Windows 11 look & feel** — Mica backdrop, rounded corners, in-app title bar
- **Responsive layout** — the station grid reflows as the window resizes; the
  navigation pane collapses automatically on narrow windows
- **Community friendly** — load is spread across radio-browser mirrors and every
  play reports a click back to the directory

## Tech

| Layer | Choice |
|---|---|
| UI | WinUI 3 (Windows App SDK 1.8), unpackaged, self-contained |
| Framework | .NET 8, `net8.0-windows10.0.19041.0` |
| MVVM | CommunityToolkit.Mvvm |
| Directory | radio-browser.info public JSON API |
| Playback | `Windows.Media.Playback.MediaPlayer` |

```
src/
  BigWaveRadio.Core/   radio-browser API client, models, library service
  BigWaveRadio.App/    WinUI 3 app (pages, view models, controls)
```

## Build & run

```powershell
dotnet restore
dotnet build
dotnet run --project src/BigWaveRadio.App
```

Or open `BigWaveRadio.sln` in Visual Studio 2022 (Windows App SDK workload) and F5.

The app is unpackaged (no MSIX installer); the Windows App SDK runtime is
self-contained in the output folder, so the `bin` folder can be zipped and
copied as-is.

## Notes

- Station streams are served directly by each station; availability and quality
  depend on the broadcaster.
- radio-browser.info is a volunteer-run, user-contributed directory — be nice:
  the client rotates mirrors and sends a proper user agent.
