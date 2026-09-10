# WinForms → WPF Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a WPF front end (`VideoDownloader.Wpf`) alongside the existing WinForms app, sharing download/queue/notification logic through a new `VideoDownloader.Core` class library, with no behavior change to the WinForms app.

**Architecture:** Extract everything UI-agnostic (plus a newly-decoupled `Downloader`, `DownloadJob`, `Notifications`, `DownloadQueueManager`) into `VideoDownloader.Core`. Rewire the existing WinForms app to consume it (no visible change). Build `VideoDownloader.Wpf` as a pure MVVM view layer (CommunityToolkit.Mvvm) on top of the same `Core`.

**Tech Stack:** .NET 8, WinForms, WPF, CommunityToolkit.Mvvm, YoutubeDLSharp.

**Spec:** `docs/superpowers/specs/2026-09-10-wpf-migration-design.md`

## Global Constraints

- No automated test suite exists in this repo (per `CLAUDE.md`) — every task's "test cycle" is: solution builds, and (where noted) the app is actually run to confirm the change works. Do not invent a test project.
- `VideoDownloader.Core` must never reference WinForms or WPF assemblies.
- No behavior change to the WinForms app's user-facing behavior at any point — each task that touches WinForms must leave it buildable and working the same as before.
- No dark mode, no WPF installer/MSI in this plan (see spec Non-goals).
- Binary paths (`yt-dlp.exe`, `ffmpeg.exe`) are always resolved via `AppContext.BaseDirectory`, never `PATH` — this applies to any new code that shells out.
- Localization: every new user-facing string goes through `Localization.T(...)` with a Polish dictionary entry added in the same task.

---

## Part 1 — Core extraction (WinForms stays working throughout)

### Task 1: Create `VideoDownloader.Core` project and wire it into the solution

**Files:**
- Create: `VideoDownloader.Core/VideoDownloader.Core.csproj`
- Modify: `VideoDownloader.sln`
- Modify: `VideoDownloader/VideoDownloader.csproj`

**Interfaces:**
- Produces: an empty, buildable `VideoDownloader.Core` class library that `VideoDownloader.csproj` references.

- [ ] **Step 1: Create the class library project**

```bash
mkdir VideoDownloader.Core
```

Write `VideoDownloader.Core/VideoDownloader.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

- [ ] **Step 2: Add the project to the solution**

```bash
dotnet sln VideoDownloader.sln add VideoDownloader.Core/VideoDownloader.Core.csproj
```

- [ ] **Step 3: Reference it from the WinForms project**

```bash
dotnet add VideoDownloader/VideoDownloader.csproj reference VideoDownloader.Core/VideoDownloader.Core.csproj
```

- [ ] **Step 4: Verify the solution builds**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds, 3 projects reported (VideoDownloader, VideoDownloader.Core, FFmpegBuild is excluded from default build as already configured).

- [ ] **Step 5: Commit**

```bash
git add VideoDownloader.Core VideoDownloader.sln VideoDownloader/VideoDownloader.csproj
git commit -m "Add empty VideoDownloader.Core project"
```

---

### Task 2: Move pure utility files into Core

**Files:**
- Move: `VideoDownloader/Core/Helper.cs` → `VideoDownloader.Core/Helper.cs`
- Move: `VideoDownloader/Core/Errors.cs` → `VideoDownloader.Core/Errors.cs`
- Move: `VideoDownloader/Core/Localization.cs` → `VideoDownloader.Core/Localization.cs`
- Move: `VideoDownloader/Core/Windows/Registry.cs` → `VideoDownloader.Core/Windows/Registry.cs`
- Move: `VideoDownloader/Core/Tools/DataSize.cs` → `VideoDownloader.Core/Tools/DataSize.cs`
- Move: `VideoDownloader/Core/Tools/Time.cs` → `VideoDownloader.Core/Tools/Time.cs`
- Move: `VideoDownloader/Core/Tools/YtDlpUpdater.cs` → `VideoDownloader.Core/Tools/YtDlpUpdater.cs`
- Modify: `VideoDownloader.Core/Tools/Time.cs` (`internal class Time` → `public class Time`, needed cross-assembly by `FrmMain.cs`)

**Interfaces:**
- Consumes: nothing new.
- Produces: `VideoDownloader.Core.Helper.URL.Verify(string)`, `VideoDownloader.Core.Errors.ParseErrorMessage(Exception)`, `VideoDownloader.Core.Localization` (unchanged public surface), `VideoDownloader.Core.Windows.Registry` (unchanged public surface), `VideoDownloader.Core.Tools.Time.FromSeconds(float)` (now public), `VideoDownloader.Core.Tools.DataSize.FromBytes(double)`, `VideoDownloader.Core.Tools.YtDlpUpdater.UpdateAsync()`.

None of these files change namespace — they're already `VideoDownloader.Core` / `VideoDownloader.Core.Windows` / `VideoDownloader.Core.Tools`, so no `using` changes are needed anywhere else in the WinForms project.

- [ ] **Step 1: Move the files with git, preserving history**

```bash
mkdir -p VideoDownloader.Core/Windows VideoDownloader.Core/Tools
git mv VideoDownloader/Core/Helper.cs VideoDownloader.Core/Helper.cs
git mv VideoDownloader/Core/Errors.cs VideoDownloader.Core/Errors.cs
git mv VideoDownloader/Core/Localization.cs VideoDownloader.Core/Localization.cs
git mv VideoDownloader/Core/Windows/Registry.cs VideoDownloader.Core/Windows/Registry.cs
git mv VideoDownloader/Core/Tools/DataSize.cs VideoDownloader.Core/Tools/DataSize.cs
git mv VideoDownloader/Core/Tools/Time.cs VideoDownloader.Core/Tools/Time.cs
git mv VideoDownloader/Core/Tools/YtDlpUpdater.cs VideoDownloader.Core/Tools/YtDlpUpdater.cs
```

- [ ] **Step 2: Make `Time` public**

In `VideoDownloader.Core/Tools/Time.cs`, change:

```csharp
internal class Time
```

to:

```csharp
public class Time
```

- [ ] **Step 3: Verify the solution builds**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds — the WinForms project picks these types up unchanged through the project reference (SDK-style projects auto-include `.cs` files, no `.csproj` edits needed on either side).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Move UI-agnostic utility classes into VideoDownloader.Core"
```

---

### Task 3: Move `DataSource` types into Core

**Files:**
- Move: `VideoDownloader/Core/DataSource.cs` → `VideoDownloader.Core/DataSource.cs`
- Modify: `VideoDownloader.Core/VideoDownloader.Core.csproj` (add `YoutubeDLSharp` package reference — `DataSource.cs` has `implicit operator` conversions from `YoutubeDLSharp.Metadata.FormatData`)

**Interfaces:**
- Produces: `VideoDownloader.Core.DataSource`, `VideoDownloader.Core.Source`, `VideoDownloader.Core.DataAudioSource`, `VideoDownloader.Core.DataVideoSource` (unchanged public surface).

- [ ] **Step 1: Move the file**

```bash
git mv VideoDownloader/Core/DataSource.cs VideoDownloader.Core/DataSource.cs
```

- [ ] **Step 2: Add the YoutubeDLSharp package reference to Core**

In `VideoDownloader.Core/VideoDownloader.Core.csproj`, add:

```xml
  <ItemGroup>
    <PackageReference Include="YoutubeDLSharp" Version="1.2.0" />
  </ItemGroup>
```

- [ ] **Step 3: Verify the solution builds**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Move DataSource types into VideoDownloader.Core"
```

---

### Task 4: Add `DownloadJob` to Core (new, standalone)

**Files:**
- Create: `VideoDownloader.Core/DownloadJob.cs`

**Interfaces:**
- Produces: `VideoDownloader.Core.DownloadingState` enum (`Waiting`, `Downloading`, `Completed`, `Failed`, `Canceled`); `VideoDownloader.Core.DownloadJob` with settable `Title`, `Url`, `Resolution`, `Format`, `Duration` (all `string`), `State` (`DownloadingState`), `ProgressPercentage` (`double`), `Speed` (`string`), `ETA` (`string`), `FileSize` (`string`), `CancellationTokenSource` (`CancellationTokenSource?`), methods `DownloadBegin()` and `DownloadEnd()`, and `event PropertyChangedEventHandler? PropertyChanged`.

This task adds a file nothing references yet — it cannot break the build.

- [ ] **Step 1: Write `DownloadJob.cs`**

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoDownloader.Core
{
    public enum DownloadingState
    {
        Waiting,
        Downloading,
        Completed,
        Failed,
        Canceled
    }

    /// <summary>
    /// UI-agnostic model for one download job. <see cref="Downloader.Download"/> writes
    /// progress into this; WinForms/WPF views observe <see cref="PropertyChanged"/> to render it.
    /// </summary>
    public class DownloadJob : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _url = string.Empty;
        private string _resolution = string.Empty;
        private string _format = string.Empty;
        private string _duration = string.Empty;
        private DownloadingState _state = DownloadingState.Waiting;
        private double _progressPercentage;
        private string _speed = string.Empty;
        private string _eta = string.Empty;
        private string _fileSize = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title { get => _title; set => SetField(ref _title, value); }
        public string Url { get => _url; set => SetField(ref _url, value); }
        public string Resolution { get => _resolution; set => SetField(ref _resolution, value); }
        public string Format { get => _format; set => SetField(ref _format, value); }
        public string Duration { get => _duration; set => SetField(ref _duration, value); }
        public DownloadingState State { get => _state; set => SetField(ref _state, value); }
        public double ProgressPercentage { get => _progressPercentage; set => SetField(ref _progressPercentage, value); }
        public string Speed { get => _speed; set => SetField(ref _speed, value); }
        public string ETA { get => _eta; set => SetField(ref _eta, value); }
        public string FileSize { get => _fileSize; set => SetField(ref _fileSize, value); }

        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public void DownloadBegin()
        {
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new CancellationTokenSource();
            State = DownloadingState.Downloading;
        }

        public void DownloadEnd()
        {
            State = DownloadingState.Completed;
            ProgressPercentage = 100.0;
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
```

