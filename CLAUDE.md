# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

A Windows desktop app (.NET 8, WPF) for downloading videos via [yt-dlp](https://github.com/yt-dlp/yt-dlp), with a custom-built FFmpeg bundled for post-processing/remuxing.

## Solution structure

- **VideoDownloader.Wpf** (`net8.0-windows`) — the WPF application (MVVM via CommunityToolkit.Mvvm). This is where almost all work happens.
- **VideoDownloader.Core** (`net8.0-windows`) — shared, UI-framework-agnostic class library: download/queue/notification logic (`Downloader`, `DownloadJob`, `DownloadQueueManager`, `Notifications`, `QualityMatcher`), localization, registry persistence, error parsing, and a SQLite-backed download history (`History/`, via the `Microsoft.Data.Sqlite` package). Referenced by `VideoDownloader.Wpf`.
- **FFmpegBuild** — native Makefile-style vcxproj that cross-compiles a stripped-down `ffmpeg.exe` from source via MSYS2/mingw and drops it into `ExternalLib\ffmpeg.exe`. Not part of the default solution build (avoids requiring MSYS2 on every dev machine) — build it manually only when FFmpeg needs to change. See `FFmpegBuild/README.md` (Polish) for details.
- **Installer** — WiX v5 SDK-style project (`VideoDownloader.Installer.wixproj`) producing the MSI package (`Installer/bin/x64/Release/VideoDownloader-{version}-Setup.msi`). Packages the already-built `VideoDownloader.Wpf` Release output, not the source — build that project in Release first. `StageAppFiles` (a `BeforeTargets="CoreCompile"` target in the wixproj) copies a Windows-only subset of that output (no `.pdb`, no non-Windows native runtimes, no leftover release zip) into `obj\app\`, which `Product.wxs`'s `<Files>` element then harvests into MSI components. The wixproj pins `<Platform>x64</Platform>` (without it, a plain `dotnet build -c Release` silently produces a 32-bit MSI that installs to `Program Files (x86)` and registers in ARP as X86 even though the app itself is x64 - this bit once already). Installs per-machine under `Program Files\APPIT\VideoDownloader` (requires elevation, via `StandardDirectory Id="ProgramFiles64Folder"`) with a Start Menu shortcut. Replaces the old `Setup.vdproj` (legacy VS Installer Projects extension, not WiX despite older docs/plans referring to it as `VideoDownloader.Setup`).
- **ExternalLib** — third-party executables (`yt-dlp.exe`, `ffmpeg.exe`) copied into the app's build output by a `PostBuild` xcopy target in `VideoDownloader.Wpf.csproj`.

There is no automated test suite in this repo.

## Common commands

```
# Build the app
dotnet build VideoDownloader.Wpf\VideoDownloader.Wpf.csproj -c Release

# Run for development
dotnet run --project VideoDownloader.Wpf

# Build the MSI installer (after the app build above)
dotnet build Installer\VideoDownloader.Installer.wixproj -c Release
```

`FFmpegBuild` is excluded from the default solution build and must be built manually if `ffmpeg.exe` needs regenerating. `Installer` depends on `VideoDownloader.Wpf`'s Release output already existing on disk (it's a file-path dependency, not a `ProjectReference`, so build ordering isn't automatic within Visual Studio/`dotnet build VideoDownloader.sln`) — build `VideoDownloader.Wpf` in Release first, then `Installer`, as two separate steps.

When bumping the app version, update it in both `VideoDownloader.Wpf\VideoDownloader.Wpf.csproj` (`<Version>`) and `Installer\VideoDownloader.Installer.wixproj` (`<ProductVersion>`) - they aren't derived from a single source.

## Release process

When the user asks to "wypuść release" (cut a release) for a version bump, do all of the following, in order:

1. Update `README.md`, `CLAUDE.md`, and `index.html` to reflect whatever changed since the last release (new features, fixed docs, etc.) - don't just bump the version number silently.
2. Bump the version (see the note above - both `VideoDownloader.Wpf.csproj` and `VideoDownloader.Installer.wixproj`).
3. Build `VideoDownloader.Wpf` in Release, then the zip (Windows-only runtimes, no `.pdb`, no stale zip already sitting in the output dir - see prior release commits for the exact PowerShell staging step), then the MSI installer.
4. Commit with a **thematic split** - separate commits per logical change (e.g. one for the version bump/doc updates, others for any unrelated feature/fix work that rode along), not one giant commit. Ask before bundling in unrelated pre-existing uncommitted changes that aren't part of this release.
5. `git push`.
6. Tag (`git tag -a vX.Y.Z`), push the tag, and `gh release create` with both the `.zip` and `.msi` as assets and release notes summarizing what changed since the previous tag.

## Architecture

### Download flow

The pipeline: a "source chooser" step → `Downloader` (per-download-job wrapper around `YoutubeDLSharp.YoutubeDL`) → `VideoDownloader.Core.DownloadQueueManager` → `yt-dlp.exe`/`ffmpeg.exe` subprocesses.

1. User pastes/enters a URL (`MainViewModel.SourceUrl`). A new `Downloader` is created per job, pointed at the bundled `ExternalLib\yt-dlp.exe`/`ffmpeg.exe` (resolved via `AppContext.BaseDirectory`).
2. `Downloader.FetchSources()` calls `ytdl.RunVideoDataFetch` and converts the raw `YoutubeDLSharp.Metadata.FormatData` list into `Core.DataSource` (`DataVideoSource`/`DataAudioSource`), filtering out non-video/non-audio formats and non-primary audio tracks (description/hard-of-hearing/audio-description tracks). If the URL resolves to a playlist entry instead of formats, it re-resolves against the specific entry ID embedded after a comma in the URL.
3. Sources are picked either interactively or automatically, sharing the same `Core.QualityMatcher.MatchVideo`/`MatchAudio` fallback logic (match resolution+extension or language+extension, then resolution/language alone, then `LastOrDefault()`/`FirstOrDefault()`). `MainViewModel.DownloadOne` goes through `IDialogService` — `ShowSourceChooser()` (interactive, backed by `SourceChooserWindow`/`SourceChooserViewModel`) or `ChooseBestQuality()` (batch/multi-URL flow via `MainViewModel.LoadMultipleAsync`).
4. `MainViewModel.DownloadOne` hands the chosen `Downloader` + sources + a new `Core.DownloadJob` to a shared `Core.DownloadQueueManager` instance (one per app) via `Enqueue()`. `TryStartQueuedDownloads()` drains its internal queue up to `MaxConcurrentDownloads` concurrent jobs, re-checking every time a job finishes or the limit changes. This queue is the only concurrency control — there's no cap inside `Downloader` itself.
5. `Downloader.Download()` runs `ytdl.RunVideoDownload` with the picked video+audio format IDs joined as `"{video}+{audio}"`, always merging/recoding to MP4, and reports progress by writing into the job's `Core.DownloadJob` properties. The job-card `DataTemplate` (`Templates/JobCardTemplate.xaml`, bound via `MainViewModel.Jobs`) observes `DownloadJob.PropertyChanged` to render it, deliberately keeping the card visible but dimmed on cancel.
6. Whichever way the job ends (`Completed`/`Failed`/`Canceled`), `DownloadQueueManager.RunAsync`'s `finally` block calls `SaveHistoryAsync`, which persists a `Core.History.DownloadHistoryEntry` via `HistoryRepository` — see **Download history** below.

### Cross-cutting pieces

- **Localization** (`VideoDownloader.Core/Localization.cs`) — no resx-based i18n; `Localization.T(english)` looks up a Polish string in a static dictionary and falls back to the English key itself if missing or if the app isn't in Polish. Language is `Auto` (follows OS UI culture), `English`, or `Polish`, persisted via `VideoDownloader.Core/Windows/Registry.cs`. WPF binds most localized text via the `{loc:Tr 'key'}` markup extension (`VideoDownloader.Wpf/Localization/TrExtension.cs`), which self-refreshes on `Localization.LanguageChanged` using a weakly-referenced target so unused windows don't leak. When adding user-facing strings, wrap them in `Localization.T(...)` and add a Polish entry to the dictionary — untranslated strings degrade gracefully but silently.
- **Persistence** (`VideoDownloader.Core/Windows/Registry.cs`) — all app settings (last destination path, max concurrent downloads, language) are stored under `HKCU\SOFTWARE\{Company}\{AppTitle}` via raw `Microsoft.Win32.Registry` calls, not `Settings.settings`/appsettings. All reads/writes swallow exceptions and fall back to defaults.
- **Notifications** (`VideoDownloader.Core/Notifications.cs`) — transient Info/Warning/Error/Success banners. WPF binds `MainViewModel.Notifications` directly to render them. Not a toast/dialog system.
- **Confirmation dialogs** — `IDialogService.Confirm(message, title)` shows a themed Yes/No `Views/ConfirmDialogWindow`, not `System.Windows.MessageBox.Show` (which ignores the app's theme/localization chrome). Use it for any new Yes/No prompt.
- **Errors** (`VideoDownloader.Core/Errors.cs`) — `Errors.ParseErrorMessage` special-cases HTTP 403 / "paid" substrings in yt-dlp exceptions into a friendlier localized message; extend this rather than adding new ad-hoc error string matching elsewhere.
- **yt-dlp self-update** (`VideoDownloader.Core/Tools/YtDlpUpdater.cs`) — runs `yt-dlp.exe -U` with a 60s timeout on every app load (`MainWindow`/`MainViewModel.CheckForYtDlpUpdateAsync`), reporting results via `Notifications`.
- **Download history** (`VideoDownloader.Core/History/`) — `HistoryRepository` persists `DownloadHistoryEntry` rows (URL, title, timestamps, duration, size, average speed, status, error message) to a per-user SQLite database at `%LOCALAPPDATA%\{Company}\{AppTitle}\history.db` (path from `HistoryRepository.GetDefaultDatabasePath()`); every public method swallows its own exceptions, same convention as `Registry`, so a locked/corrupt DB never breaks a download. `HistorySizeParser.ParseBytes` converts yt-dlp's IEC size strings (e.g. `"45.67MiB"`) for storage. `VideoDownloader.Wpf/Views/HistoryWindow.xaml` + `ViewModels/HistoryViewModel.cs` render it read-mostly: a live search filter (`ICollectionView`), per-row redownload (routes back through `MainViewModel.RedownloadFromHistoryAsync` via a callback passed into `IDialogService.ShowHistory`) and open-in-browser actions, and a clear-all action.
- **Theming** (`VideoDownloader.Wpf/Theming/ThemeManager.cs`) — `AppTheme` is `System`/`Light`/`Dark`, persisted like `Language` via `Registry.GetTheme()`/`SetTheme()`. `ThemeManager.Apply()` swaps `Themes/ColorsLight.xaml`/`ColorsDark.xaml` into `Application.Resources.MergedDictionaries` at runtime; while `System` is selected it also follows OS theme changes via `SystemEvents.UserPreferenceChanged`. Every themed brush in `Themes/Styles.xaml` and every Window's `Background`/`Foreground` **must** use `{DynamicResource ...}`, not `{StaticResource ...}` — `StaticResource` resolves once at XAML load time and won't repaint when the palette swaps. This bit twice already: a `ComboBox`'s inner `ToggleButton` not forwarding `Background` via `TemplateBinding` (so it fell back to the OS theme's control color), and `FlatButtonStyle`/`FlatComboBoxItemStyle` not setting an explicit `Foreground` (so Popup/button text didn't reliably inherit the palette's text color). When adding a new themed visual, set `Foreground`/`Background` explicitly via `DynamicResource` rather than relying on property inheritance.

### Binary resolution

`Downloader` and `YtDlpUpdater` both resolve `yt-dlp.exe`/`ffmpeg.exe` relative to `AppContext.BaseDirectory` (i.e. the app's own output/install directory), not `PATH`. Any code that shells out to these tools should follow the same pattern rather than assuming they're globally installed.
