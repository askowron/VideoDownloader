namespace VideoDownloader.Core
{
    public partial class NotificationControl : UserControl
    {
        private const int AutoDismissMilliseconds = 10000;

        public NotificationControl()
        {
            InitializeComponent();

            var autoDismissTimer = new System.Windows.Forms.Timer { Interval = AutoDismissMilliseconds };
            autoDismissTimer.Tick += (s, e) =>
            {
                autoDismissTimer.Stop();
                Dispose();
            };
            Disposed += (s, e) => autoDismissTimer.Dispose();
            autoDismissTimer.Start();
        }

        public NotificationControl(Notifications.NotificationType notificationType, string message) : this()
        {
            lMessage.Text = message;
            switch (notificationType)
            {
                case Notifications.NotificationType.Info:
                    this.BackColor = Color.LightBlue;
                    break;
                case Notifications.NotificationType.Warning:
                    this.BackColor = Color.Khaki;
                    break;
                case Notifications.NotificationType.Error:
                    this.BackColor = Color.LightCoral;
                    break;
                case Notifications.NotificationType.Success:
                    this.BackColor = Color.LightGreen;
                    break;
                default:
                    this.BackColor = SystemColors.Control;
                    break;
            }
        }
    }
}
