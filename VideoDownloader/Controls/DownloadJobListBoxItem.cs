using VideoDownloader.Core;

namespace VideoDownloader.Controls
{
    public partial class DownloadJobListBoxItem : UserControl
    {
        public enum DownloadingState
        {
            Waiting,
            Downloading,
            Completed,
            Failed,
            Canceled
        }

        private CancellationTokenSource _cts;

        private string _downloadSpeed = string.Empty;
        private string _eta = string.Empty;
        private string _videoFormat = string.Empty;
        private DownloadingState _state;

        public DownloadingState State 
        { 
            get => _state;
            set 
            {
                _state = value;
                if(StateChanged != null)
                    StateChanged.Invoke(this, _state);
            } 
        }

        protected event EventHandler<DownloadingState> StateChanged;

        public DownloadJobListBoxItem()
        {
            InitializeComponent();
            State = DownloadingState.Waiting;
            StateChanged += DownloadJobListBoxItem_StateChanged;
        }

        private void DownloadJobListBoxItem_StateChanged(object? sender, DownloadingState newState)
        {
            switch(newState)
            {
                case DownloadingState.Waiting:
                    progressBar.BackColor = System.Drawing.Color.LightYellow;
                    break;
                case DownloadingState.Downloading:
                    progressBar.BackColor = progressBar.BaseBackColor;
                    break;
                case DownloadingState.Completed:
                    progressBar.BackColor = System.Drawing.Color.LightSkyBlue;
                    break;
                case DownloadingState.Failed:
                    progressBar.BackColor = System.Drawing.Color.OrangeRed;
                    break;
                case DownloadingState.Canceled:
                    progressBar.BackColor = System.Drawing.Color.LightSlateGray;
                    break;
            }

            progressBar.Invalidate();
        }

        public string VideoTitle
        {
            get => lTitle.Text;
            set => lTitle.Text = value;
        }

        public string VideoDuration
        {
            get => lDuration.Text;
            set => lDuration.Text = Localization.T("Duration:") + " " + value;
        }

        public string VideoResolution
        {
            get => lResolution.Text;
            set => lResolution.Text = Localization.T("Resolution:") + " " + value;
        }

        public string VideoFileSize
        {
            get => lSize.Text;
            set => lSize.Text = Localization.T("Size (MB):") + " " + value;
        }

        public string URL
        {
            get => lUrl.Text;
            set => lUrl.Text = value;
        }

        public string VideoFormat
        {
            get => _videoFormat;
            set
            {
                _videoFormat = value;
                lFormat.Text = Localization.T("Format:") + " " + value;
            }
        }

        public string DownloadSpeed
        {
            get => progressBar.Speed;
            set => progressBar.Speed = value;
        }

        public string ETA
        {
            get => progressBar.ETA;
            set => progressBar.ETA = value;
        }

        public double ProgressPercentage
        {
            get => progressBar.Value;
            set
            {
                progressBar.PreciseValue = value;
            }
        }

        public CancellationTokenSource CancellationToken
        {
            get => _cts;
            set => _cts = value;
        }

        internal void DownloadBegin()
        {
            _cts?.Dispose();
            CancellationToken = new CancellationTokenSource();
            State = DownloadingState.Downloading;
        }

        internal void DownloadEnd()
        {
            State = DownloadingState.Completed;
            btnCancel.Visible = false;
            progressBar.PreciseValue = 100.0;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (State == DownloadingState.Downloading && MessageBox.Show(Localization.T("Are you sure you want to cancel downloading?"), Localization.T("Downloading"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                CancellationToken.Cancel();
                State = DownloadingState.Canceled;
            }
        }
    }
}
