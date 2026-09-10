using System.Collections.Generic;
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

        private readonly List<System.Threading.Timer> _pendingTimers = new();

        public ObservableCollection<NotificationItem> Items { get; } = new();

        public void AddMessage(NotificationType notificationType, string message)
        {
            var item = new NotificationItem { Type = notificationType, Message = message };
            Items.Add(item);

            System.Threading.Timer? timer = null;
            timer = new System.Threading.Timer(_ =>
            {
                Items.Remove(item);
                lock (_pendingTimers)
                {
                    _pendingTimers.Remove(timer!);
                }
                timer!.Dispose();
            }, null, AutoDismissMilliseconds, Timeout.Infinite);

            lock (_pendingTimers)
            {
                _pendingTimers.Add(timer);
            }
        }

        public void Info(string message) => AddMessage(NotificationType.Info, message);
        public void Success(string message) => AddMessage(NotificationType.Success, message);
        public void Warning(string message) => AddMessage(NotificationType.Warning, message);
        public void Error(string message) => AddMessage(NotificationType.Error, message);

        public void ClearMessages() => Items.Clear();

        public void ClearOldNotifications() => ClearMessages();
    }
}
