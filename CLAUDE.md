# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

A Windows desktop app (.NET 8, WinForms) for downloading videos via [yt-dlp](https://github.com/yt-dlp/yt-dlp), with a custom-built FFmpeg bundled for post-processing/remuxing.

## Solution structure

- **VideoDownloader** (`net8.0-windows`) — the WinForms application. This is where almost all work happens.
- **FFmpegBuild** — native Makefile-style vcxproj that cross-compiles a stripped-down `ffmpeg.exe` from source via MSYS2/mingw and drops it into `ExternalLib\ffmpeg.exe`. Not part of the default solution build (avoids requiring MSYS2 on every dev machine) — build it manually only when FFmpeg needs to change. See `FFmpegBuild/README.md` (Polish) for details.
- **VideoDownloader.Setup** — WiX installer project producing the MSI package.
- **ExternalLib** — third-party executables (`yt-dlp.exe`, `ffmpeg.exe`) copied into the app's build output by a `PostBuild` xcopy target in `VideoDownloader.csproj`.

There is no automated test suite in this repo.

## Common commands

```
# Build the app
dotnet build VideoDownloader\VideoDownloader.csproj -c Release

# Run for development
dotnet run --project VideoDownloader
```

Building the full solution in Visual Studio also builds `VideoDownloader.Setup` (requires the WiX Toolset extension); `FFmpegBuild` is excluded and must be built manually if `ffmpeg.exe` needs regenerating.

## Architecture

### Download flow

`FrmMain` (main window) → `Downloader` (per-download-job wrapper around `YoutubeDLSharp.YoutubeDL`) → `yt-dlp.exe`/`ffmpeg.exe` subprocesses.

1. User pastes/enters a URL in `FrmMain`. A new `Downloader` is created per job, pointed at the bundled `ExternalLib\yt-dlp.exe`/`ffmpeg.exe` (resolved via `AppContext.BaseDirectory`).
2. `Downloader.FetchSources()` calls `ytdl.RunVideoDataFetch` and converts the raw `YoutubeDLSharp.Metadata.FormatData` list into `Core.DataSource` (`DataVideoSource`/`DataAudioSource`), filtering out non-video/non-audio formats and non-primary audio tracks (description/hard-of-hearing/audio-description tracks). If the URL resolves to a playlist entry instead of formats, it re-resolves against the specific entry ID embedded after a comma in the URL.
3. `FrmSourceChooser` shows fetched sources for manual pick (`Download` button flow) or auto-picks the best video + first audio track (`ChooseBestQuality()`, used by the drag-and-drop/batch/multi-URL flow).
4. `FrmMain.EnqueueDownload` pushes the job onto an in-memory `Queue<PendingDownload>`; `TryStartQueuedDownloads()` drains it up to `tbMaxConcurrent.Value` concurrent jobs, re-checking every time a job finishes or the concurrency slider changes. This queue is the only concurrency control — there's no cap inside `Downloader` itself.
5. `Downloader.Download()` runs `ytdl.RunVideoDownload` with the picked video+audio format IDs joined as `"{video}+{audio}"`, always merging/recoding to MP4, and reports progress back onto the job's `DownloadJobListBoxItem` (a custom control per active download in `flpJobs`).

### Cross-cutting pieces

- **Localization** (`Core/Localization.cs`) — no resx-based i18n; `Localization.T(english)` looks up a Polish string in a static dictionary and falls back to the English key itself if missing or if the app isn't in Polish. Language is `Auto` (follows OS UI culture), `English`, or `Polish`, persisted via `Core/Windows/Registry.cs`. When adding user-facing strings, wrap them in `Localization.T(...)` and add a Polish entry to the dictionary — untranslated strings degrade gracefully but silently.
- **Persistence** (`Core/Windows/Registry.cs`) — all app settings (last destination path, max concurrent downloads, language) are stored under `HKCU\SOFTWARE\{Company}\{AppTitle}` via raw `Microsoft.Win32.Registry` calls, not `Settings.settings`/appsettings. All reads/writes swallow exceptions and fall back to defaults.
- **Notifications** (`Core/Notifications.cs` + `NotificationControl`) — transient Info/Warning/Error/Success banners added as controls to `flpNotifications` on `FrmMain`; not a toast/dialog system.
- **Errors** (`Core/Errors.cs`) — `Errors.ParseErrorMessage` special-cases HTTP 403 / "paid" substrings in yt-dlp exceptions into a friendlier localized message; extend this rather than adding new ad-hoc error string matching elsewhere.
- **yt-dlp self-update** (`Core/Tools/YtDlpUpdater.cs`) — runs `yt-dlp.exe -U` with a 60s timeout on every `FrmMain` load, reporting results via `Notifications`.

### Binary resolution

`Downloader` and `YtDlpUpdater` both resolve `yt-dlp.exe`/`ffmpeg.exe` relative to `AppContext.BaseDirectory` (i.e. the app's own output/install directory), not `PATH`. Any code that shells out to these tools should follow the same pattern rather than assuming they're globally installed.
