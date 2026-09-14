# Download History (SQLite) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist every download attempt (completed, failed, or canceled) to a local SQLite database — capturing when it ran, how long it took, bytes transferred, average speed, and final status — and let both front ends browse that history in a read-only screen.

**Architecture:** A new `VideoDownloader.Core/History` namespace owns the SQLite persistence (`HistoryRepository`) and the row model (`DownloadHistoryEntry`). `DownloadQueueManager` (already the single place both front ends funnel every download through) writes one row per finished job. Each front end gets its own thin read-only screen (`FrmHistory` for WinForms, `HistoryWindow`/`HistoryViewModel` for WPF) that calls the same repository directly.

**Tech Stack:** .NET 8, `Microsoft.Data.Sqlite` 8.0.11, WinForms `DataGridView`, WPF `ListView`/`GridView` + CommunityToolkit.Mvvm.

**Spec:** `docs/superpowers/specs/2026-09-14-download-history-design.md`

## Global Constraints

- NuGet dependency: `Microsoft.Data.Sqlite` version `8.0.11`, added to `VideoDownloader.Core.csproj` only.
- DB file path: `%LOCALAPPDATA%\{Registry.COMPANY}\{Registry.APPNAME}\history.db` (identical for both front ends — see spec's "Storage layer" section for why).
- Every download attempt is recorded — `Completed`, `Failed`, and `Canceled` alike.
- History screens in both front ends are **read-only** for this pass — no delete/clear/export.
- No automated test suite exists in this repo (per `CLAUDE.md`); verification is manual `dotnet build` + running the app, per the spec's "Testing" section.
- Every new user-facing string goes through `Localization.T(...)` with a Polish entry added to `VideoDownloader.Core/Localization.cs`'s dictionary.
- `HistoryRepository`'s public methods must swallow their own exceptions (mirrors `Registry.cs`'s convention) — persistence failures must never break a download or crash a history screen.
- The `Setup\Setup.vdproj` installer project is out of scope for this plan (it isn't buildable via `dotnet`/CLI tooling at all — it requires the Visual Studio Installer Projects extension) — no task here touches it.

---

### Task 1: History data model, size parser, and public `DataSize` formatter

**Files:**
- Create: `VideoDownloader.Core/History/DownloadHistoryEntry.cs`
- Create: `VideoDownloader.Core/History/HistorySizeParser.cs`
- Modify: `VideoDownloader.Core/Tools/DataSize.cs`
- Modify: `VideoDownloader.Core/VideoDownloader.Core.csproj`

**Interfaces:**
- Produces: `DownloadHistoryEntry` (public class, namespace `VideoDownloader.Core.History`) with settable properties `Url` (string), `Title` (string), `StartedAtUtc` (DateTime), `FinishedAtUtc` (DateTime), `DurationSeconds` (double), `FileSizeBytes` (long?), `AverageSpeedBytesPerSec` (double?), `Status` (string), `Resolution` (string?), `Format` (string?), `ErrorMessage` (string?).
- Produces: `HistorySizeParser.ParseBytes(string? text) → long?` (static, namespace `VideoDownloader.Core.History`).
- Produces: `VideoDownloader.Core.Tools.DataSize.FromBytes(double bytes) → string` — now `public` (was `internal`), unchanged behavior.

- [ ] **Step 1: Add the `Microsoft.Data.Sqlite` package reference**

Edit `VideoDownloader.Core/VideoDownloader.Core.csproj`. Current `ItemGroup`:

```xml
  <ItemGroup>
    <PackageReference Include="YoutubeDLSharp" Version="1.2.0" />
  </ItemGroup>
```

Change to:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.11" />
    <PackageReference Include="YoutubeDLSharp" Version="1.2.0" />
  </ItemGroup>
```

- [ ] **Step 2: Create `DownloadHistoryEntry`**

Create `VideoDownloader.Core/History/DownloadHistoryEntry.cs`:

```csharp
namespace VideoDownloader.Core.History
{
    /// <summary>Represents one persisted download attempt, successful or not.</summary>
    public class DownloadHistoryEntry
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime FinishedAtUtc { get; set; }
        public double DurationSeconds { get; set; }
        public long? FileSizeBytes { get; set; }
        public double? AverageSpeedBytesPerSec { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Resolution { get; set; }
        public string? Format { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
```

- [ ] **Step 3: Create `HistorySizeParser`**

Create `VideoDownloader.Core/History/HistorySizeParser.cs`:

```csharp
using System.Globalization;
using System.Text.RegularExpressions;

namespace VideoDownloader.Core.History
{
    /// <summary>Parses yt-dlp's IEC-formatted size strings (e.g. "45.67MiB") into a byte count.</summary>
    public static class HistorySizeParser
    {
        private static readonly Regex SizePattern = new(@"^\s*([\d.,]+)\s*([KMGT]?i?B)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static long? ParseBytes(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var match = SizePattern.Match(text);
            if (!match.Success)
                return null;

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return null;

            double multiplier = match.Groups[2].Value.ToUpperInvariant() switch
            {
                "B" => 1,
                "KB" or "KIB" => 1024,
                "MB" or "MIB" => 1024 * 1024,
                "GB" or "GIB" => 1024d * 1024 * 1024,
                "TB" or "TIB" => 1024d * 1024 * 1024 * 1024,
                _ => 0
            };

            if (multiplier == 0)
                return null;

            return (long)(value * multiplier);
        }
    }
}
```

- [ ] **Step 4: Make `DataSize` public**

Read `VideoDownloader.Core/Tools/DataSize.cs`. Current content:

```csharp
namespace VideoDownloader.Core.Tools
{
    internal class DataSize
    {
        public static string FromBytes(double bytes)
        {
            return string.Format("{0:F2} MB", bytes / 1024.0 / 1024.0);
        }
    }
}
```

Change `internal class DataSize` to `public static class DataSize` (it only has static members, and both front ends' history screens need to call it):

```csharp
namespace VideoDownloader.Core.Tools
{
    public static class DataSize
    {
        public static string FromBytes(double bytes)
        {
            return string.Format("{0:F2} MB", bytes / 1024.0 / 1024.0);
        }
    }
}
```

- [ ] **Step 5: Build to verify**

Run: `dotnet build VideoDownloader.Core\VideoDownloader.Core.csproj -c Release`
Expected: Build succeeds with 0 errors. This also confirms `Microsoft.Data.Sqlite` restored correctly (check the output for a restore step referencing the package).

- [ ] **Step 6: Commit**

```bash
git add VideoDownloader.Core/VideoDownloader.Core.csproj VideoDownloader.Core/History/DownloadHistoryEntry.cs VideoDownloader.Core/History/HistorySizeParser.cs VideoDownloader.Core/Tools/DataSize.cs
git commit -m "Add SQLite dependency and download-history data model"
```

---

### Task 2: `HistoryRepository`

**Files:**
- Create: `VideoDownloader.Core/History/HistoryRepository.cs`

**Interfaces:**
- Consumes: `DownloadHistoryEntry` (Task 1), `Registry.COMPANY` / `Registry.APPNAME` (`VideoDownloader.Core.Windows.Registry`, both `public static string` properties).
- Produces: `HistoryRepository` (public class) with a parameterless constructor (resolves the default DB path) and a `HistoryRepository(string databasePath)` constructor; `Task AddAsync(DownloadHistoryEntry entry)`; `Task<IReadOnlyList<DownloadHistoryEntry>> GetAllAsync()`; static `HistoryRepository.GetDefaultDatabasePath() → string`.

- [ ] **Step 1: Create `HistoryRepository`**

Create `VideoDownloader.Core/History/HistoryRepository.cs`:

```csharp
using System.Globalization;
using Microsoft.Data.Sqlite;
using VideoDownloader.Core.Windows;

namespace VideoDownloader.Core.History
{
    /// <summary>
    /// Reads/writes <see cref="DownloadHistoryEntry"/> rows in a per-user SQLite database shared
    /// by both front ends (see <see cref="GetDefaultDatabasePath"/>). Every public method
    /// swallows its own exceptions, mirroring <see cref="Registry"/>'s silent-fallback
    /// convention, so a locked/corrupt/inaccessible DB file never breaks a download or crashes
    /// the history screen.
    /// </summary>
    public class HistoryRepository
    {
        private readonly string _connectionString;

        public HistoryRepository() : this(GetDefaultDatabasePath())
        {
        }

        public HistoryRepository(string databasePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        }

        public static string GetDefaultDatabasePath()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Registry.COMPANY,
                Registry.APPNAME);
            return Path.Combine(folder, "history.db");
        }

        public async Task AddAsync(DownloadHistoryEntry entry)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                await EnsureTableAsync(connection);

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO DownloadHistory
                        (Url, Title, StartedAtUtc, FinishedAtUtc, DurationSeconds, FileSizeBytes, AverageSpeedBytesPerSec, Status, Resolution, Format, ErrorMessage)
                    VALUES
                        ($url, $title, $startedAtUtc, $finishedAtUtc, $durationSeconds, $fileSizeBytes, $averageSpeedBytesPerSec, $status, $resolution, $format, $errorMessage);";

                command.Parameters.AddWithValue("$url", entry.Url);
                command.Parameters.AddWithValue("$title", entry.Title);
                command.Parameters.AddWithValue("$startedAtUtc", entry.StartedAtUtc.ToString("o", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$finishedAtUtc", entry.FinishedAtUtc.ToString("o", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$durationSeconds", entry.DurationSeconds);
                command.Parameters.AddWithValue("$fileSizeBytes", (object?)entry.FileSizeBytes ?? DBNull.Value);
                command.Parameters.AddWithValue("$averageSpeedBytesPerSec", (object?)entry.AverageSpeedBytesPerSec ?? DBNull.Value);
                command.Parameters.AddWithValue("$status", entry.Status);
                command.Parameters.AddWithValue("$resolution", (object?)entry.Resolution ?? DBNull.Value);
                command.Parameters.AddWithValue("$format", (object?)entry.Format ?? DBNull.Value);
                command.Parameters.AddWithValue("$errorMessage", (object?)entry.ErrorMessage ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public async Task<IReadOnlyList<DownloadHistoryEntry>> GetAllAsync()
        {
            var results = new List<DownloadHistoryEntry>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                await EnsureTableAsync(connection);

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Url, Title, StartedAtUtc, FinishedAtUtc, DurationSeconds, FileSizeBytes, AverageSpeedBytesPerSec, Status, Resolution, Format, ErrorMessage
                    FROM DownloadHistory
                    ORDER BY StartedAtUtc DESC;";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new DownloadHistoryEntry
                    {
                        Url = reader.GetString(0),
                        Title = reader.GetString(1),
                        StartedAtUtc = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        FinishedAtUtc = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        DurationSeconds = reader.GetDouble(4),
                        FileSizeBytes = reader.IsDBNull(5) ? null : reader.GetInt64(5),
                        AverageSpeedBytesPerSec = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                        Status = reader.GetString(7),
                        Resolution = reader.IsDBNull(8) ? null : reader.GetString(8),
                        Format = reader.IsDBNull(9) ? null : reader.GetString(9),
                        ErrorMessage = reader.IsDBNull(10) ? null : reader.GetString(10),
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            return results;
        }

        private static async Task EnsureTableAsync(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS DownloadHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    StartedAtUtc TEXT NOT NULL,
                    FinishedAtUtc TEXT NOT NULL,
                    DurationSeconds REAL NOT NULL,
                    FileSizeBytes INTEGER NULL,
                    AverageSpeedBytesPerSec REAL NULL,
                    Status TEXT NOT NULL,
                    Resolution TEXT NULL,
                    Format TEXT NULL,
                    ErrorMessage TEXT NULL
                );";
            await command.ExecuteNonQueryAsync();
        }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build VideoDownloader.Core\VideoDownloader.Core.csproj -c Release`
Expected: Build succeeds with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add VideoDownloader.Core/History/HistoryRepository.cs
git commit -m "Add SQLite-backed HistoryRepository"
```

---

### Task 3: Wire `DownloadJob`/`Downloader` to carry start time and error message

**Files:**
- Modify: `VideoDownloader.Core/DownloadJob.cs`
- Modify: `VideoDownloader.Core/Downloader.cs`

**Interfaces:**
- Produces: `DownloadJob.StartedAtUtc` (`DateTime?`, private setter, stamped in `DownloadBegin()`); `DownloadJob.ErrorMessage` (`string?`, public setter, stamped in `Downloader.Download`'s catch block).

- [ ] **Step 1: Add `StartedAtUtc` and `ErrorMessage` to `DownloadJob`**

In `VideoDownloader.Core/DownloadJob.cs`, current relevant section:

```csharp
        public string FileSize { get => _fileSize; set => SetField(ref _fileSize, value); }

        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public void DownloadBegin()
        {
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new CancellationTokenSource();
            State = DownloadingState.Downloading;
        }
```

Change to:

```csharp
        public string FileSize { get => _fileSize; set => SetField(ref _fileSize, value); }

        public CancellationTokenSource? CancellationTokenSource { get; set; }

        /// <summary>UTC timestamp of the most recent <see cref="DownloadBegin"/> call. Used to
        /// compute the history entry's duration once the job finishes.</summary>
        public DateTime? StartedAtUtc { get; private set; }

        /// <summary>Set alongside <see cref="DownloadingState.Failed"/> in
        /// <see cref="Downloader.Download"/>; carries the parsed error message into the
        /// persisted history entry.</summary>
        public string? ErrorMessage { get; set; }

        public void DownloadBegin()
        {
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new CancellationTokenSource();
            StartedAtUtc = DateTime.UtcNow;
            State = DownloadingState.Downloading;
        }
```

- [ ] **Step 2: Stamp `ErrorMessage` on failure**

In `VideoDownloader.Core/Downloader.cs`, current catch block (inside `Download`):

```csharp
            catch (Exception ex)
            {
                job.State = DownloadingState.Failed;
                _Notifications.Error(string.Format(Localization.T("Download failed ({0}): {1}"), job.Title, Errors.ParseErrorMessage(ex)));
            }
```

Change to:

```csharp
            catch (Exception ex)
            {
                job.State = DownloadingState.Failed;
                job.ErrorMessage = Errors.ParseErrorMessage(ex);
                _Notifications.Error(string.Format(Localization.T("Download failed ({0}): {1}"), job.Title, job.ErrorMessage));
            }
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build VideoDownloader.Core\VideoDownloader.Core.csproj -c Release`
Expected: Build succeeds with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add VideoDownloader.Core/DownloadJob.cs VideoDownloader.Core/Downloader.cs
git commit -m "Track download start time and error message on DownloadJob"
```

---

### Task 4: Persist a history row for every finished download in `DownloadQueueManager`

**Files:**
- Modify: `VideoDownloader.Core/DownloadQueueManager.cs`

**Interfaces:**
- Consumes: `HistoryRepository` (Task 2), `HistorySizeParser.ParseBytes` (Task 1), `DownloadHistoryEntry` (Task 1), `DownloadJob.StartedAtUtc`/`ErrorMessage` (Task 3).
- Produces: no new public API — this is a pure side-effect wired into the existing `RunAsync` flow that Tasks 6/7 depend on transitively (they read back what this writes).

- [ ] **Step 1: Add the `using` and the `HistoryRepository` field**

In `VideoDownloader.Core/DownloadQueueManager.cs`, current top:

```csharp
using System.Collections.ObjectModel;

namespace VideoDownloader.Core
{
    /// <summary>
    /// Owns the pending-download queue and the simultaneous-download limit. Shared between
    /// the WinForms and WPF front ends so both apps behave identically.
    /// </summary>
    public class DownloadQueueManager
    {
        private sealed class PendingDownload
        {
            public required Downloader Downloader { get; init; }
            public required (DataVideoSource video, DataAudioSource audio) Sources { get; init; }
            public required DownloadJob Job { get; init; }
        }

        private readonly Queue<PendingDownload> _pending = new();
        private int _maxConcurrentDownloads = 1;
```

Change to:

```csharp
using System.Collections.ObjectModel;
using VideoDownloader.Core.History;

namespace VideoDownloader.Core
{
    /// <summary>
    /// Owns the pending-download queue and the simultaneous-download limit. Shared between
    /// the WinForms and WPF front ends so both apps behave identically.
    /// </summary>
    public class DownloadQueueManager
    {
        private sealed class PendingDownload
        {
            public required Downloader Downloader { get; init; }
            public required (DataVideoSource video, DataAudioSource audio) Sources { get; init; }
            public required DownloadJob Job { get; init; }
        }

        private readonly Queue<PendingDownload> _pending = new();
        private readonly HistoryRepository _history = new();
        private int _maxConcurrentDownloads = 1;
```

- [ ] **Step 2: Save a history entry after every job finishes**

Current `RunAsync`:

```csharp
        private async Task RunAsync(PendingDownload pending)
        {
            try
            {
                await pending.Downloader.Download(pending.Sources, pending.Job);
            }
            finally
            {
                pending.Downloader.Dispose();
                CountsChanged?.Invoke(this, EventArgs.Empty);
                TryStartQueuedDownloads();
            }
        }
    }
}
```

Change to:

```csharp
        private async Task RunAsync(PendingDownload pending)
        {
            try
            {
                await pending.Downloader.Download(pending.Sources, pending.Job);
            }
            finally
            {
                await SaveHistoryAsync(pending.Job);
                pending.Downloader.Dispose();
                CountsChanged?.Invoke(this, EventArgs.Empty);
                TryStartQueuedDownloads();
            }
        }

        /// <summary>
        /// Persists one history row per finished download attempt (Completed/Failed/Canceled
        /// alike). Average speed is computed from the final reported size over the wall-clock
        /// duration rather than trusting the last instantaneous <see cref="DownloadJob.Speed"/>
        /// sample, which can be stale or missing right at completion/cancellation.
        /// </summary>
        private async Task SaveHistoryAsync(DownloadJob job)
        {
            var finishedAtUtc = DateTime.UtcNow;
            var startedAtUtc = job.StartedAtUtc ?? finishedAtUtc;
            var durationSeconds = Math.Max(0, (finishedAtUtc - startedAtUtc).TotalSeconds);
            var fileSizeBytes = HistorySizeParser.ParseBytes(job.FileSize);

            await _history.AddAsync(new DownloadHistoryEntry
            {
                Url = job.Url,
                Title = job.Title,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = finishedAtUtc,
                DurationSeconds = durationSeconds,
                FileSizeBytes = fileSizeBytes,
                AverageSpeedBytesPerSec = fileSizeBytes.HasValue && durationSeconds > 0
                    ? fileSizeBytes.Value / durationSeconds
                    : null,
                Status = job.State.ToString(),
                Resolution = job.Resolution,
                Format = job.Format,
                ErrorMessage = job.ErrorMessage
            });
        }
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build VideoDownloader.Core\VideoDownloader.Core.csproj -c Release`
Expected: Build succeeds with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add VideoDownloader.Core/DownloadQueueManager.cs
git commit -m "Persist a history entry for every finished download"
```

---

### Task 5: WinForms history screen (`FrmHistory`) and toolbar entry point

**Files:**
- Create: `VideoDownloader/FrmHistory.cs`
- Create: `VideoDownloader/FrmHistory.Designer.cs`
- Modify: `VideoDownloader/FrmMain.Designer.cs`
- Modify: `VideoDownloader/FrmMain.cs`
- Modify: `VideoDownloader.Core/Localization.cs`

**Interfaces:**
- Consumes: `HistoryRepository` (Task 2), `DownloadHistoryEntry` (Task 1), `DataSize.FromBytes` (Task 1), `Time.FromSeconds` (existing, `VideoDownloader.Core.Tools.Time`), `Localization.T` (existing).
- Produces: `FrmHistory` (WinForms `Form`), opened via a new "🕑 History" status-strip label on `FrmMain`.

- [ ] **Step 1: Add the new localization strings**

In `VideoDownloader.Core/Localization.cs`, the dictionary currently has a `// FrmAbout` section ending with:

```csharp
            // FrmAbout
            ["About VideoDownloader"] = "O programie VideoDownloader",
            ["Version:"] = "Wersja:",
            ["Build date:"] = "Data kompilacji:",
            ["Author:"] = "Autor:",
            ["Uses the following tools:"] = "Korzysta z następujących narzędzi:",
            ["Close"] = "Zamknij",

            // DownloadJobListBoxItem
```

Insert a new `// FrmHistory` section between them:

```csharp
            // FrmAbout
            ["About VideoDownloader"] = "O programie VideoDownloader",
            ["Version:"] = "Wersja:",
            ["Build date:"] = "Data kompilacji:",
            ["Author:"] = "Autor:",
            ["Uses the following tools:"] = "Korzysta z następujących narzędzi:",
            ["Close"] = "Zamknij",

            // FrmHistory
            ["🕑 History"] = "🕑 Historia",
            ["Download history"] = "Historia pobrań",
            ["Date"] = "Data",
            ["Title"] = "Tytuł",
            ["URL"] = "URL",
            ["Duration"] = "Czas trwania",
            ["Size"] = "Rozmiar",
            ["Avg. speed"] = "Śr. prędkość",
            ["Status"] = "Status",
            ["Completed"] = "Zakończono",
            ["Failed"] = "Niepowodzenie",
            ["Canceled"] = "Anulowano",

            // DownloadJobListBoxItem
```

- [ ] **Step 2: Create `FrmHistory.Designer.cs`**

Create `VideoDownloader/FrmHistory.Designer.cs`:

```csharp
namespace VideoDownloader
{
    partial class FrmHistory
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            dgvHistory = new DataGridView();
            colDate = new DataGridViewTextBoxColumn();
            colTitle = new DataGridViewTextBoxColumn();
            colUrl = new DataGridViewTextBoxColumn();
            colDuration = new DataGridViewTextBoxColumn();
            colSize = new DataGridViewTextBoxColumn();
            colSpeed = new DataGridViewTextBoxColumn();
            colStatus = new DataGridViewTextBoxColumn();
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
            SuspendLayout();
            //
            // dgvHistory
            //
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.AllowUserToDeleteRows = false;
            dgvHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvHistory.Columns.AddRange(new DataGridViewColumn[] { colDate, colTitle, colUrl, colDuration, colSize, colSpeed, colStatus });
            dgvHistory.Location = new Point(12, 12);
            dgvHistory.Name = "dgvHistory";
            dgvHistory.ReadOnly = true;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.Size = new Size(776, 400);
            dgvHistory.TabIndex = 0;
            //
            // colDate
            //
            colDate.Name = "colDate";
            colDate.ReadOnly = true;
            colDate.HeaderText = "Date";
            colDate.Width = 130;
            //
            // colTitle
            //
            colTitle.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colTitle.Name = "colTitle";
            colTitle.ReadOnly = true;
            colTitle.HeaderText = "Title";
            //
            // colUrl
            //
            colUrl.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colUrl.Name = "colUrl";
            colUrl.ReadOnly = true;
            colUrl.HeaderText = "URL";
            //
            // colDuration
            //
            colDuration.Name = "colDuration";
            colDuration.ReadOnly = true;
            colDuration.HeaderText = "Duration";
            colDuration.Width = 80;
            //
            // colSize
            //
            colSize.Name = "colSize";
            colSize.ReadOnly = true;
            colSize.HeaderText = "Size";
            colSize.Width = 90;
            //
            // colSpeed
            //
            colSpeed.Name = "colSpeed";
            colSpeed.ReadOnly = true;
            colSpeed.HeaderText = "Avg. speed";
            colSpeed.Width = 100;
            //
            // colStatus
            //
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            colStatus.HeaderText = "Status";
            colStatus.Width = 90;
            //
            // btnClose
            //
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.DialogResult = DialogResult.OK;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Location = new Point(688, 418);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(100, 30);
            btnClose.TabIndex = 1;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            //
            // FrmHistory
            //
            AcceptButton = btnClose;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(800, 460);
            Controls.Add(dgvHistory);
            Controls.Add(btnClose);
            Font = new Font("Segoe UI", 9.75F);
            MinimumSize = new Size(600, 300);
            Name = "FrmHistory";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Download history";
            Load += FrmHistory_Load;
            ((System.ComponentModel.ISupportInitialize)dgvHistory).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dgvHistory;
        private DataGridViewTextBoxColumn colDate;
        private DataGridViewTextBoxColumn colTitle;
        private DataGridViewTextBoxColumn colUrl;
        private DataGridViewTextBoxColumn colDuration;
        private DataGridViewTextBoxColumn colSize;
        private DataGridViewTextBoxColumn colSpeed;
        private DataGridViewTextBoxColumn colStatus;
        private Button btnClose;
    }
}
```

- [ ] **Step 3: Create `FrmHistory.cs`**

Create `VideoDownloader/FrmHistory.cs`:

```csharp
using VideoDownloader.Core;
using VideoDownloader.Core.History;
using VideoDownloader.Core.Tools;

namespace VideoDownloader
{
    public partial class FrmHistory : Form
    {
        private readonly HistoryRepository _history = new();

        public FrmHistory()
        {
            InitializeComponent();
        }

        private async void FrmHistory_Load(object sender, EventArgs e)
        {
            Text = Localization.T("Download history");
            colDate.HeaderText = Localization.T("Date");
            colTitle.HeaderText = Localization.T("Title");
            colUrl.HeaderText = Localization.T("URL");
            colDuration.HeaderText = Localization.T("Duration");
            colSize.HeaderText = Localization.T("Size");
            colSpeed.HeaderText = Localization.T("Avg. speed");
            colStatus.HeaderText = Localization.T("Status");
            btnClose.Text = Localization.T("Close");

            Cursor = Cursors.WaitCursor;
            try
            {
                var entries = await _history.GetAllAsync();
                foreach (var entry in entries)
                {
                    dgvHistory.Rows.Add(
                        entry.StartedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        entry.Title,
                        entry.Url,
                        Time.FromSeconds((float)entry.DurationSeconds),
                        entry.FileSizeBytes.HasValue ? DataSize.FromBytes(entry.FileSizeBytes.Value) : "-",
                        entry.AverageSpeedBytesPerSec.HasValue ? DataSize.FromBytes(entry.AverageSpeedBytesPerSec.Value) + "/s" : "-",
                        Localization.T(entry.Status));
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
```

- [ ] **Step 4: Add the "🕑 History" toolbar label to `FrmMain.Designer.cs`**

In `VideoDownloader/FrmMain.Designer.cs`, the field-declaration block near the top of `InitializeComponent()` currently ends the status-strip items with:

```csharp
            lBuyMeCoffee = new ToolStripStatusLabel();
            lAbout = new ToolStripStatusLabel();
            cbLanguage = new ToolStripComboBox();
```

Change to:

```csharp
            lBuyMeCoffee = new ToolStripStatusLabel();
            lAbout = new ToolStripStatusLabel();
            lHistory = new ToolStripStatusLabel();
            cbLanguage = new ToolStripComboBox();
```

The `status.Items.AddRange` call currently reads:

```csharp
            status.Items.AddRange(new ToolStripItem[] { lStatus, lMaxConcurrent, tshMaxConcurrent, lBuyMeCoffee, lAbout, cbLanguage });
```

Change to:

```csharp
            status.Items.AddRange(new ToolStripItem[] { lStatus, lMaxConcurrent, tshMaxConcurrent, lBuyMeCoffee, lAbout, lHistory, cbLanguage });
```

The `lAbout` init block currently reads:

```csharp
            //
            // lAbout
            //
            lAbout.ForeColor = Color.FromArgb(0, 102, 204);
            lAbout.Name = "lAbout";
            lAbout.Size = new Size(50, 17);
            lAbout.Text = "ℹ️ About";
            lAbout.TextAlign = ContentAlignment.MiddleRight;
            lAbout.Click += lAbout_Click;
            //
            // cbLanguage
            //
```

Insert an `lHistory` init block between them:

```csharp
            //
            // lAbout
            //
            lAbout.ForeColor = Color.FromArgb(0, 102, 204);
            lAbout.Name = "lAbout";
            lAbout.Size = new Size(50, 17);
            lAbout.Text = "ℹ️ About";
            lAbout.TextAlign = ContentAlignment.MiddleRight;
            lAbout.Click += lAbout_Click;
            //
            // lHistory
            //
            lHistory.ForeColor = Color.FromArgb(0, 102, 204);
            lHistory.Name = "lHistory";
            lHistory.Size = new Size(60, 17);
            lHistory.Text = "🕑 History";
            lHistory.TextAlign = ContentAlignment.MiddleRight;
            lHistory.Click += lHistory_Click;
            //
            // cbLanguage
            //
```

Finally, the field-declarations section at the bottom of the file currently reads:

```csharp
        private ToolStripStatusLabel lBuyMeCoffee;
        private ToolStripStatusLabel lAbout;
        private ToolStripComboBox cbLanguage;
```

Change to:

```csharp
        private ToolStripStatusLabel lBuyMeCoffee;
        private ToolStripStatusLabel lAbout;
        private ToolStripStatusLabel lHistory;
        private ToolStripComboBox cbLanguage;
```

- [ ] **Step 5: Wire the click handler and localization in `FrmMain.cs`**

In `VideoDownloader/FrmMain.cs`, `ApplyLocalization()` currently reads:

```csharp
            lBuyMeCoffee.Text = Localization.T("☕ Buy me a coffee");
            lAbout.Text = Localization.T("ℹ️ About");
```

Change to:

```csharp
            lBuyMeCoffee.Text = Localization.T("☕ Buy me a coffee");
            lAbout.Text = Localization.T("ℹ️ About");
            lHistory.Text = Localization.T("🕑 History");
```

The `lAbout_Click` handler currently reads:

```csharp
        private void lAbout_Click(object sender, EventArgs e)
        {
            using (FrmAbout frm = new FrmAbout())
            {
                frm.ShowDialog(this);
            }
        }
```

Add a matching handler right after it:

```csharp
        private void lAbout_Click(object sender, EventArgs e)
        {
            using (FrmAbout frm = new FrmAbout())
            {
                frm.ShowDialog(this);
            }
        }

        private void lHistory_Click(object sender, EventArgs e)
        {
            using (FrmHistory frm = new FrmHistory())
            {
                frm.ShowDialog(this);
            }
        }
```

- [ ] **Step 6: Build to verify**

Run: `dotnet build VideoDownloader\VideoDownloader.csproj -c Release`
Expected: Build succeeds with 0 errors.

Then check that the SQLite native asset landed in the build output (confirms `Microsoft.Data.Sqlite`'s native dependency resolved correctly for this app):

Run: `Get-ChildItem -Recurse VideoDownloader\bin\Release\net8.0-windows\ -Filter e_sqlite3.dll`
Expected: At least one `e_sqlite3.dll` is listed (typically under a `runtimes\win-x64\native\` subfolder of the output directory).

- [ ] **Step 7: Manual smoke test**

Run: `dotnet run --project VideoDownloader`
In the running app: click "🕑 History" in the status bar. Expected: `FrmHistory` opens with an empty grid (no downloads recorded yet) and correctly localized column headers/title in whatever language the app is currently set to. Close it, then switch the language dropdown and reopen History to confirm the headers/title re-localize too. Close the app.

- [ ] **Step 8: Commit**

```bash
git add VideoDownloader/FrmHistory.cs VideoDownloader/FrmHistory.Designer.cs VideoDownloader/FrmMain.Designer.cs VideoDownloader/FrmMain.cs VideoDownloader.Core/Localization.cs
git commit -m "Add WinForms download history screen"
```

---

### Task 6: WPF history screen (`HistoryWindow`) and toolbar entry point

**Files:**
- Create: `VideoDownloader.Wpf/ViewModels/HistoryViewModel.cs`
- Create: `VideoDownloader.Wpf/Views/HistoryWindow.xaml`
- Create: `VideoDownloader.Wpf/Views/HistoryWindow.xaml.cs`
- Modify: `VideoDownloader.Wpf/Services/IDialogService.cs`
- Modify: `VideoDownloader.Wpf/Services/DialogService.cs`
- Modify: `VideoDownloader.Wpf/ViewModels/MainViewModel.cs`
- Modify: `VideoDownloader.Wpf/MainWindow.xaml`

**Interfaces:**
- Consumes: `HistoryRepository` (Task 2), `DownloadHistoryEntry` (Task 1), `DataSize.FromBytes`/`Time.FromSeconds` (Task 1 / existing), `Localization.T` (existing), all localization keys added in Task 5's `Localization.cs` edit.
- Produces: `HistoryViewModel` with `ObservableCollection<HistoryRowViewModel> Entries` and `Task LoadAsync()`; `IDialogService.ShowHistory() → Task`; `MainViewModel.HistoryCommand` (generated by `[RelayCommand]` on a `History()` method).

- [ ] **Step 1: Create `HistoryViewModel`**

Create `VideoDownloader.Wpf/ViewModels/HistoryViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using VideoDownloader.Core.History;
using VideoDownloader.Core.Tools;

namespace VideoDownloader.Wpf.ViewModels
{
    public class HistoryViewModel
    {
        private readonly HistoryRepository _history = new();

        public ObservableCollection<HistoryRowViewModel> Entries { get; } = new();

        public async Task LoadAsync()
        {
            Entries.Clear();
            var entries = await _history.GetAllAsync();
            foreach (var entry in entries)
                Entries.Add(new HistoryRowViewModel(entry));
        }
    }

    /// <summary>Pre-formatted, display-ready view of one <see cref="DownloadHistoryEntry"/> row.</summary>
    public class HistoryRowViewModel
    {
        public HistoryRowViewModel(DownloadHistoryEntry entry)
        {
            Date = entry.StartedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            Title = entry.Title;
            Url = entry.Url;
            Duration = Time.FromSeconds((float)entry.DurationSeconds);
            Size = entry.FileSizeBytes.HasValue ? DataSize.FromBytes(entry.FileSizeBytes.Value) : "-";
            Speed = entry.AverageSpeedBytesPerSec.HasValue ? DataSize.FromBytes(entry.AverageSpeedBytesPerSec.Value) + "/s" : "-";
            Status = Core.Localization.T(entry.Status);
        }

        public string Date { get; }
        public string Title { get; }
        public string Url { get; }
        public string Duration { get; }
        public string Size { get; }
        public string Speed { get; }
        public string Status { get; }
    }
}
```

- [ ] **Step 2: Create `HistoryWindow.xaml`**

Create `VideoDownloader.Wpf/Views/HistoryWindow.xaml`:

```xml
<Window x:Class="VideoDownloader.Wpf.Views.HistoryWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:loc="clr-namespace:VideoDownloader.Wpf.Localization"
        Title="{loc:Tr 'Download history'}" Height="480" Width="820" MinWidth="600" MinHeight="300"
        WindowStartupLocation="CenterOwner"
        Background="{StaticResource BackgroundBrush}">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <ListView Grid.Row="0" ItemsSource="{Binding Entries}">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="{loc:Tr 'Date'}" DisplayMemberBinding="{Binding Date}" Width="130" />
                    <GridViewColumn Header="{loc:Tr 'Title'}" DisplayMemberBinding="{Binding Title}" Width="220" />
                    <GridViewColumn Header="{loc:Tr 'URL'}" DisplayMemberBinding="{Binding Url}" Width="180" />
                    <GridViewColumn Header="{loc:Tr 'Duration'}" DisplayMemberBinding="{Binding Duration}" Width="80" />
                    <GridViewColumn Header="{loc:Tr 'Size'}" DisplayMemberBinding="{Binding Size}" Width="90" />
                    <GridViewColumn Header="{loc:Tr 'Avg. speed'}" DisplayMemberBinding="{Binding Speed}" Width="100" />
                    <GridViewColumn Header="{loc:Tr 'Status'}" DisplayMemberBinding="{Binding Status}" Width="90" />
                </GridView>
            </ListView.View>
        </ListView>

        <Button Grid.Row="1" Content="{loc:Tr 'Close'}" Style="{StaticResource PrimaryButtonStyle}"
                HorizontalAlignment="Right" Width="100" Margin="0,12,0,0" Click="Close_Click" />
    </Grid>
</Window>
```

- [ ] **Step 3: Create `HistoryWindow.xaml.cs`**

Create `VideoDownloader.Wpf/Views/HistoryWindow.xaml.cs`:

```csharp
using System.Windows;

namespace VideoDownloader.Wpf.Views
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
```

- [ ] **Step 4: Add `ShowHistory()` to `IDialogService`**

In `VideoDownloader.Wpf/Services/IDialogService.cs`, current content:

```csharp
    public interface IDialogService
    {
        void ShowAbout();

        /// <summary>Shows a Yes/No confirmation dialog; returns true if the user confirmed.</summary>
        bool Confirm(string message, string title);
```

Change to:

```csharp
    public interface IDialogService
    {
        void ShowAbout();

        /// <summary>Shows the download history screen.</summary>
        Task ShowHistory();

        /// <summary>Shows a Yes/No confirmation dialog; returns true if the user confirmed.</summary>
        bool Confirm(string message, string title);
```

- [ ] **Step 5: Implement `ShowHistory()` in `DialogService`**

In `VideoDownloader.Wpf/Services/DialogService.cs`, current `ShowAbout()`:

```csharp
        public void ShowAbout()
        {
            var window = new AboutWindow { DataContext = new AboutViewModel() };
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }
```

Add `ShowHistory()` right after it:

```csharp
        public void ShowAbout()
        {
            var window = new AboutWindow { DataContext = new AboutViewModel() };
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        public async Task ShowHistory()
        {
            var viewModel = new HistoryViewModel();

            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                await viewModel.LoadAsync();
            }
            finally
            {
                System.Windows.Input.Mouse.OverrideCursor = null;
            }

            var window = new HistoryWindow { DataContext = viewModel };
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }
```

- [ ] **Step 6: Add `HistoryCommand` to `MainViewModel`**

In `VideoDownloader.Wpf/ViewModels/MainViewModel.cs`, current `About()` command:

```csharp
        [RelayCommand]
        private void About() => _dialogService.ShowAbout();
```

Add a `History()` command right after it:

```csharp
        [RelayCommand]
        private void About() => _dialogService.ShowAbout();

        [RelayCommand]
        private async Task History() => await _dialogService.ShowHistory();
```

- [ ] **Step 7: Add the History button to `MainWindow.xaml`**

In `VideoDownloader.Wpf/MainWindow.xaml`, the status bar currently reads:

```xml
                    <Button Content="{loc:Tr 'ℹ️ About'}" Style="{StaticResource FlatButtonStyle}" Margin="16,0,0,0" Command="{Binding AboutCommand}" />
                    <Button Content="{loc:Tr '☕ Buy me a coffee'}" Style="{StaticResource FlatButtonStyle}" Margin="8,0,0,0" Command="{Binding BuyCoffeeCommand}" />
```

Change to:

```xml
                    <Button Content="{loc:Tr 'ℹ️ About'}" Style="{StaticResource FlatButtonStyle}" Margin="16,0,0,0" Command="{Binding AboutCommand}" />
                    <Button Content="{loc:Tr '🕑 History'}" Style="{StaticResource FlatButtonStyle}" Margin="8,0,0,0" Command="{Binding HistoryCommand}" />
                    <Button Content="{loc:Tr '☕ Buy me a coffee'}" Style="{StaticResource FlatButtonStyle}" Margin="8,0,0,0" Command="{Binding BuyCoffeeCommand}" />
```

- [ ] **Step 8: Build to verify**

Run: `dotnet build VideoDownloader.Wpf\VideoDownloader.Wpf.csproj -c Release`
Expected: Build succeeds with 0 errors (the `[RelayCommand]` source generator produces `HistoryCommand` from `History()` — if this fails to resolve in XAML, check the generated `MainViewModel.g.cs` under `obj\` for a method named `HistoryCommand`).

Then check the SQLite native asset landed in this app's output too:

Run: `Get-ChildItem -Recurse VideoDownloader.Wpf\bin\Release\net8.0-windows\ -Filter e_sqlite3.dll`
Expected: At least one `e_sqlite3.dll` is listed.

- [ ] **Step 9: Manual smoke test**

Run: `dotnet run --project VideoDownloader.Wpf`
In the running app: click "🕑 History" in the status bar. Expected: `HistoryWindow` opens with an empty list (or, if Task 5's WinForms smoke test already ran and recorded rows, whatever rows exist — remember the DB is shared) and correctly localized column headers/title. Close it, then close the app.

- [ ] **Step 10: Commit**

```bash
git add VideoDownloader.Wpf/ViewModels/HistoryViewModel.cs VideoDownloader.Wpf/Views/HistoryWindow.xaml VideoDownloader.Wpf/Views/HistoryWindow.xaml.cs VideoDownloader.Wpf/Services/IDialogService.cs VideoDownloader.Wpf/Services/DialogService.cs VideoDownloader.Wpf/ViewModels/MainViewModel.cs VideoDownloader.Wpf/MainWindow.xaml
git commit -m "Add WPF download history screen"
```

---

### Task 7: Manual end-to-end verification

**Files:** none (verification only; fix forward in the relevant earlier task's files if something's broken, then re-run this task).

- [ ] **Step 1: Completed download records correctly**

Run `dotnet run --project VideoDownloader`. Download a short real video to completion. Open "🕑 History". Expected: a `Completed`-status row with today's date, the video's title, a non-"-" size and speed, and a duration roughly matching how long the download actually took.

- [ ] **Step 2: Canceled download records correctly**

Start another download and cancel it partway through (via the job's cancel control). Open "🕑 History" again. Expected: a new row with status `Canceled` (localized), and size/speed either populated (if some data had already downloaded) or "-" (if canceled immediately).

- [ ] **Step 3: Failed download records correctly**

Trigger a failure — e.g. paste a malformed/unreachable URL and attempt to download. Expected: a new row with status `Failed` (localized). (`ErrorMessage` is stored in the DB but not currently shown in either grid — this is fine per the spec; it's there for future use.)

- [ ] **Step 4: History is shared between both apps**

Close `VideoDownloader`, run `dotnet run --project VideoDownloader.Wpf`, open its History window. Expected: all three rows from Steps 1-3 appear there too, proving both apps read/write the same `history.db`.

- [ ] **Step 5: Confirm the DB file itself**

Run: `Get-Item "$env:LOCALAPPDATA\APPIT\VideoDownloader\history.db"`
Expected: the file exists and has non-zero size.

No commit for this task (no files change) unless a verification step fails and needs a fix — if so, make the fix in the appropriate earlier task's file(s), re-run that task's build/smoke-test steps, commit the fix there, then re-run this task's steps from the top.
