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
