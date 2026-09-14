# Download history (SQLite) design

Date: 2026-09-14
Branch: `main`

## Goal

Persist a record of every download attempt (completed, failed, or canceled)
to a local SQLite database, capturing when it ran, how long it took, how
much data was transferred, at what average speed, and its final status.
Both front ends get a read-only screen to browse this history.

## Non-goals

- No automated test suite is being added (repo has none today; verification
  stays manual, per `CLAUDE.md`).
- No delete/clear/export UI for history entries in this pass — the screen
  is read-only. Can be added later if needed.
- No paging/filtering/search in the history screen — a single sorted list
  is enough for v1.
- No change to `Downloader`'s download behavior itself; this only adds a
  side-effect after each attempt finishes.

## Storage layer (`VideoDownloader.Core/History/`)

New NuGet dependency: `Microsoft.Data.Sqlite`, added to
`VideoDownloader.Core.csproj` only. Both front ends pick it up transitively
through their existing `Core` reference (same pattern as `YoutubeDLSharp`
today).

### `DownloadHistoryEntry`

Plain record type:

- `Url` (string)
- `Title` (string)
- `StartedAtUtc` (DateTime)
- `FinishedAtUtc` (DateTime)
- `DurationSeconds` (double)
- `FileSizeBytes` (long?) — null if it couldn't be parsed/was never reported
- `AverageSpeedBytesPerSec` (double?) — null if bytes or duration are unknown
- `Status` (string — `"Completed"` / `"Failed"` / `"Canceled"`, matching
  `DownloadingState` names)
- `Resolution` (string?)
- `Format` (string?)
- `ErrorMessage` (string?) — populated only for `Failed`

### `HistoryRepository`

- Resolves the DB file at
  `%LOCALAPPDATA%\{Registry.COMPANY}\{Registry.APPNAME}\history.db`.
  `Registry` lives in `Core` and resolves `COMPANY`/`APPNAME` via
  `Assembly.GetExecutingAssembly()` — i.e. `Core.dll`'s own assembly
  attributes — so this path is identical regardless of which front end
  hosts it. WinForms and WPF therefore share one history file, the same
  way they already share registry settings.
- Creates the containing directory and the `DownloadHistory` table
  (`CREATE TABLE IF NOT EXISTS`) lazily on first use.
- `Task AddAsync(DownloadHistoryEntry entry)` — single-row insert.
- `Task<IReadOnlyList<DownloadHistoryEntry>> GetAllAsync()` — all rows,
  ordered by `StartedAtUtc DESC`.
- Every public method wraps its SQLite calls in try/catch and swallows
  failures (logging via `System.Diagnostics.Debug.WriteLine`, same as
  `Registry.cs`), so a locked/corrupt/inaccessible DB file never breaks a
  download or crashes a history-screen open. `AddAsync` failing silently
  means that one entry is lost, not that the app misbehaves.

### Size parsing

A small static helper (`HistorySizeParser.ParseBytes(string)` in the
`History` namespace) parses yt-dlp's IEC-formatted size strings (e.g.
`"45.67MiB"`, `"812KiB"`, `"1.2GiB"`) into a `long?` byte count, returning
`null` for anything it doesn't recognize. Used to convert
`DownloadJob.FileSize`'s final value into `FileSizeBytes`.

## Wiring into the existing pipeline

- `DownloadJob` gains:
  - `StartedAtUtc` (`DateTime?`, private setter) — stamped inside
    `DownloadBegin()`.
  - `ErrorMessage` (`string?`) — stamped in `Downloader.Download()`'s catch
    block, alongside the existing `State = DownloadingState.Failed`.
- `DownloadQueueManager` owns a `HistoryRepository` instance (constructed
  internally — no constructor-signature change, so both front ends' existing
  `new DownloadQueueManager()` call sites are untouched).
- In `RunAsync`'s `finally` block, after `await pending.Downloader.Download(...)`
  returns (regardless of which terminal state the job ended in), it builds a
  `DownloadHistoryEntry` from `pending.Job` (title/url/resolution/format were
  set at enqueue time; state/file-size/error-message/started-at are now
  final) plus `DateTime.UtcNow` as `FinishedAtUtc`, and calls
  `await _history.AddAsync(entry)` before `TryStartQueuedDownloads()`.
  This runs for `Completed`, `Failed`, and `Canceled` alike.
- `AverageSpeedBytesPerSec` is computed as `FileSizeBytes / DurationSeconds`
  (when both are known and duration > 0), not read from the last observed
  instantaneous `job.Speed` sample — this better represents the speed of the
  download as a whole and avoids relying on a transient last-tick value that
  may be stale or missing right at completion/cancellation.

## History screens

Both are read-only, opened on demand, showing the same columns: Date
(local time, formatted from `StartedAtUtc`), Title, URL, Duration
(`mm:ss`), Size (formatted MB from `FileSizeBytes`), Avg speed (formatted
MB/s from `AverageSpeedBytesPerSec`), Status. Rows come from
`HistoryRepository.GetAllAsync()`, newest first.

### WinForms

- New `FrmHistory` (+ `FrmHistory.Designer.cs`) with a `DataGridView`
  bound to the loaded rows.
- New "🕑 History" label-button on `FrmMain`'s toolbar next to the
  existing "ℹ️ About" one; a `lHistory_Click` handler opens `FrmHistory`
  via `using`, mirroring `lAbout_Click`/`FrmAbout` exactly.

### WPF

- New `HistoryWindow.xaml` / `.xaml.cs` + `HistoryViewModel` (loads rows
  into an `ObservableCollection` for binding).
- `IDialogService.ShowHistory()` / `DialogService.ShowHistory()` — creates
  the view model, sets the wait cursor, awaits the load, then
  `ShowDialog()`, mirroring the existing `ShowSourceChooser` pattern.
- New toolbar `Button` next to "About" in `MainWindow.xaml`, bound to a
  new `MainViewModel.HistoryCommand` → `_dialogService.ShowHistory()`.

### Localization

New user-facing strings ("History"/"Historia", column headers, the
history window's title) go through `Localization.T(...)` with Polish
entries added to the dictionary, per CLAUDE.md's localization convention.

## Error handling

- History persistence failures never propagate to the download flow or the
  UI — `HistoryRepository` swallows its own exceptions (see above), the
  same "fall back silently" convention `Registry.cs` already uses for
  settings persistence.
- If `GetAllAsync()` fails when opening the history screen, the screen
  shows an empty list rather than throwing — acceptable for v1 given this
  is a diagnostic/informational feature, not core functionality.

## Testing

No automated suite in this repo. Manual verification plan:

1. Build both `VideoDownloader` and `VideoDownloader.Wpf`.
2. Run a download to completion in one app; confirm a `Completed` row
   appears in that app's History screen with sensible duration/size/speed.
3. Start and cancel a download; confirm a `Canceled` row appears.
4. Trigger a failure (e.g. invalid URL after fetch); confirm a `Failed` row
   appears with `ErrorMessage` populated.
5. Run a download in the *other* app and confirm it shows up in both apps'
   History screens — proving the shared DB path.
6. Confirm `%LOCALAPPDATA%\APPIT\VideoDownloader\history.db` exists and is
   a valid SQLite file.

## Open risk

`Microsoft.Data.Sqlite` ships a native SQLite binary per-RID via its
`SQLitePCLRaw.bundle_e_sqlite3` dependency. Need to verify it copies
correctly into both apps' build output, the WPF publish output, and the
`VideoDownloader.Setup` MSI payload (which currently packages only the
WinForms app) — flagged here to check during implementation rather than
assumed to "just work".