- [ ] **Step 2: Verify the solution builds**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add VideoDownloader.Core/DownloadJob.cs
git commit -m "Add DownloadJob model to VideoDownloader.Core"
```

---

### Task 5: Rework `Notifications` to be framework-agnostic, adapt WinForms

**Files:**
- Create: `VideoDownloader.Core/NotificationItem.cs`
- Move + rewrite: `VideoDownloader/Core/Notifications.cs` → `VideoDownloader.Core/Notifications.cs`
- Modify: `VideoDownloader/Core/NotificationControl.cs` (becomes a dumb renderer, loses its own timer)
- Modify: `VideoDownloader/FrmMain.cs` (construct `Notifications` without a `Panel`, sync `flpNotifications` to the collection)

**Interfaces:**
- Consumes: nothing new.
- Produces: `VideoDownloader.Core.NotificationItem` (`Type`, `Message`, `Timestamp`); `VideoDownloader.Core.Notifications` with `ObservableCollection<NotificationItem> Items`, and unchanged `AddMessage(NotificationType, string)` / `Info` / `Warning` / `Error` / `Success` / `ClearOldNotifications()`.

- [ ] **Step 1: Write `NotificationItem.cs`**

```csharp
namespace VideoDownloader.Core
{
    public class NotificationItem
    {
        public required Notifications.NotificationType Type { get; init; }
        public required string Message { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.Now;
    }
}
```

- [ ] **Step 2: Move and rewrite `Notifications.cs`**

```bash
git mv VideoDownloader/Core/Notifications.cs VideoDownloader.Core/Notifications.cs
```

Replace its contents:

```csharp
using System.Collections.ObjectModel;

namespace VideoDownloader.Core
{
    public class Notifications
    {
        public enum NotificationType
        {
            Info,
            Warning,
            Error,
            Success
        }

        private const int AutoDismissMilliseconds = 10000;

        public ObservableCollection<NotificationItem> Items { get; } = new();

        public void AddMessage(NotificationType notificationType, string message)
        {
            var item = new NotificationItem { Type = notificationType, Message = message };
            Items.Add(item);

            var timer = new System.Threading.Timer(_ =>
            {
                Items.Remove(item);
            }, null, AutoDismissMilliseconds, Timeout.Infinite);
        }

        public void Info(string message) => AddMessage(NotificationType.Info, message);
        public void Success(string message) => AddMessage(NotificationType.Success, message);
        public void Warning(string message) => AddMessage(NotificationType.Warning, message);
        public void Error(string message) => AddMessage(NotificationType.Error, message);

        public void ClearMessages() => Items.Clear();

        internal void ClearOldNotifications() => ClearMessages();
    }
}
```

> Note: `ObservableCollection<T>` raises `CollectionChanged` on the thread that mutates it. The `System.Threading.Timer` callback runs on a thread-pool thread, so its `Items.Remove(item)` call must be marshalled back to the UI thread by whichever view is subscribed (WinForms: `Control.Invoke`; WPF: `Dispatcher.Invoke`) — handle this in the subscriber, not here, to keep `Core` UI-framework-free. Document this in Step 4 below.

- [ ] **Step 3: Simplify `NotificationControl`**

Read the current `VideoDownloader/Core/NotificationControl.cs` and `.Designer.cs` first. Replace the code-behind with:

```csharp
namespace VideoDownloader.Core
{
    public partial class NotificationControl : UserControl
    {
        public NotificationControl()
        {
            InitializeComponent();
        }

