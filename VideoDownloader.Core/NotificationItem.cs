namespace VideoDownloader.Core
{
    public class NotificationItem
    {
        public required Notifications.NotificationType Type { get; init; }
        public required string Message { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.Now;
    }
}
