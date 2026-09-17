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

            /// <summary>Id of the "Waiting" row <see cref="Enqueue"/> already persisted for this
            /// job, resolved before the download itself starts. Awaited (not blocked on) so a
            /// slow-to-open history.db never delays starting the download.</summary>
            public required Task<long> HistoryIdTask { get; init; }
        }

        private readonly Queue<PendingDownload> _pending = new();
        private readonly HistoryRepository _history = new();
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

        // Canceled jobs are excluded here (but stay in Jobs) to match pre-migration behavior: a
        // canceled WinForms DownloadJobListBoxItem used to Dispose() itself, which removed it from
        // flpJobs.Controls and thus from the old Controls.Length-based total. Jobs itself keeps the
        // entry (rather than removing it) so a canceled job can still be rendered - e.g. a future
        // WPF job card showing a "Canceled" state - without corrupting the pending-queue bookkeeping.
        public int TotalCount => Jobs.Count(j => j.State != DownloadingState.Canceled);

        public void Enqueue(Downloader downloader, (DataVideoSource video, DataAudioSource audio) sources, DownloadJob job)
        {
            Jobs.Add(job);
            var historyIdTask = _history.AddAsync(new DownloadHistoryEntry
            {
                Url = job.Url,
                Title = job.Title,
                StartedAtUtc = DateTime.UtcNow,
                FinishedAtUtc = DateTime.UtcNow,
                DurationSeconds = 0,
                Status = job.State.ToString(),
                Resolution = job.Resolution,
                Format = job.Format
            });
            _pending.Enqueue(new PendingDownload { Downloader = downloader, Sources = sources, Job = job, HistoryIdTask = historyIdTask });
            TryStartQueuedDownloads();

            // TryStartQueuedDownloads() only raises CountsChanged when it actually starts a
            // download, which it won't if the concurrency limit is already saturated - but
            // TotalCount (Jobs.Count) still changed from the Add above, so listeners relying
            // solely on CountsChanged (e.g. WPF's MainViewModel.StatusText) would otherwise miss
            // this enqueue and undercount until some other job's state next changes. Always raise
            // it here so every consumer sees the new total immediately.
            CountsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Cancels a job that is still sitting in <see cref="_pending"/> (i.e. hasn't started
        /// downloading yet). Downloading jobs are canceled separately via their own
        /// <see cref="DownloadJob.CancellationTokenSource"/> - this only covers the queued case,
        /// which has no token source to cancel and would otherwise sit there forever.
        /// </summary>
        public bool CancelWaiting(DownloadJob job)
        {
            if (job.State != DownloadingState.Waiting) return false;

            PendingDownload? match = null;
            var remaining = new Queue<PendingDownload>(_pending.Count);
            foreach (var pending in _pending)
            {
                if (match == null && pending.Job == job)
                {
                    match = pending;
                    continue;
                }
                remaining.Enqueue(pending);
            }

            if (match == null) return false;

            _pending.Clear();
            foreach (var pending in remaining)
                _pending.Enqueue(pending);

            job.State = DownloadingState.Canceled;
            match.Downloader.Dispose();
            _ = SaveHistoryAsync(match);
            CountsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Removes a finished job's card from <see cref="Jobs"/> without touching the file it
        /// produced or its already-saved history entry - just decluttering the list.
        /// </summary>
        public void RemoveJob(DownloadJob job)
        {
            if (Jobs.Remove(job))
                CountsChanged?.Invoke(this, EventArgs.Empty);
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
                await SaveHistoryAsync(pending);
                pending.Downloader.Dispose();
                CountsChanged?.Invoke(this, EventArgs.Empty);
                TryStartQueuedDownloads();
            }
        }

        /// <summary>
        /// Updates the "Waiting" row <see cref="Enqueue"/> already persisted for this job into
        /// its final Completed/Failed/Canceled state. Average speed is computed from the final
        /// reported size over the wall-clock duration rather than trusting the last
        /// instantaneous <see cref="DownloadJob.Speed"/> sample, which can be stale or missing
        /// right at completion/cancellation.
        /// </summary>
        private async Task SaveHistoryAsync(PendingDownload pending)
        {
            var job = pending.Job;
            var finishedAtUtc = DateTime.UtcNow;
            var startedAtUtc = job.StartedAtUtc ?? finishedAtUtc;
            var durationSeconds = Math.Max(0, (finishedAtUtc - startedAtUtc).TotalSeconds);
            var fileSizeBytes = HistorySizeParser.ParseBytes(job.FileSize);

            long historyId = await pending.HistoryIdTask;
            await _history.UpdateAsync(historyId, new DownloadHistoryEntry
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