        public NotificationControl(NotificationItem item) : this()
        {
            lMessage.Text = item.Message;
            BackColor = item.Type switch
            {
                Notifications.NotificationType.Info => Color.LightBlue,
                Notifications.NotificationType.Warning => Color.Khaki,
                Notifications.NotificationType.Error => Color.LightCoral,
                Notifications.NotificationType.Success => Color.LightGreen,
                _ => SystemColors.Control
            };
        }
    }
}
```

(The `Designer.cs` file is unchanged — it only declares `lMessage` and layout.)

- [ ] **Step 4: Rewire `FrmMain`**

In `VideoDownloader/FrmMain.cs`:

Replace:

```csharp
_Notifications = new Notifications(flpNotifications);
```

with:

```csharp
_Notifications = new Notifications();
_Notifications.Items.CollectionChanged += Notifications_CollectionChanged;
```

Add a new method (near the other `#region Notifications Functions` handlers):

```csharp
private void Notifications_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
{
    if (InvokeRequired)
    {
        Invoke(() => Notifications_CollectionChanged(sender, e));
        return;
    }

    if (e.OldItems != null)
    {
        foreach (NotificationItem removed in e.OldItems)
        {
            var control = flpNotifications.Controls.OfType<NotificationControl>()
                .FirstOrDefault(c => c.Tag == removed);
            control?.Dispose();
        }
    }

    if (e.NewItems != null)
    {
        foreach (NotificationItem added in e.NewItems)
        {
            var control = new NotificationControl(added) { Tag = added };
            flpNotifications.Controls.Add(control);
        }
    }
}
```

Replace every remaining `_Notifications.ClearOldNotifications();` — no change needed there, it still works (it now clears `Items`, which fires `CollectionChanged` with a `Reset` action). Handle `Reset` in the same method by adding, at its top:

```csharp
if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
{
    foreach (Control c in flpNotifications.Controls.OfType<NotificationControl>().ToArray())
        c.Dispose();
    return;
}
```

- [ ] **Step 5: Verify the solution builds and notifications still work**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

Run: `dotnet run --project VideoDownloader`
Manually trigger a notification (e.g. click Download with an empty destination path to get the "select a valid destination path" warning). Confirm the banner appears and auto-dismisses after ~10s.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Make Notifications framework-agnostic, rewire WinForms to observe it"
```

---

### Task 6: Move `Downloader` into Core, retarget it at `DownloadJob`

**Files:**
- Move + modify: `VideoDownloader/Downloader.cs` → `VideoDownloader.Core/Downloader.cs`
- Modify: `VideoDownloader/Controls/DownloadJobListBoxItem.cs` (becomes a view over a `DownloadJob` instead of owning its own state)
- Modify: `VideoDownloader/FrmMain.cs` (construct a `DownloadJob`, pass it to `Downloader.Download`, wrap it in a `DownloadJobListBoxItem`)

**Interfaces:**
- Consumes: `VideoDownloader.Core.DownloadJob` (Task 4), `VideoDownloader.Core.Notifications` (Task 5), `VideoDownloader.Core.DataVideoSource`/`DataAudioSource` (Task 3).
- Produces: `VideoDownloader.Core.Downloader` with `Download((DataVideoSource video, DataAudioSource audio) sources, DownloadJob job)` (signature changed from `DownloadJobListBoxItem job`); everything else on `Downloader` (constructor, `SourceURL`, `DestinationPath`, `FetchSources()`, `Dispose()`) unchanged.

This is the pivotal task — `Downloader` cannot live in `Core` while still depending on the WinForms `DownloadJobListBoxItem`, and `DownloadJobListBoxItem` cannot keep being written into directly once `Downloader` moves. Both sides change together so the build stays green.

- [ ] **Step 1: Move `Downloader.cs`**

```bash
git mv VideoDownloader/Downloader.cs VideoDownloader.Core/Downloader.cs
```

- [ ] **Step 2: Retarget it at `DownloadJob`**

In `VideoDownloader.Core/Downloader.cs`:

Remove `using VideoDownloader.Controls;` (no longer needed).

Change the `Download` method signature and body from:

```csharp
public async Task Download((DataVideoSource video, DataAudioSource audio) sources, DownloadJobListBoxItem job)
{
    var progress = new Progress<DownloadProgress>(p => {
        if (p.Progress > 0)
            job.ProgressPercentage = (double)p.Progress * 100;

        job.DownloadSpeed = p.DownloadSpeed;
        job.ETA = p.ETA;
        job.VideoFileSize = p.TotalDownloadSize;
    });

    job.DownloadBegin();
```

to:

```csharp
public async Task Download((DataVideoSource video, DataAudioSource audio) sources, DownloadJob job)
{
    var progress = new Progress<DownloadProgress>(p => {
        if (p.Progress > 0)
            job.ProgressPercentage = (double)p.Progress * 100;

        job.Speed = p.DownloadSpeed;
        job.ETA = p.ETA;
        job.FileSize = p.TotalDownloadSize;
    });

    job.DownloadBegin();
```

Further down, change:

```csharp
                var extraOptions = new OptionSet()
                {
                    Output = Path.Combine(
                        Path.TrimEndingDirectorySeparator(DestinationPath),
                        string.Join("_", job.VideoTitle.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim() + "." +
                        job.VideoFormat),
```

to:

```csharp
                var extraOptions = new OptionSet()
                {
                    Output = Path.Combine(
                        Path.TrimEndingDirectorySeparator(DestinationPath),
                        string.Join("_", job.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim() + "." +
                        job.Format),
```

Change:

```csharp
                var result = await ytdl.RunVideoDownload(videoUrl,
                    $"{sources.video.Id}+{sources.audio.Id}",
                    DownloadMergeFormat.Mp4,
                    VideoRecodeFormat.Mp4,
                    job.CancellationToken.Token,
```

to:

```csharp
                var result = await ytdl.RunVideoDownload(videoUrl,
                    $"{sources.video.Id}+{sources.audio.Id}",
                    DownloadMergeFormat.Mp4,
                    VideoRecodeFormat.Mp4,
                    job.CancellationTokenSource!.Token,
```

Change the `catch`/`finally` block from:

```csharp
            catch (OperationCanceledException)
            {
                job.Dispose();
                return;
            }
            catch (Exception ex)
            {
                job.State = DownloadJobListBoxItem.DownloadingState.Failed;
                _Notifications.Error(string.Format(Localization.T("Download failed ({0}): {1}"), job.VideoTitle, Errors.ParseErrorMessage(ex)));
            }
            finally
            {
                job.CancellationToken.Dispose();
            }
```

to:

```csharp
            catch (OperationCanceledException)
            {
                job.State = DownloadingState.Canceled;
                return;
            }
            catch (Exception ex)
            {
                job.State = DownloadingState.Failed;
                _Notifications.Error(string.Format(Localization.T("Download failed ({0}): {1}"), job.Title, Errors.ParseErrorMessage(ex)));
            }
            finally
            {
                job.CancellationTokenSource?.Dispose();
            }
```

- [ ] **Step 3: Turn `DownloadJobListBoxItem` into a view over `DownloadJob`**

Read `VideoDownloader/Controls/DownloadJobListBoxItem.cs` and `.Designer.cs` first (control names: `lTitle`, `lUrl`, `lResolution`, `progressBar` (`ProgressBarExtended`), `lFormat`, `lDuration`, `lSize`, `btnCancel`).

Replace `VideoDownloader/Controls/DownloadJobListBoxItem.cs` contents with:

```csharp
using VideoDownloader.Core;

namespace VideoDownloader.Controls
{
    public partial class DownloadJobListBoxItem : UserControl
    {
        public DownloadJob Job { get; }

        public DownloadJobListBoxItem(DownloadJob job)
        {
            InitializeComponent();
            Job = job;
            Job.PropertyChanged += Job_PropertyChanged;
            RenderAll();
        }

        private void Job_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => Job_PropertyChanged(sender, e));
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(DownloadJob.Title): lTitle.Text = Job.Title; break;
                case nameof(DownloadJob.Url): lUrl.Text = Job.Url; break;
                case nameof(DownloadJob.Resolution): lResolution.Text = Localization.T("Resolution:") + " " + Job.Resolution; break;
                case nameof(DownloadJob.Format): lFormat.Text = Localization.T("Format:") + " " + Job.Format; break;
                case nameof(DownloadJob.Duration): lDuration.Text = Localization.T("Duration:") + " " + Job.Duration; break;
                case nameof(DownloadJob.FileSize): lSize.Text = Localization.T("Size (MB):") + " " + Job.FileSize; break;
                case nameof(DownloadJob.Speed): progressBar.Speed = Job.Speed; progressBar.Invalidate(); break;
                case nameof(DownloadJob.ETA): progressBar.ETA = Job.ETA; progressBar.Invalidate(); break;
                case nameof(DownloadJob.ProgressPercentage): progressBar.PreciseValue = Job.ProgressPercentage; break;
                case nameof(DownloadJob.State): RenderState(); break;
            }
        }

        private void RenderAll()
        {
            lTitle.Text = Job.Title;
            lUrl.Text = Job.Url;
            lResolution.Text = Localization.T("Resolution:") + " " + Job.Resolution;
            lFormat.Text = Localization.T("Format:") + " " + Job.Format;
            lDuration.Text = Localization.T("Duration:") + " " + Job.Duration;
            lSize.Text = Localization.T("Size (MB):") + " " + Job.FileSize;
            progressBar.Speed = Job.Speed;
            progressBar.ETA = Job.ETA;
            progressBar.PreciseValue = Job.ProgressPercentage;
            RenderState();
        }

        private void RenderState()
        {
            switch (Job.State)
            {
                case DownloadingState.Waiting:
                    progressBar.BackColor = Color.LightYellow;
                    break;
                case DownloadingState.Downloading:
                    progressBar.BackColor = progressBar.BaseBackColor;
                    break;
                case DownloadingState.Completed:
                    progressBar.BackColor = Color.LightSkyBlue;
                    btnCancel.Visible = false;
                    break;
                case DownloadingState.Failed:
                    progressBar.BackColor = Color.OrangeRed;
                    break;
                case DownloadingState.Canceled:
                    progressBar.BackColor = Color.LightSlateGray;
                    break;
            }
            progressBar.Invalidate();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (Job.State == DownloadingState.Downloading && MessageBox.Show(Localization.T("Are you sure you want to cancel downloading?"), Localization.T("Downloading"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Job.CancellationTokenSource?.Cancel();
                Job.State = DownloadingState.Canceled;
            }
        }
    }
}
```

Note the parameterless constructor is gone (the Designer file's `InitializeComponent()` still works fine being called from the new constructor — WinForms designer support for this control is no longer usable in the visual designer, which is expected and acceptable since it's now always constructed with a `DownloadJob`).

- [ ] **Step 4: Rewire `FrmMain`'s two download call sites**

Read the current `VideoDownloader/FrmMain.cs` (it changed in Task 5, Step 4).

In `BatchDownload`, replace:

```csharp
                    if (sources != null)
                    {
                        DownloadJobListBoxItem item = new DownloadJobListBoxItem();
                        item.VideoTitle = scForm.VideoTitle;
                        item.URL = downloader.SourceURL;
                        item.VideoResolution = sources.Item1.Resolution;
                        item.VideoFormat = sources.Item1.Extension;
                        item.VideoDuration = Time.FromSeconds(scForm.Duration);

                        EnqueueDownload(downloader, (sources.Item1, sources.Item2), item);
                        downloader = null;
                        tbLink.Clear();

                        return (sources.Item1, sources.Item2);
                    }
```

with:

```csharp
                    if (sources != null)
                    {
                        var job = new DownloadJob
                        {
                            Title = scForm.VideoTitle,
                            Url = downloader.SourceURL,
                            Resolution = sources.Item1.Resolution,
                            Format = sources.Item1.Extension,
                            Duration = Time.FromSeconds(scForm.Duration)
                        };

                        EnqueueDownload(downloader, (sources.Item1, sources.Item2), job);
                        downloader = null;
                        tbLink.Clear();

                        return (sources.Item1, sources.Item2);
                    }
```

In `btnDownload_Click`, replace:

```csharp
                        DownloadJobListBoxItem item = new DownloadJobListBoxItem();
                        item.VideoTitle = scForm.VideoTitle;
                        item.URL = downloader.SourceURL;
                        item.VideoResolution = scForm.SelectedSource.Item1.Resolution;
                        item.VideoFormat = scForm.SelectedSource.Item1.Extension;
                        item.VideoDuration = Time.FromSeconds(scForm.Duration);

                        EnqueueDownload(downloader, scForm.SelectedSource, item);
```

with:

```csharp
                        var job = new DownloadJob
                        {
                            Title = scForm.VideoTitle,
                            Url = downloader.SourceURL,
                            Resolution = scForm.SelectedSource.Item1.Resolution,
                            Format = scForm.SelectedSource.Item1.Extension,
                            Duration = Time.FromSeconds(scForm.Duration)
                        };

                        EnqueueDownload(downloader, scForm.SelectedSource, job);
```

Change `EnqueueDownload`'s signature and body from:

```csharp
        private void EnqueueDownload(Downloader downloader, (DataVideoSource video, DataAudioSource audio) sources, DownloadJobListBoxItem job)
        {
            flpJobs.Controls.Add(job);
            _downloadQueue.Enqueue(new PendingDownload { Downloader = downloader, Sources = sources, Job = job });
            TryStartQueuedDownloads();
        }
```

to:

```csharp
        private void EnqueueDownload(Downloader downloader, (DataVideoSource video, DataAudioSource audio) sources, DownloadJob job)
        {
            flpJobs.Controls.Add(new DownloadJobListBoxItem(job));
            _downloadQueue.Enqueue(new PendingDownload { Downloader = downloader, Sources = sources, Job = job });
            TryStartQueuedDownloads();
        }
```

Change the `PendingDownload` nested class's `Job` property type from `DownloadJobListBoxItem` to `DownloadJob`:

```csharp
        private sealed class PendingDownload
        {
            public required Downloader Downloader { get; init; }
            public required (DataVideoSource video, DataAudioSource audio) Sources { get; init; }
            public required DownloadJob Job { get; init; }
        }
```

Change the job-count LINQ helpers, which currently read `DownloadJobListBoxItem.State`, to read the wrapped model instead:

```csharp
        private DownloadJobListBoxItem[] DownloadJobListBoxItems => flpJobs.Controls.Cast<DownloadJobListBoxItem>().ToArray();
        protected int DownloadJobActiveCount => DownloadJobListBoxItems.Count(item => item.Job.State == DownloadingState.Downloading);
        protected int DownloadJobCompletedCount => DownloadJobListBoxItems.Count(item => item.Job.State == DownloadingState.Completed);
        protected int DownloadJobCount => DownloadJobListBoxItems.Length;
```

- [ ] **Step 5: Verify the solution builds**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds with no errors. (Compiler will flag any remaining `DownloadJobListBoxItem.DownloadingState`/`job.VideoTitle`-style references if any were missed — fix them the same way as above.)

- [ ] **Step 6: Manually verify one real download**

Run: `dotnet run --project VideoDownloader`
Paste a video URL, download it, confirm progress/speed/ETA update live and the file lands in the destination folder. Confirm Cancel still works mid-download.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Move Downloader into VideoDownloader.Core, retarget it at DownloadJob"
```

---

### Task 7: Add `DownloadQueueManager` to Core (new, standalone)

**Files:**
- Create: `VideoDownloader.Core/DownloadQueueManager.cs`

**Interfaces:**
- Consumes: `VideoDownloader.Core.Downloader` (Task 6), `VideoDownloader.Core.DownloadJob` (Task 4), `VideoDownloader.Core.DataVideoSource`/`DataAudioSource` (Task 3).
- Produces: `VideoDownloader.Core.DownloadQueueManager` with `ObservableCollection<DownloadJob> Jobs`, `int MaxConcurrentDownloads { get; set; }`, `void Enqueue(Downloader downloader, (DataVideoSource video, DataAudioSource audio) sources, DownloadJob job)`, `int ActiveCount`, `int CompletedCount`, `int TotalCount`, `event EventHandler? CountsChanged`.

This adds a file nothing references yet — cannot break the build.

- [ ] **Step 1: Write `DownloadQueueManager.cs`**

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

        public ObservableCollection<DownloadJob> Jobs { get; } = new();

        public event EventHandler? CountsChanged;

        public int MaxConcurrentDownloads
        {
            get => _maxConcurrentDownloads;
            set
            {
                if (_maxConcurrentDownloads == value) return;
                _maxConcurrentDownloads = value;
                TryStartQueuedDownloads();
            }
        }

        public int ActiveCount => Jobs.Count(j => j.State == DownloadingState.Downloading);
        public int CompletedCount => Jobs.Count(j => j.State == DownloadingState.Completed);
        public int TotalCount => Jobs.Count;

        public void Enqueue(Downloader downloader, (DataVideoSource video, DataAudioSource audio) sources, DownloadJob job)
        {
            Jobs.Add(job);
            _pending.Enqueue(new PendingDownload { Downloader = downloader, Sources = sources, Job = job });
            TryStartQueuedDownloads();
        }

        public void TryStartQueuedDownloads()
        {
            bool started = false;
            while (ActiveCount < MaxConcurrentDownloads && _pending.Count > 0)
            {
                var pending = _pending.Dequeue();
                _ = RunAsync(pending);
                started = true;
            }

            if (started)
                CountsChanged?.Invoke(this, EventArgs.Empty);
        }

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

- [ ] **Step 2: Verify the solution builds**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add VideoDownloader.Core/DownloadQueueManager.cs
git commit -m "Add DownloadQueueManager to VideoDownloader.Core"
```

---

### Task 8: Rewire `FrmMain` onto `DownloadQueueManager`

**Files:**
- Modify: `VideoDownloader/FrmMain.cs`

**Interfaces:**
- Consumes: `VideoDownloader.Core.DownloadQueueManager` (Task 7).

- [ ] **Step 1: Replace the queue fields and methods**

Read the current `VideoDownloader/FrmMain.cs` in full first (it's been modified in Tasks 5 and 6).

Remove:

```csharp
        private readonly Queue<PendingDownload> _downloadQueue = new();

        private int MaxConcurrentDownloads => tbMaxConcurrent.Value;
```

and the nested `PendingDownload` class, and remove these three methods entirely: `EnqueueDownload`, `TryStartQueuedDownloads`, `RunQueuedDownloadAsync`.

Add a field:

```csharp
        private readonly DownloadQueueManager _queue = new();
```

In the constructor, after `_Notifications = new Notifications(); ...`, add:

```csharp
            _queue.Jobs.CollectionChanged += Queue_JobsChanged;
            _queue.CountsChanged += (s, e) => DownloadJobCountChanged?.Invoke(this, EventArgs.Empty);
```

Add the handler (near `Notifications_CollectionChanged`):

```csharp
        private void Queue_JobsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => Queue_JobsChanged(sender, e));
                return;
            }

            if (e.NewItems != null)
            {
                foreach (DownloadJob job in e.NewItems)
                    flpJobs.Controls.Add(new DownloadJobListBoxItem(job));
            }
        }
```

Replace the two `EnqueueDownload(downloader, ..., job)` call sites (in `BatchDownload` and `btnDownload_Click`) with `_queue.Enqueue(downloader, ..., job)`.

Replace the job-count helpers:

```csharp
        private DownloadJobListBoxItem[] DownloadJobListBoxItems => flpJobs.Controls.Cast<DownloadJobListBoxItem>().ToArray();
        protected int DownloadJobActiveCount => DownloadJobListBoxItems.Count(item => item.Job.State == DownloadingState.Downloading);
        protected int DownloadJobCompletedCount => DownloadJobListBoxItems.Count(item => item.Job.State == DownloadingState.Completed);
        protected int DownloadJobCount => DownloadJobListBoxItems.Length;
```

with:

```csharp
        protected int DownloadJobActiveCount => _queue.ActiveCount;
        protected int DownloadJobCompletedCount => _queue.CompletedCount;
        protected int DownloadJobCount => _queue.TotalCount;
```

Update `tbMaxConcurrent_ValueChanged`:

```csharp
        private void tbMaxConcurrent_ValueChanged(object sender, EventArgs e)
        {
            UpdateMaxConcurrentLabel();
            Registry.SetMaxConcurrentDownloads(tbMaxConcurrent.Value);
            _queue.MaxConcurrentDownloads = tbMaxConcurrent.Value;
        }
```

And set the initial value once, in the constructor right after `tbMaxConcurrent.Value = ...`:

```csharp
            _queue.MaxConcurrentDownloads = tbMaxConcurrent.Value;
```

- [ ] **Step 2: Verify the solution builds**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

- [ ] **Step 3: Manually verify queueing behavior**

Run: `dotnet run --project VideoDownloader`
Queue 3+ URLs with the concurrency slider set to 1, confirm they run one at a time in order; raise the slider mid-queue and confirm more start immediately; confirm the status line (`Jobs: N | Active: N | Completed: N`) still updates correctly.

- [ ] **Step 4: Commit**

```bash
git add VideoDownloader/FrmMain.cs
git commit -m "Rewire FrmMain onto the shared DownloadQueueManager"
```

---

### Task 9: WinForms regression checkpoint

**Files:** none (verification-only task).

- [ ] **Step 1: Full build**

Run: `dotnet build VideoDownloader.sln -c Release`
Expected: Clean build, no warnings introduced by this migration's changes.

- [ ] **Step 2: Full manual pass of `VideoDownloader` (WinForms)**

Run: `dotnet run --project VideoDownloader --configuration Release`

Walk through every item in the spec's Verification plan §4 against the WinForms app specifically:
- Invalid URL shows an error notification.
- Cancel mid-download works and marks the job Canceled.
- Batch download via multi-line clipboard paste works (first URL prompts for quality, rest auto-match).
- Batch download via drag-and-drop of multiple URLs works.
- Batch download via "Load from file" (`.txt`) works.
- Switching language PL ⇄ EN updates all visible text immediately.
- Changing the concurrency slider mid-queue starts/holds jobs correctly.
- yt-dlp self-update check runs on load and reports via a notification.
- About dialog opens and shows correct version/build date/links.

- [ ] **Step 3: Report results**

If anything regressed from pre-migration behavior, fix it now (this is still Part 1 — the WPF work in Part 2 assumes a known-good `Core`). Do not proceed to Part 2 until this checklist is clean.

- [ ] **Step 4: Commit** (only if fixes were needed)

```bash
git add -A
git commit -m "Fix WinForms regression found during Core-migration checkpoint"
```

---

## Part 2 — WPF app

### Task 10: Scaffold `VideoDownloader.Wpf`, packaging, styling, localization plumbing

**Files:**
- Create: `VideoDownloader.Wpf/VideoDownloader.Wpf.csproj`
- Create: `VideoDownloader.Wpf/App.xaml`, `VideoDownloader.Wpf/App.xaml.cs`
- Create: `VideoDownloader.Wpf/MainWindow.xaml`, `VideoDownloader.Wpf/MainWindow.xaml.cs`
- Create: `VideoDownloader.Wpf/Localization/TrExtension.cs`
- Create: `VideoDownloader.Wpf/Themes/Colors.xaml`
- Create: `VideoDownloader.Wpf/Themes/Styles.xaml`
- Modify: `VideoDownloader.sln`

**Interfaces:**
- Produces: `VideoDownloader.Wpf.Localization.TrExtension` (XAML markup extension `{loc:Tr 'English key'}`), app-wide styles under `{StaticResource ...}` keys `AccentBrush`, `PrimaryButtonStyle`.

- [ ] **Step 1: Create the project**

```bash
mkdir VideoDownloader.Wpf
```

Write `VideoDownloader.Wpf/VideoDownloader.Wpf.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationIcon>..\VideoDownloader\Resources\VideoDownloader.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\VideoDownloader.Core\VideoDownloader.Core.csproj" />
  </ItemGroup>

  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="xcopy &quot;$(SolutionDir)ExternalLib\*.exe&quot; &quot;$(TargetDir)&quot; /Y /I" />
  </Target>

</Project>
```

- [ ] **Step 2: Add to the solution**

```bash
dotnet sln VideoDownloader.sln add VideoDownloader.Wpf/VideoDownloader.Wpf.csproj
```

- [ ] **Step 3: Write the `Tr` markup extension**

```csharp
using System.Windows.Markup;
using VideoDownloader.Core;

namespace VideoDownloader.Wpf.Localization
{
    /// <summary>
    /// XAML markup extension over the shared Core.Localization dictionary: {loc:Tr 'English key'}.
    /// Re-evaluates every bound usage when Localization.LanguageChanged fires.
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class TrExtension : MarkupExtension
    {
        public string Key { get; set; }

        public TrExtension() { Key = string.Empty; }
        public TrExtension(string key) { Key = key; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget))!;
            if (target.TargetObject is not System.Windows.DependencyObject targetElement)
                return Localization.T(Key);

            if (target.TargetProperty is System.Windows.DependencyProperty dp)
            {
                void Refresh(object? s, EventArgs e) =>
                    targetElement.Dispatcher.Invoke(() => targetElement.SetValue(dp, Localization.T(Key)));

                Localization.LanguageChanged += Refresh;
            }

            return Localization.T(Key);
        }
    }
}
```

- [ ] **Step 4: Write the theme resource dictionaries**

`VideoDownloader.Wpf/Themes/Colors.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Color x:Key="AccentColor">#2F6FED</Color>
    <Color x:Key="BackgroundColor">#FAFAFA</Color>
    <Color x:Key="CardColor">#FFFFFF</Color>
    <Color x:Key="BorderColor">#E2E2E2</Color>

    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}" />
    <SolidColorBrush x:Key="CardBrush" Color="{StaticResource CardColor}" />
    <SolidColorBrush x:Key="BorderBrush2" Color="{StaticResource BorderColor}" />
</ResourceDictionary>
```

`VideoDownloader.Wpf/Themes/Styles.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="Colors.xaml" />
    </ResourceDictionary.MergedDictionaries>

    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource AccentBrush}" />
        <Setter Property="Foreground" Value="White" />
        <Setter Property="Padding" Value="14,6" />
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}" CornerRadius="4" Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType="Border" x:Key="CardStyle">
        <Setter Property="Background" Value="{StaticResource CardBrush}" />
        <Setter Property="BorderBrush" Value="{StaticResource BorderBrush2}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="CornerRadius" Value="6" />
        <Setter Property="Padding" Value="10" />
        <Setter Property="Margin" Value="0,0,0,6" />
    </Style>

