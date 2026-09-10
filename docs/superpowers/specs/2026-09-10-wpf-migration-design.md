# WinForms → WPF migration design

Date: 2026-09-10
Branch: `wpf-migration`

## Goal

Add a WPF front end for VideoDownloader alongside the existing WinForms app,
sharing the download/queue/notification logic through a new class library so
neither app duplicates it. The WinForms app keeps running unchanged for end
users; both apps stay in the solution after this work (no decision yet on
retiring WinForms — revisit later).

## Non-goals

- No automated test suite is being added (repo has none today; verification
  stays manual, per `CLAUDE.md`).
- No installer/MSI for the WPF build in this pass — `VideoDownloader.Setup`
  keeps packaging the WinForms exe only.
- No dark mode. The WPF UI gets one refreshed light theme; dark mode is a
  possible fast-follow, not part of this design.
- No behavior changes to the WinForms app's user-facing behavior — the
  WinForms-side changes described below are a rewire of internals to consume
  the new shared Core library, not a redesign.

## Solution structure

Three projects after this change:

- **`VideoDownloader.Core`** (new) — `net8.0` class library, no UI framework
  reference. Holds everything currently UI-agnostic (`DataSource.cs`,
  `Localization.cs`, `Windows/Registry.cs`, `Errors.cs`, `Tools/*`) plus the
  refactored `Downloader`, `DownloadJob`, `Notifications`/`NotificationItem`,
  and `DownloadQueueManager` described below. Takes the `YoutubeDLSharp`
  package reference (moved from the WinForms project, since `Downloader`
  moves here).
- **`VideoDownloader`** (existing WinForms app) — references `Core`. Keeps
  its forms/controls (`FrmMain`, `FrmSourceChooser`, `FrmAbout`,
  `DownloadJobListBoxItem`, `ProgressBarExtended`, `NotificationControl`)
  and its exact current look/behavior, but the glue code between those
  controls and the download logic is rewired to consume `Core`'s shared
  model classes instead of being written into directly.
- **`VideoDownloader.Wpf`** (new) — `net8.0-windows`, `UseWPF=true`,
  references `Core` and the `CommunityToolkit.Mvvm` package. Pure
  view/ViewModel layer; contains no download/queue/notification logic of
  its own.

`VideoDownloader.sln` gains entries for the two new projects.
`VideoDownloader.Setup` is untouched.

## Shared Core: the model layer

Three classes move into `Core` in a form that has no WinForms dependency:

### `DownloadJob`

Replaces `DownloadJobListBoxItem` as the type `Downloader.Download()` writes
progress into. Implements `INotifyPropertyChanged`. Same shape as today's
control API: `Title`, `Url`, `Resolution`, `Format`, `Duration`, `State`
(the `DownloadingState` enum moves here too: `Waiting`/`Downloading`/
`Completed`/`Failed`/`Canceled`), `ProgressPercentage`, `Speed`, `ETA`,
`FileSize`, `CancellationTokenSource`, and the `DownloadBegin()`/
`DownloadEnd()` helper methods.

`Downloader.Download()`'s signature changes from
`(sources, DownloadJobListBoxItem job)` to `(sources, DownloadJob job)`.
Its internal logic (building `OptionSet`, calling `ytdl.RunVideoDownload`,
progress callback, exception handling) is otherwise unchanged.

### `Notifications` / `NotificationItem`

`Notifications` no longer wraps a WinForms `Panel`. It holds an
`ObservableCollection<NotificationItem>` (`NotificationItem` = `Type`,
`Message`, `Timestamp`). Public API (`Info`/`Warning`/`Error`/`Success`/
`AddMessage`/`ClearOldNotifications`) is unchanged in shape. The 10-second
auto-dismiss timer moves from `NotificationControl` into this service — it
removes the item from the collection itself, so consumers never implement
dismiss logic.

### `DownloadQueueManager`

The concurrency/queueing logic currently living in `FrmMain`
(`_downloadQueue`, `TryStartQueuedDownloads`, `EnqueueDownload`, active/
completed counts) becomes a reusable service:

