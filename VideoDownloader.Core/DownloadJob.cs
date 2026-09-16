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