</ResourceDictionary>
```

- [ ] **Step 5: Write `App.xaml` / `App.xaml.cs`**

`VideoDownloader.Wpf/App.xaml`:

```xml
<Application x:Class="VideoDownloader.Wpf.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Themes/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

`VideoDownloader.Wpf/App.xaml.cs`:

```csharp
using System.Windows;

namespace VideoDownloader.Wpf
{
    public partial class App : Application
    {
    }
}
```

- [ ] **Step 6: Write a minimal `MainWindow` that proves the plumbing**

`VideoDownloader.Wpf/MainWindow.xaml`:

```xml
<Window x:Class="VideoDownloader.Wpf.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:loc="clr-namespace:VideoDownloader.Wpf.Localization"
        Title="VideoDownloader" Height="600" Width="900"
        Background="{StaticResource BackgroundBrush}">
    <Grid Margin="16">
        <Button Content="{loc:Tr 'Download'}" Style="{StaticResource PrimaryButtonStyle}"
                HorizontalAlignment="Left" VerticalAlignment="Top" />
    </Grid>
</Window>
```

`VideoDownloader.Wpf/MainWindow.xaml.cs`:

```csharp
using System.Windows;

namespace VideoDownloader.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 7: Verify it builds and runs**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds (4 projects now: VideoDownloader, VideoDownloader.Core, VideoDownloader.Wpf, FFmpegBuild excluded).

Run: `dotnet run --project VideoDownloader.Wpf`
Expected: A window opens showing a styled blue rounded button reading "Pobierz" (if the OS UI culture is Polish) or "Download" otherwise, and `ExternalLib`'s `yt-dlp.exe`/`ffmpeg.exe` are present in `VideoDownloader.Wpf/bin/Debug/net8.0-windows/`.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Scaffold VideoDownloader.Wpf project with styling and localization plumbing"
```

