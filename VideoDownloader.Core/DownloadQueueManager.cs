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

        // Canceled jobs are excluded here (but stay in Jobs) to match pre-migration behavior: a
        // canceled WinForms DownloadJobListBoxItem used to Dispose() itself, which removed it from
        // flpJobs.Controls and thus from the old Controls.Length-based total. Jobs itself keeps the
        // entry (rather than removing it) so a canceled job can still be rendered - e.g. a future
        // WPF job card showing a "Canceled" state - without corrupting the pending-queue bookkeeping.
        public int TotalCount => Jobs.Count(j => j.State != DownloadingState.Canceled);

        public void Enqueue(Downloader downloader, (DataVideoSource video, DataAudioSource audio) sources, DownloadJob job)
        {
            Jobs.Add(job);
            _pending.Enqueue(new PendingDownload { Downloader = downloader, Sources = sources, Job = job });
            TryStartQueuedDownloads();

            // TryStartQueuedDownloads() only raises CountsChanged when it actually starts a
            // download, which it won't if the concurrency limit is already saturated - but
            // TotalCount (Jobs.Count) still changed from the Add above, so listeners relying
            // solely on CountsChanged (e.g. WPF's MainViewModel.StatusText) would otherwise miss
            // this enqueue and undercount until some other job's state next changes. Always raise
            // it here so every consumer sees the new total immediately.
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
                pending.Downloader.Dispose();
                CountsChanged?.Invoke(this, EventArgs.Empty);
                TryStartQueuedDownloads();
            }
        }
    }
}