- `ObservableCollection<DownloadJob> Jobs`
- `int MaxConcurrentDownloads` (settable; triggers `TryStartQueuedDownloads`
  the same way the slider's `ValueChanged` does today)
- `EnqueueDownload(Downloader downloader, (DataVideoSource, DataAudioSource)
  sources, DownloadJob job)`
- `TryStartQueuedDownloads()` (internal queue draining, same semantics as
  today)
- `ActiveCount` / `CompletedCount` / `TotalCount` computed properties

Both apps get identical, single-sourced concurrency handling instead of
reimplementing it.

## WinForms adapter changes

This is the one place existing, working code gets touched. Scope is a
rewire, not a rewrite:

- `FrmMain` creates one `DownloadQueueManager` instead of owning
  `_downloadQueue`/`TryStartQueuedDownloads`/`EnqueueDownload` itself.
  `tbMaxConcurrent`'s `ValueChanged` sets `queueManager.MaxConcurrentDownloads`
  instead of calling into local methods.
- `flpJobs` syncs to `queueManager.Jobs.CollectionChanged`: on add, create a
  `DownloadJobListBoxItem` that wraps the new `DownloadJob` and mirrors its
  `PropertyChanged` into the control's labels/progress bar (instead of the
  control's properties being *written into* directly by `Downloader`).
- `flpNotifications` syncs to the `Notifications` service's collection the
  same way: add/remove a `NotificationControl` per `NotificationItem`.
  `NotificationControl` becomes a dumb renderer constructed from a
  `NotificationItem` (no more owning an auto-dismiss timer itself).
- `Downloader` construction in `FrmMain`/`BatchDownload` is otherwise
  unchanged (still `new Downloader(_Notifications)`), since `Notifications`'
  public API shape doesn't change.

No visible/behavioral change is intended for WinForms end users. If manual
verification turns up any behavior drift, that's a bug to fix, not an
accepted trade-off.

## WPF app architecture

- **ViewModels** (CommunityToolkit.Mvvm `ObservableObject` /
  `[RelayCommand]`):
  - `MainViewModel` — `SourceUrl`, `DestinationPath`, wraps one
    `DownloadQueueManager` (exposes `Jobs` and the `Notifications` service's
    collection straight to XAML), commands: `Download`, `Browse`,
    `OpenFolder`, `LoadFile`, `About`, `BuyCoffee`.
  - `SourceChooserViewModel` — mirrors `FrmSourceChooser`'s fetch-then-pick
    flow (`FetchSources`, manual pick, `ChooseBestQuality` for batches).
  - `AboutViewModel` — trivial, mirrors `FrmAbout`.
  - ViewModels never reference `Window` types directly. A small
    `IDialogService` (shows `SourceChooserWindow` / `AboutWindow`, returns
    the picked result / dialog outcome) keeps ViewModels testable and
    decoupled from views.
- **Views**: `MainWindow.xaml`, `SourceChooserWindow.xaml`,
  `AboutWindow.xaml`, plus:
  - a `JobCard` DataTemplate bound to `DownloadJob` — a `Grid` with a
    `ProgressBar` plus an overlay `TextBlock` for percent/speed/ETA text
    (simpler than `ProgressBarExtended`'s custom GDI+ painting; state→color
    via `DataTrigger` on `DownloadJob.State`)
  - a `NotificationBanner` DataTemplate bound to `NotificationItem`
    (color via `DataTrigger` on `NotificationType`)
- **Localization in XAML**: a `{loc:Tr 'English key'}` `MarkupExtension`
  (WPF-project-local) that calls `Core.Localization.T` and re-evaluates on
  `Localization.LanguageChanged`, so the Polish dictionary in `Core` stays
  the single source of truth and no per-string ViewModel properties are
  needed.
- **Styling**: one refreshed light-theme `ResourceDictionary` (accent
  palette, rounded buttons/cards, cleaner spacing) applied app-wide via
  `App.xaml`. No dark mode (see Non-goals).
- **Drag-and-drop / clipboard paste**: `AllowDrop` + `Drop` on the job list
  area, and `Window.Activated`, mirror `flpJobs_DragDrop` /
  `FrmMain_Activated`'s logic 1:1.

## Feature parity checklist

Every WinForms screen gets a WPF counterpart with the same functionality:

- Main window: URL input, destination path + Browse + Open-folder, batch
  load from `.txt`, drag-drop multi-URL paste, max-concurrent slider, job
  list, notification banners, status line (`Jobs: N | Active: N |
  Completed: N`), language selector, About / Buy-me-a-coffee links.
- Source-chooser dialog: manual video/audio pick, `ChooseBestQuality`
  auto-pick for batch downloads (matches preferred resolution/language from
  the first URL in a batch).
- About dialog: version, build date, author, yt-dlp/FFmpeg links.
- yt-dlp self-update check on load, reported via notifications.
- Registry-persisted settings (`Core.Windows.Registry`, unchanged): last
  destination path, max concurrent downloads, language.

## Error handling

- `Errors.ParseErrorMessage` stays the single source of truth for turning
  yt-dlp exceptions into friendly messages — reused as-is from `Core`.
- Confirmation dialogs (batch-download "are you sure?", cancel-download)
  map directly to WPF's own `System.Windows.MessageBox` — same UX as
  today's WinForms `MessageBox.Show`.
- Deliberate deviation: `FrmSourceChooser`'s blocking error `MessageBox` on
  fetch failure becomes a `Notifications` banner in the WPF app, so all
  errors surface through one consistent channel there instead of mixing
  modal dialogs and banners.

## Packaging detail

The `PostBuild` xcopy target in `VideoDownloader.csproj` that drops
`yt-dlp.exe`/`ffmpeg.exe` into the output directory must be replicated in
`VideoDownloader.Wpf.csproj` (or factored into a shared
`Directory.Build.targets`), or the WPF app won't find its binaries at
`AppContext.BaseDirectory`.

## Verification plan

No automated test suite exists in this repo, so verification is manual:

1. Both projects build cleanly (`dotnet build` on the solution).
2. `VideoDownloader.Wpf` launches without exceptions.
3. Golden path: paste a URL → fetch sources → pick quality → download
   completes and the file lands in the destination folder.
4. Edge cases: invalid URL, cancel mid-download, batch download via
   multi-line paste / drag-drop / `.txt` load, PL/EN language switch,
   changing the concurrency slider mid-queue.
5. Regression check on the WinForms app after its adapter rewire: the same
   golden path and edge cases, to confirm no behavior drift from routing
   through the new `Core` classes.

Claude will drive as much of this as is feasible from the CLI/build tooling
and report explicitly what was and wasn't exercised — full interactive
click-through is ultimately the user's call to confirm.