---

### Task 11: `IDialogService` + About window (first vertical slice)

**Files:**
- Create: `VideoDownloader.Wpf/Services/IDialogService.cs`
- Create: `VideoDownloader.Wpf/Services/DialogService.cs`
- Create: `VideoDownloader.Wpf/ViewModels/AboutViewModel.cs`
- Create: `VideoDownloader.Wpf/Views/AboutWindow.xaml`, `AboutWindow.xaml.cs`
- Modify: `VideoDownloader.Wpf/MainWindow.xaml`, `MainWindow.xaml.cs`

**Interfaces:**
- Produces: `VideoDownloader.Wpf.Services.IDialogService` with `void ShowAbout()`; `AboutViewModel` with `string Version`, `string BuildDate`, `string Author`, `ICommand OpenEmailCommand`, `ICommand OpenFFmpegCommand`, `ICommand OpenYtDlpCommand`.

- [ ] **Step 1: Add Polish strings needed for this task**

In `VideoDownloader.Core/Localization.cs`, these keys already exist in the `_pl` dictionary (`About VideoDownloader`, `Version:`, `Build date:`, `Author:`, `Uses the following tools:`, `Close`) — no changes needed here, they carry over unchanged from the WinForms app.

- [ ] **Step 2: Write `IDialogService` / `DialogService`**

```csharp
namespace VideoDownloader.Wpf.Services
{
    public interface IDialogService
    {
        void ShowAbout();
    }
}
```

```csharp
using VideoDownloader.Wpf.ViewModels;
using VideoDownloader.Wpf.Views;

namespace VideoDownloader.Wpf.Services
{
    public class DialogService : IDialogService
    {
        public void ShowAbout()
        {
            var window = new AboutWindow { DataContext = new AboutViewModel() };
            window.ShowDialog();
        }
    }
}
```

- [ ] **Step 3: Write `AboutViewModel`**

```csharp
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VideoDownloader.Wpf.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public string Version => $"{Core.Localization.T("Version:")} {GetVersion()}";
        public string BuildDate => $"{Core.Localization.T("Build date:")} {GetBuildDate():yyyy-MM-dd HH:mm}";
        public string Author => $"{Core.Localization.T("Author:")} Adam Skowroński";

        private static string GetVersion() =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        private static DateTime GetBuildDate() =>
            File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);

        [RelayCommand]
        private void OpenEmail() => OpenLink("mailto:info@appit.pl");

        [RelayCommand]
        private void OpenFFmpeg() => OpenLink("https://ffmpeg.org");

        [RelayCommand]
        private void OpenYtDlp() => OpenLink("https://github.com/yt-dlp/yt-dlp");

        private static void OpenLink(string url) =>
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
}
```

- [ ] **Step 4: Write `AboutWindow`**

`VideoDownloader.Wpf/Views/AboutWindow.xaml`:

```xml
<Window x:Class="VideoDownloader.Wpf.Views.AboutWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:loc="clr-namespace:VideoDownloader.Wpf.Localization"
        Title="{loc:Tr 'About VideoDownloader'}" Height="260" Width="380"
        WindowStartupLocation="CenterOwner" ResizeMode="NoResize"
        Background="{StaticResource BackgroundBrush}">
    <StackPanel Margin="16">
        <TextBlock Text="{Binding Version}" Margin="0,0,0,4" />
        <TextBlock Text="{Binding BuildDate}" Margin="0,0,0,4" />
        <TextBlock Text="{Binding Author}" Margin="0,0,0,12" />
        <TextBlock Text="{loc:Tr 'Uses the following tools:'}" Margin="0,0,0,4" />
        <TextBlock>
            <Hyperlink Command="{Binding OpenYtDlpCommand}">yt-dlp</Hyperlink>
        </TextBlock>
        <TextBlock>
            <Hyperlink Command="{Binding OpenFFmpegCommand}">FFmpeg</Hyperlink>
        </TextBlock>
        <Button Content="{loc:Tr 'Close'}" Style="{StaticResource PrimaryButtonStyle}"
                HorizontalAlignment="Right" Margin="0,16,0,0" Click="Close_Click" />
    </StackPanel>
</Window>
```

`VideoDownloader.Wpf/Views/AboutWindow.xaml.cs`:

```csharp
using System.Windows;

namespace VideoDownloader.Wpf.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
```

- [ ] **Step 5: Wire a temporary trigger in `MainWindow` to prove the slice end-to-end**

In `MainWindow.xaml`, replace the `Button` with:

```xml
        <Button Content="{loc:Tr 'ℹ️ About'}" Style="{StaticResource PrimaryButtonStyle}"
                HorizontalAlignment="Left" VerticalAlignment="Top" Click="About_Click" />
```

In `MainWindow.xaml.cs`, add:

```csharp
using VideoDownloader.Wpf.Services;

namespace VideoDownloader.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly IDialogService _dialogService = new DialogService();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void About_Click(object sender, RoutedEventArgs e) => _dialogService.ShowAbout();
    }
}
```

(Task 12 replaces this temporary click handler with the real `MainViewModel`-driven command.)

- [ ] **Step 6: Verify it runs**

