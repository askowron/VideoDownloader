namespace VideoDownloader.Core.History
{
    /// <summary>Represents one persisted download attempt, successful or not.</summary>
    public class DownloadHistoryEntry
    {
        /// <summary>Database row id. Null for an entry not yet persisted.</summary>
        public long? Id { get; set; }
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
