using System;
using System.Collections.Generic;
using System.Linq;

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

        protected Panel _NotificationsContainer;

        public Notifications(Panel notificationsContainer)
        {
            _NotificationsContainer = notificationsContainer;
        }

        public void AddMessage(NotificationType notificationType, string message)
        {
            if (_NotificationsContainer != null)
            {
                _NotificationsContainer.Controls.Add(new NotificationControl(notificationType, message));
            }
        }

        public void Info(string message)
        {
            AddMessage(NotificationType.Info, message);
        }

        public void Success(string message)
        {
            AddMessage(NotificationType.Success, message);
        }

        public void Warning(string message)
        {
            AddMessage(NotificationType.Warning, message);
        }
        public void Error(string message)
        {
            AddMessage(NotificationType.Error, message);
        }

        public void ClearMessages()
        {
            if (_NotificationsContainer != null)
            {
                foreach (Control c in _NotificationsContainer.Controls.OfType<Control>().ToArray())
                    c.Dispose();
            }
        }

        internal void ClearOldNotifications() => ClearMessages();
    }
}