Run: `dotnet run --project VideoDownloader.Wpf`
Click the About button, confirm the About window opens with correct version/build date/author and the yt-dlp/FFmpeg links open a browser, then Close works.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Add IDialogService and About window vertical slice"
```

---

### Task 12: `MainViewModel` + `MainWindow` layout (queue, notifications, settings)

**Files:**
- Create: `VideoDownloader.Wpf/ViewModels/MainViewModel.cs`
- Create: `VideoDownloader.Wpf/Templates/JobCardTemplate.xaml` (DataTemplate for `DownloadJob`)
- Create: `VideoDownloader.Wpf/Templates/NotificationTemplate.xaml` (DataTemplate for `NotificationItem`)
- Modify: `VideoDownloader.Wpf/App.xaml` (merge the new template dictionaries)
- Rewrite: `VideoDownloader.Wpf/MainWindow.xaml`, `MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `DownloadQueueManager`, `Notifications`, `Registry`, `Localization`, `AppLanguage` (all `Core`, existing).
- Produces: `MainViewModel` with `string SourceUrl`, `string DestinationPath`, `int MaxConcurrentDownloads`, `ObservableCollection<DownloadJob> Jobs`, `ObservableCollection<NotificationItem> Notifications`, `string StatusText`, `AppLanguage Language`, commands `BrowseCommand`, `OpenFolderCommand`, `AboutCommand`, `BuyCoffeeCommand`, `CancelJobCommand` (`RelayCommand<DownloadJob>` — mirrors WinForms' `DownloadJobListBoxItem.btnCancel_Click`, since `DownloadJob` itself must stay a plain Core model with no WPF `ICommand` on it). (`DownloadCommand`, `LoadFileCommand` are added in Tasks 15–16, once the source-chooser flow exists to back them.)

- [ ] **Step 1: Write `MainViewModel`**

```csharp
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoDownloader.Core;
using VideoDownloader.Core.Windows;
using VideoDownloader.Wpf.Services;

namespace VideoDownloader.Wpf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly DownloadQueueManager _queue = new();
        private readonly Notifications _notifications = new();

        [ObservableProperty]
        private string _sourceUrl = string.Empty;

        [ObservableProperty]
        private string _destinationPath = string.Empty;

        [ObservableProperty]
        private int _maxConcurrentDownloads = 3;

        [ObservableProperty]
        private string _statusText = string.Empty;

        public ObservableCollection<DownloadJob> Jobs => _queue.Jobs;
        public ObservableCollection<NotificationItem> Notifications => _notifications.Items;

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            DestinationPath = Registry.GetLastDestinationPath();
            MaxConcurrentDownloads = Math.Clamp(Registry.GetMaxConcurrentDownloads(), 1, 20);
            _queue.MaxConcurrentDownloads = MaxConcurrentDownloads;

            _queue.CountsChanged += (s, e) => Application.Current.Dispatcher.Invoke(UpdateStatusText);
            UpdateStatusText();
        }

        partial void OnMaxConcurrentDownloadsChanged(int value)
        {
            Registry.SetMaxConcurrentDownloads(value);
            _queue.MaxConcurrentDownloads = value;
        }

        private void UpdateStatusText()
        {
            StatusText = _queue.TotalCount > 0
                ? string.Format(Core.Localization.T("Jobs: {0} | Active: {1} | Completed: {2}"), _queue.TotalCount, _queue.ActiveCount, _queue.CompletedCount)
                : string.Format(Core.Localization.T("Jobs: {0} | Completed: {0}"), _queue.TotalCount);
        }

        [RelayCommand]
        private void Browse()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Destination Folder",
                AutoUpgradeEnabled = true,
                ShowNewFolderButton = true,
                SelectedPath = Directory.Exists(DestinationPath) ? DestinationPath : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DestinationPath = dialog.SelectedPath;
                Registry.SetLastDestinationPath(DestinationPath);
            }
        }

        [RelayCommand]
        private void OpenFolder()
        {
            if (DestinationPath.Length > 0 && Path.Exists(DestinationPath))
            {
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{DestinationPath}\"", UseShellExecute = true });
            }
        }

        [RelayCommand]
        private void About() => _dialogService.ShowAbout();

        [RelayCommand]
        private void BuyCoffee() =>
            Process.Start(new ProcessStartInfo { FileName = "https://buycoffee.to/rico", UseShellExecute = true });

        [RelayCommand]
        private void CancelJob(DownloadJob? job)
        {
            if (job == null) return;

            if (job.State == DownloadingState.Downloading &&
                MessageBox.Show(Core.Localization.T("Are you sure you want to cancel downloading?"), Core.Localization.T("Downloading"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                job.CancellationTokenSource?.Cancel();
                job.State = DownloadingState.Canceled;
            }
        }

        internal DownloadQueueManager Queue => _queue;
        internal Notifications NotificationsService => _notifications;
    }
}
```

`CancelJob` uses `System.Windows.MessageBox` — add `using System.Windows;` to the top of the file.

> `FolderBrowserDialog` is a WinForms type but has no WinForms *UI* dependency issue here — it's a common, supported pattern for WPF apps that want the classic folder picker without adding `Microsoft.Win32.OpenFolderDialog`'s Windows-version constraints. Add `<UseWindowsForms>true</UseWindowsForms>` to `VideoDownloader.Wpf.csproj`'s `PropertyGroup` for this to compile.

- [ ] **Step 2: Enable WinForms interop in the csproj**

In `VideoDownloader.Wpf/VideoDownloader.Wpf.csproj`, add to the `PropertyGroup`:

```xml
    <UseWindowsForms>true</UseWindowsForms>
```

- [ ] **Step 3: Write the `JobCard` DataTemplate**

`VideoDownloader.Wpf/Templates/JobCardTemplate.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     xmlns:core="clr-namespace:VideoDownloader.Core;assembly=VideoDownloader.Core"
                     xmlns:loc="clr-namespace:VideoDownloader.Wpf.Localization">

    <DataTemplate DataType="{x:Type core:DownloadJob}">
        <Border Style="{StaticResource CardStyle}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0">
                    <TextBlock Text="{Binding Title}" FontWeight="Bold" TextTrimming="CharacterEllipsis" />
                    <TextBlock Text="{Binding Url}" Foreground="Gray" FontSize="11" TextTrimming="CharacterEllipsis" />
                    <Grid Height="28" Margin="0,6,0,0">
                        <ProgressBar Minimum="0" Maximum="100" Value="{Binding ProgressPercentage, Mode=OneWay}" />
                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" FontSize="11">
                            <Run Text="{Binding ProgressPercentage, StringFormat={}{0:F1}%, Mode=OneWay}" />
                            <Run Text=" " />
                            <Run Text="{Binding Speed, Mode=OneWay}" />
                            <Run Text=" " />
                            <Run Text="{Binding ETA, Mode=OneWay}" />
                        </TextBlock>
                    </Grid>
                </StackPanel>
                <Button Grid.Column="1" Content="✕" Margin="8,0,0,0" VerticalAlignment="Top"
                        Command="{Binding DataContext.CancelJobCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                        CommandParameter="{Binding}">
                    <Button.Style>
                        <Style TargetType="Button">
                            <Setter Property="Visibility" Value="Visible" />
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding State}" Value="Downloading">
                                    <Setter Property="Visibility" Value="Visible" />
                                </DataTrigger>
                                <DataTrigger Binding="{Binding State}" Value="Waiting">
                                    <Setter Property="Visibility" Value="Visible" />
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Button.Style>
                </Button>
            </Grid>
        </Border>
    </DataTemplate>

</ResourceDictionary>
```

The `RelativeSource={RelativeSource AncestorType=Window}` walk works because `MainWindow`'s `DataContext` is the `MainViewModel` (set in Step 6) and the job list `ItemsControl` lives inside that same `Window` — so `DataContext.CancelJobCommand` resolves to `MainViewModel.CancelJobCommand`, while `CommandParameter="{Binding}"` stays bound to the `DownloadJob` this template instance renders.

- [ ] **Step 4: Write the `NotificationBanner` DataTemplate**

`VideoDownloader.Wpf/Templates/NotificationTemplate.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     xmlns:core="clr-namespace:VideoDownloader.Core;assembly=VideoDownloader.Core">

    <DataTemplate DataType="{x:Type core:NotificationItem}">
        <Border CornerRadius="4" Padding="10,6" Margin="0,0,0,4">
            <Border.Style>
                <Style TargetType="Border">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding Type}" Value="Info">
                            <Setter Property="Background" Value="LightBlue" />
                        </DataTrigger>
                        <DataTrigger Binding="{Binding Type}" Value="Warning">
                            <Setter Property="Background" Value="Khaki" />
                        </DataTrigger>
                        <DataTrigger Binding="{Binding Type}" Value="Error">
                            <Setter Property="Background" Value="LightCoral" />
                        </DataTrigger>
                        <DataTrigger Binding="{Binding Type}" Value="Success">
                            <Setter Property="Background" Value="LightGreen" />
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Border.Style>
            <TextBlock Text="{Binding Message}" TextWrapping="Wrap" />
        </Border>
    </DataTemplate>

</ResourceDictionary>
```

- [ ] **Step 5: Merge the templates in `App.xaml`**

```xml
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Themes/Styles.xaml" />
                <ResourceDictionary Source="Templates/JobCardTemplate.xaml" />
                <ResourceDictionary Source="Templates/NotificationTemplate.xaml" />
            </ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 6: Rewrite `MainWindow`**

`VideoDownloader.Wpf/MainWindow.xaml`:

```xml
<Window x:Class="VideoDownloader.Wpf.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:loc="clr-namespace:VideoDownloader.Wpf.Localization"
        Title="VideoDownloader" Height="650" Width="950" MinWidth="600"
        Background="{StaticResource BackgroundBrush}"
        Activated="Window_Activated">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,8">
            <TextBlock Text="{loc:Tr 'Source (link)'}" VerticalAlignment="Center" Width="140" />
            <TextBox Text="{Binding SourceUrl, UpdateSourceTrigger=PropertyChanged}" Width="500" />
            <Button Content="{loc:Tr 'Download'}" Style="{StaticResource PrimaryButtonStyle}" Margin="8,0,0,0"
                    Command="{Binding DownloadCommand}" />
        </StackPanel>

        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,8">
            <TextBlock Text="{loc:Tr 'Destination (path)'}" VerticalAlignment="Center" Width="140" />
            <TextBox Text="{Binding DestinationPath}" Width="440" IsReadOnly="True" />
            <Button Content="{loc:Tr 'Browse'}" Margin="8,0,0,0" Command="{Binding BrowseCommand}" />
            <Button Content="📂" Margin="8,0,0,0" Command="{Binding OpenFolderCommand}" />
        </StackPanel>

        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,8">
            <TextBlock Text="{Binding MaxConcurrentDownloads, StringFormat='Simultaneous downloads: {0}'}" VerticalAlignment="Center" Width="200" />
            <Slider Minimum="1" Maximum="20" Value="{Binding MaxConcurrentDownloads}" Width="200" TickFrequency="1" IsSnapToTickEnabled="True" />
            <Button Content="{loc:Tr 'ℹ️ About'}" Margin="16,0,0,0" Command="{Binding AboutCommand}" />
            <Button Content="{loc:Tr '☕ Buy me a coffee'}" Margin="8,0,0,0" Command="{Binding BuyCoffeeCommand}" />
        </StackPanel>

        <ItemsControl Grid.Row="3" ItemsSource="{Binding Notifications}" Margin="0,0,0,8" />

        <ScrollViewer Grid.Row="4" VerticalScrollBarVisibility="Auto"
                      AllowDrop="True" Drop="JobList_Drop" DragEnter="JobList_DragEnter">
            <ItemsControl ItemsSource="{Binding Jobs}" />
        </ScrollViewer>

        <TextBlock Grid.Row="5" Text="{Binding StatusText}" Margin="0,8,0,0" />
    </Grid>
</Window>
```

`VideoDownloader.Wpf/MainWindow.xaml.cs`:

```csharp
using System.Windows;
using VideoDownloader.Wpf.Services;
using VideoDownloader.Wpf.ViewModels;

namespace VideoDownloader.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel(new DialogService());
            DataContext = _viewModel;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            // Clipboard-paste-on-activate is wired in Task 16.
        }

        private void JobList_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.Text) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void JobList_Drop(object sender, DragEventArgs e)
        {
            // Multi-URL drag-drop is wired in Task 16.
        }
    }
}
```

`DownloadCommand` doesn't exist on `MainViewModel` yet — this is expected; it's added in Task 15. The binding will simply be inert (button does nothing) until then. Note that for XAML binding errors to not break the build (they're runtime warnings, not compile errors), this is safe to leave as-is between tasks.

- [ ] **Step 7: Verify it builds and runs**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

Run: `dotnet run --project VideoDownloader.Wpf`
Expected: Window shows URL/destination fields, Browse opens a folder picker and persists the choice (confirm via `VideoDownloader/Core/Windows/Registry.cs`'s registry key, or by restarting the app and seeing the path retained), the concurrency slider label updates live, About opens correctly, Buy-coffee opens a browser. (The job list and its Cancel button have nothing to render yet — no download flow exists until Task 14 — so this is a layout/wiring check only, not a functional one.)

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add MainViewModel and MainWindow layout with job/notification templates"
```

---

### Task 13: `SourceChooserViewModel` + `SourceChooserWindow`

**Files:**
- Create: `VideoDownloader.Wpf/ViewModels/SourceChooserViewModel.cs`
- Create: `VideoDownloader.Wpf/Views/SourceChooserWindow.xaml`, `SourceChooserWindow.xaml.cs`
- Modify: `VideoDownloader.Wpf/Services/IDialogService.cs`, `DialogService.cs`

**Interfaces:**
- Consumes: `Downloader.FetchSources()`, `DataVideoSource`, `DataAudioSource` (all `Core`, existing, unchanged since Task 6).
- Produces: `IDialogService.ShowSourceChooser(Downloader downloader, out string videoTitle, out float duration)` returning `(DataVideoSource, DataAudioSource)?` (`null` = cancelled).

- [ ] **Step 1: Write `SourceChooserViewModel`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoDownloader.Core;

namespace VideoDownloader.Wpf.ViewModels
{
    public partial class SourceChooserViewModel : ObservableObject
    {
        private readonly Downloader _downloader;

        public ObservableCollection<DataVideoSource> VideoSources { get; } = new();
        public ObservableCollection<DataAudioSource> AudioSources { get; } = new();

        [ObservableProperty]
        private DataVideoSource? _selectedVideo;

        [ObservableProperty]
        private DataAudioSource? _selectedAudio;

        [ObservableProperty]
        private bool _canDownload;

        public string VideoTitle { get; private set; } = string.Empty;
        public float Duration { get; private set; }

        public bool? DialogResult { get; private set; }

        public event EventHandler? RequestClose;

        public SourceChooserViewModel(Downloader downloader)
        {
            _downloader = downloader;
        }

        public async Task LoadAsync()
        {
            var sources = await _downloader.FetchSources();
            if (sources == null)
                throw new Exception(Localization.T("Failed to retrieve sources."));

            VideoTitle = sources.VideoTitle;
            Duration = sources.Duration;

            foreach (var v in sources.VideoSources) VideoSources.Add(v);
            foreach (var a in sources.AudioSources) AudioSources.Add(a);

            SelectedVideo = VideoSources.LastOrDefault();
            SelectedAudio = AudioSources.FirstOrDefault();
            CanDownload = VideoSources.Count > 0 && AudioSources.Count > 0;
        }

        [RelayCommand]
        private void Download()
        {
            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
```

- [ ] **Step 2: Write `SourceChooserWindow`**

`VideoDownloader.Wpf/Views/SourceChooserWindow.xaml`:

```xml
<Window x:Class="VideoDownloader.Wpf.Views.SourceChooserWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:loc="clr-namespace:VideoDownloader.Wpf.Localization"
        Title="{loc:Tr 'Select sources'}" Height="220" Width="480"
        WindowStartupLocation="CenterOwner" ResizeMode="NoResize"
        Background="{StaticResource BackgroundBrush}">
    <StackPanel Margin="16">
        <TextBlock Text="{loc:Tr 'Video:'}" Margin="0,0,0,4" />
        <ComboBox ItemsSource="{Binding VideoSources}" SelectedItem="{Binding SelectedVideo}" Margin="0,0,0,12" />

        <TextBlock Text="{loc:Tr 'Audio:'}" Margin="0,0,0,4" />
        <ComboBox ItemsSource="{Binding AudioSources}" SelectedItem="{Binding SelectedAudio}" Margin="0,0,0,12" />

        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="{loc:Tr 'Cancel'}" Command="{Binding CancelCommand}" Margin="0,0,8,0" />
            <Button Content="{loc:Tr 'Download'}" Style="{StaticResource PrimaryButtonStyle}"
                    Command="{Binding DownloadCommand}" IsEnabled="{Binding CanDownload}" />
        </StackPanel>
    </StackPanel>
</Window>
```

`VideoDownloader.Wpf/Views/SourceChooserWindow.xaml.cs`:

```csharp
using System.Windows;
using VideoDownloader.Wpf.ViewModels;

namespace VideoDownloader.Wpf.Views
{
    public partial class SourceChooserWindow : Window
    {
        public SourceChooserWindow(SourceChooserViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += (s, e) =>
            {
                DialogResult = viewModel.DialogResult;
                Close();
            };
        }
    }
}
```

- [ ] **Step 3: Extend `IDialogService`**

```csharp
using VideoDownloader.Core;

namespace VideoDownloader.Wpf.Services
{
    public interface IDialogService
    {
        void ShowAbout();

        /// <summary>Shows the source chooser; returns null if the user cancelled.</summary>
        Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ShowSourceChooser(Downloader downloader);
    }
}
```

```csharp
using VideoDownloader.Core;
using VideoDownloader.Wpf.ViewModels;
using VideoDownloader.Wpf.Views;

namespace VideoDownloader.Wpf.Services
{
    public class DialogService : IDialogService
    {
        public void ShowAbout()
        {
            var window = new AboutWindow { DataContext = new AboutViewModel() };
            window.ShowDialog();
        }

        public async Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ShowSourceChooser(Downloader downloader)
        {
            var viewModel = new SourceChooserViewModel(downloader);
            await viewModel.LoadAsync();

            var window = new SourceChooserWindow(viewModel);
            bool? result = window.ShowDialog();

            if (result != true || viewModel.SelectedVideo == null || viewModel.SelectedAudio == null)
                return null;

            return (viewModel.SelectedVideo, viewModel.SelectedAudio, viewModel.VideoTitle, viewModel.Duration);
        }
    }
}
```

- [ ] **Step 4: Verify it builds**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds. (`ShowSourceChooser` isn't called from `MainViewModel` yet — that's Task 15.)

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add SourceChooserViewModel and SourceChooserWindow"
```

---

### Task 14: Wire the single-URL download flow end-to-end

**Files:**
- Modify: `VideoDownloader.Wpf/ViewModels/MainViewModel.cs`

**Interfaces:**
- Consumes: `IDialogService.ShowSourceChooser` (Task 13), `DownloadQueueManager.Enqueue` (Core, existing), `Downloader` (Core, existing), `Helper.URL.Verify` (Core, existing).
- Produces: `MainViewModel.DownloadCommand` (`AsyncRelayCommand`).

- [ ] **Step 1: Add the `Download` command**

In `MainViewModel.cs`, add `using VideoDownloader.Core.Tools;` and this method:

```csharp
        [RelayCommand]
        private async Task Download()
        {
            _notifications.ClearOldNotifications();

            if (DestinationPath.Length == 0 || !Directory.Exists(DestinationPath))
            {
                _notifications.Warning(Core.Localization.T("Please select a valid destination path before downloading."));
                return;
            }

            var downloader = new Downloader(_notifications)
            {
                SourceURL = SourceUrl,
                DestinationPath = DestinationPath
            };

            try
            {
                var picked = await _dialogService.ShowSourceChooser(downloader);
                if (picked == null)
                {
                    downloader.Dispose();
                    return;
                }

                var job = new DownloadJob
                {
                    Title = picked.Value.videoTitle,
                    Url = downloader.SourceURL,
                    Resolution = picked.Value.video.Resolution,
                    Format = picked.Value.video.Extension,
                    Duration = Time.FromSeconds(picked.Value.duration)
                };

                _queue.Enqueue(downloader, (picked.Value.video, picked.Value.audio), job);
                SourceUrl = string.Empty;
            }
            catch (Exception ex)
            {
                _notifications.Error(Core.Errors.ParseErrorMessage(ex));
                downloader.Dispose();
            }
        }
```

- [ ] **Step 2: Verify it builds and runs a real download**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

Run: `dotnet run --project VideoDownloader.Wpf`
Paste a video URL, click Download, pick a video/audio combination in the source chooser, confirm the job card appears in the list and progress/speed/ETA update live, and the file lands in the destination folder when done. Start a second download and click its ✕ button mid-download to confirm the cancel confirmation dialog appears and cancelling marks the job Canceled.

- [ ] **Step 3: Commit**

```bash
git add VideoDownloader.Wpf/ViewModels/MainViewModel.cs
git commit -m "Wire the single-URL download flow in the WPF app"
```

---

### Task 15: Batch flow (load-from-file, drag-drop, clipboard paste, auto-pick)

**Files:**
- Modify: `VideoDownloader.Wpf/ViewModels/MainViewModel.cs`
- Modify: `VideoDownloader.Wpf/Services/IDialogService.cs`, `DialogService.cs` (add a non-interactive `ChooseBestQuality` path)
- Modify: `VideoDownloader.Wpf/MainWindow.xaml.cs` (drag-drop + activate handlers)

**Interfaces:**
- Consumes: `MainViewModel.Download()`'s pieces (Task 14), `Helper.URL.Verify` (Core, existing).
- Produces: `MainViewModel.LoadFileCommand`; `MainViewModel.LoadMultipleAsync(IEnumerable<string> lines)`.

- [ ] **Step 1: Extract a batch-capable download method mirroring `FrmMain.BatchDownload`**

In `MainViewModel.cs`, replace the `Download()` method's body with a shared helper it now calls, and add batch support:

```csharp
        [RelayCommand]
        private async Task Download() => await DownloadOne(interactive: true, preferred: null);

        private async Task<(DataVideoSource video, DataAudioSource audio)?> DownloadOne(bool interactive, (DataVideoSource video, DataAudioSource audio)? preferred)
        {
            _notifications.ClearOldNotifications();

            if (DestinationPath.Length == 0 || !Directory.Exists(DestinationPath))
            {
                _notifications.Warning(Core.Localization.T("Please select a valid destination path before downloading."));
                return null;
            }

            var downloader = new Downloader(_notifications)
            {
                SourceURL = SourceUrl,
                DestinationPath = DestinationPath
            };

            try
            {
                (DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)? picked = interactive
                    ? await _dialogService.ShowSourceChooser(downloader)
                    : await _dialogService.ChooseBestQuality(downloader, preferred);

                if (picked == null)
                {
                    downloader.Dispose();
                    return null;
                }

                var job = new DownloadJob
                {
                    Title = picked.Value.videoTitle,
                    Url = downloader.SourceURL,
                    Resolution = picked.Value.video.Resolution,
                    Format = picked.Value.video.Extension,
                    Duration = Time.FromSeconds(picked.Value.duration)
                };

                _queue.Enqueue(downloader, (picked.Value.video, picked.Value.audio), job);
                SourceUrl = string.Empty;

                return (picked.Value.video, picked.Value.audio);
            }
            catch (Exception ex)
            {
                _notifications.Error(Core.Errors.ParseErrorMessage(ex));
                downloader.Dispose();
                SourceUrl = string.Empty;
                return null;
            }
        }

        public async Task LoadMultipleAsync(IReadOnlyList<string> lines)
        {
            if (lines.Count < 2 || MessageBox.Show(
                string.Format(Core.Localization.T("Found {0} urls! Are you sure you want to download them all?"), lines.Count),
                Core.Localization.T("Download"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                (DataVideoSource video, DataAudioSource audio)? preferred = null;
                bool isFirst = true;

                foreach (var line in lines)
                {
                    SourceUrl = line;
                    var result = await DownloadOne(isFirst, preferred);

                    if (isFirst)
                    {
                        if (result == null) break;
                        preferred = result;
                        isFirst = false;
                    }
                }
            }
        }

        [RelayCommand]
        private async Task LoadFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Text file (*.txt)|*.txt", CheckFileExists = true, Multiselect = false };
            if (dialog.ShowDialog() == true)
            {
                await LoadMultipleAsync(File.ReadAllLines(dialog.FileName));
            }
        }

        public async Task PasteClipboardUrlAsync()
        {
            string clipboardText = Clipboard.GetText();
            if (SourceUrl.Length == 0 && !string.IsNullOrEmpty(clipboardText))
            {
                if (Helper.URL.Verify(clipboardText))
                {
                    SourceUrl = clipboardText;
                }
                else if (clipboardText.Split('\n').Length > 1)
                {
                    var lines = clipboardText.Split('\n').Where(Helper.URL.Verify).ToArray();
                    await LoadMultipleAsync(lines);
                }
            }
        }
```

Add `using System.Windows;` (for `MessageBox`/`Clipboard`) and `using VideoDownloader.Core.Tools;` at the top of the file if not already present from Task 14.

- [ ] **Step 2: Add `ChooseBestQuality` to the dialog service**

`IDialogService.cs`, add:

```csharp
        /// <summary>Auto-picks best quality, matching <paramref name="preferred"/> if given, without showing UI.</summary>
        Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ChooseBestQuality(Downloader downloader, (DataVideoSource video, DataAudioSource audio)? preferred);
```

`DialogService.cs`, add:

```csharp
        public async Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ChooseBestQuality(Downloader downloader, (DataVideoSource video, DataAudioSource audio)? preferred)
        {
            var sources = await downloader.FetchSources();
            if (sources == null || sources.VideoSources.Count == 0 || sources.AudioSources.Count == 0)
                throw new Exception(Core.Localization.T("No downloadable video/audio formats found."));

            var video = MatchVideo(sources.VideoSources, preferred?.video) ?? sources.VideoSources.LastOrDefault();
            var audio = MatchAudio(sources.AudioSources, preferred?.audio) ?? sources.AudioSources.FirstOrDefault();

            if (video == null || audio == null)
                return null;

            return (video, audio, sources.VideoTitle, sources.Duration);
        }

        private static DataVideoSource? MatchVideo(List<DataVideoSource> sources, DataVideoSource? preferred)
        {
            if (preferred == null) return null;
            return sources.FirstOrDefault(s => s.Resolution == preferred.Resolution && s.Extension == preferred.Extension)
                ?? sources.FirstOrDefault(s => s.Resolution == preferred.Resolution);
        }

        private static DataAudioSource? MatchAudio(List<DataAudioSource> sources, DataAudioSource? preferred)
        {
            if (preferred == null) return null;
            return sources.FirstOrDefault(s => s.Language == preferred.Language && s.Extension == preferred.Extension)
                ?? sources.FirstOrDefault(s => s.Language == preferred.Language);
        }
```

- [ ] **Step 3: Wire drag-drop and clipboard-paste-on-activate in `MainWindow.xaml.cs`**

```csharp
        private async void Window_Activated(object sender, EventArgs e)
        {
            await _viewModel.PasteClipboardUrlAsync();
        }

        private void JobList_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.Text) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private async void JobList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string[] lines = ((string)e.Data.GetData(DataFormats.Text))
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                await _viewModel.LoadMultipleAsync(lines);
            }
        }
```

(Change the `Window_Activated`/`JobList_Drop` signatures from `void` to `async void` — matches the WinForms app's own `async void` event-handler pattern for the same flows.)

- [ ] **Step 4: Verify it builds and exercise every batch path**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

Run: `dotnet run --project VideoDownloader.Wpf`
- Load a `.txt` file with 2+ URLs via the Load-file button (add one to `MainWindow.xaml` if missing — bind a `Button` to `LoadFileCommand`), confirm the confirmation dialog and batch download with auto-matched quality.
- Drag-and-drop multiple URLs onto the job list area, confirm the same batch flow triggers.
- Copy a single valid URL to the clipboard, switch to the app, confirm it auto-fills the URL box.
- Copy multiple URLs (multi-line) to the clipboard, switch to the app, confirm it triggers the batch flow.

- [ ] **Step 5: Add the missing Load-file button to `MainWindow.xaml`**

If not already present, add next to the Browse button in Row 1:

```xml
            <Button Content="{loc:Tr 'Load'}" Margin="8,0,0,0" Command="{Binding LoadFileCommand}" />
```

Add the Polish key in `VideoDownloader.Core/Localization.cs`'s `_pl` dictionary: `["Load"] = "Wczytaj",` (this string didn't exist as a translated label in WinForms — `btnLoad` there had no visible text set from `ApplyLocalization`; check `FrmMain.Designer.cs` for its actual `Text` and match it, or use a clear icon-style label like `"📄 Load"` / `"📄 Wczytaj"` for parity with the other icon-prefixed buttons).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Add batch download flow (load-file, drag-drop, clipboard paste) to WPF app"
```

---

### Task 16: yt-dlp self-update check on load

**Files:**
- Modify: `VideoDownloader.Wpf/ViewModels/MainViewModel.cs`
- Modify: `VideoDownloader.Wpf/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `YtDlpUpdater.UpdateAsync()` (Core, existing, unchanged).
- Produces: `MainViewModel.CheckForYtDlpUpdateAsync()`.

- [ ] **Step 1: Add the check to `MainViewModel`**

```csharp
        public async Task CheckForYtDlpUpdateAsync()
        {
            try
            {
                string result = await YtDlpUpdater.UpdateAsync();
                if (result.Length == 0) return;

                if (result.Contains("up to date", StringComparison.OrdinalIgnoreCase))
                    _notifications.Info($"yt-dlp: {result}");
                else
                    _notifications.Success($"yt-dlp: {result}");
            }
            catch (Exception ex)
            {
                _notifications.Warning($"{Core.Localization.T("Could not update yt-dlp:")} {Core.Errors.ParseErrorMessage(ex)}");
            }
        }
```

Add `using VideoDownloader.Core.Tools;` if not already present.

- [ ] **Step 2: Call it once on load**

In `MainWindow.xaml`, add `Loaded="Window_Loaded"` to the `Window` element.

In `MainWindow.xaml.cs`:

```csharp
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.CheckForYtDlpUpdateAsync();
        }
```

- [ ] **Step 3: Verify it builds and runs**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

Run: `dotnet run --project VideoDownloader.Wpf`
Expected: On load, a notification banner reports the yt-dlp update check result (info/success/warning depending on outcome).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add yt-dlp self-update check on WPF app load"
```

---

### Task 17: Language selector

**Files:**
- Modify: `VideoDownloader.Wpf/ViewModels/MainViewModel.cs`
- Modify: `VideoDownloader.Wpf/MainWindow.xaml`

**Interfaces:**
- Consumes: `AppLanguage`, `Localization.Language` (Core, existing, unchanged).
- Produces: `MainViewModel.Language` (`AppLanguage`), `MainViewModel.AvailableLanguages` (`AppLanguage[]`).

- [ ] **Step 1: Add the property to `MainViewModel`**

```csharp
        public AppLanguage[] AvailableLanguages { get; } = Enum.GetValues<AppLanguage>();

        public AppLanguage Language
        {
            get => Core.Localization.Language;
            set
            {
                if (Core.Localization.Language == value) return;
                Core.Localization.Language = value;
                OnPropertyChanged();
            }
        }
```

In the constructor, subscribe so every `{loc:Tr}`-bound string in the window (via the `TrExtension` from Task 10) and this `ComboBox` refresh together:

```csharp
            Core.Localization.LanguageChanged += (s, e) => UpdateStatusText();
```

(`TrExtension` already self-refreshes on `LanguageChanged`; this line only re-renders `StatusText`, which is plain view-model text, not XAML-markup-extension-driven.)

- [ ] **Step 2: Add the selector to `MainWindow.xaml`**

In Row 2 (next to the concurrency slider), add:

```xml
            <ComboBox ItemsSource="{Binding AvailableLanguages}" SelectedItem="{Binding Language}" Width="100" Margin="16,0,0,0" />
```

- [ ] **Step 3: Verify it builds and runs**

Run: `dotnet build VideoDownloader.sln`
Expected: Build succeeds.

Run: `dotnet run --project VideoDownloader.Wpf`
Switch the language dropdown between `Auto`/`English`/`Polish`, confirm every `{loc:Tr}`-bound label in the main window, About window, and source-chooser window updates immediately, and the choice persists across restarts (via the shared `Registry`).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add language selector to WPF app"
```

---

### Task 18: Final full regression pass + docs

**Files:**
- Modify: `CLAUDE.md` (document the new solution structure)

- [ ] **Step 1: Full solution build**

Run: `dotnet build VideoDownloader.sln -c Release`
Expected: Clean build across all four projects.

- [ ] **Step 2: Full manual pass, both apps**

Repeat Task 9's WinForms checklist once more (confirm nothing drifted across Tasks 10–17, which shouldn't have touched WinForms code at all), then run the same checklist against `VideoDownloader.Wpf`:
- Golden path: paste URL → fetch sources → pick quality → download completes, file lands in destination.
- Invalid URL surfaces an error notification.
- Cancel mid-download marks the job Canceled.
- Batch via clipboard multi-paste, drag-drop, and Load-file all work with auto-matched quality after the first pick.
- Concurrency slider changes mid-queue start/hold jobs correctly.
- PL ⇄ EN switch updates every visible string, including in the About and source-chooser windows.
- yt-dlp self-update check runs on load and reports via notification.
- About dialog shows correct version/build date/links.

- [ ] **Step 3: Update `CLAUDE.md`**

In the "## Solution structure" section, update the bullet list to add:

```markdown
- **VideoDownloader.Core** (`net8.0`) — shared, UI-framework-agnostic class library: download/queue/notification logic (`Downloader`, `DownloadJob`, `DownloadQueueManager`, `Notifications`), localization, registry persistence, error parsing. Referenced by both `VideoDownloader` and `VideoDownloader.Wpf`.
- **VideoDownloader.Wpf** (`net8.0-windows`) — WPF front end (MVVM via CommunityToolkit.Mvvm), functionally equivalent to `VideoDownloader`. No installer yet — `VideoDownloader.Setup` still packages only the WinForms app.
```

Update the "## Common commands" section to add:

```markdown
# Run the WPF app
dotnet run --project VideoDownloader.Wpf
```

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md
git commit -m "Document VideoDownloader.Core and VideoDownloader.Wpf in CLAUDE.md"
```

- [ ] **Step 5: Report to the user**

Summarize what was built, what was verified manually and how, and explicitly flag anything from the spec's Verification plan that could not be exercised (if any) rather than claiming full coverage.
