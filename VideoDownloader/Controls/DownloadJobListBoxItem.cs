using VideoDownloader.Core;

namespace VideoDownloader.Controls
{
    public partial class DownloadJobListBoxItem : UserControl
    {
        public DownloadJob Job { get; }

        public DownloadJobListBoxItem(DownloadJob job)
        {
            InitializeComponent();
            Job = job;
            Job.PropertyChanged += Job_PropertyChanged;
            RenderAll();
        }

        private void Job_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => Job_PropertyChanged(sender, e));
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(DownloadJob.Title): lTitle.Text = Job.Title; break;
                case nameof(DownloadJob.Url): lUrl.Text = Job.Url; break;
                case nameof(DownloadJob.Resolution): lResolution.Text = Localization.T("Resolution:") + " " + Job.Resolution; break;
                case nameof(DownloadJob.Format): lFormat.Text = Localization.T("Format:") + " " + Job.Format; break;
                case nameof(DownloadJob.Duration): lDuration.Text = Localization.T("Duration:") + " " + Job.Duration; break;
                case nameof(DownloadJob.FileSize): lSize.Text = Localization.T("Size (MB):") + " " + Job.FileSize; break;
                case nameof(DownloadJob.Speed): progressBar.Speed = Job.Speed; progressBar.Invalidate(); break;
                case nameof(DownloadJob.ETA): progressBar.ETA = Job.ETA; progressBar.Invalidate(); break;
                case nameof(DownloadJob.ProgressPercentage): progressBar.PreciseValue = Job.ProgressPercentage; break;
                case nameof(DownloadJob.State): RenderState(); break;
            }
        }

        private void RenderAll()
        {
            lTitle.Text = Job.Title;
            lUrl.Text = Job.Url;
            lResolution.Text = Localization.T("Resolution:") + " " + Job.Resolution;
            lFormat.Text = Localization.T("Format:") + " " + Job.Format;
            lDuration.Text = Localization.T("Duration:") + " " + Job.Duration;
            lSize.Text = Localization.T("Size (MB):") + " " + Job.FileSize;
            progressBar.Speed = Job.Speed;
            progressBar.ETA = Job.ETA;
            progressBar.PreciseValue = Job.ProgressPercentage;
            RenderState();
        }

        private void RenderState()
        {
            switch (Job.State)
            {
                case DownloadingState.Waiting:
                    progressBar.BackColor = Color.LightYellow;
                    break;
                case DownloadingState.Downloading:
                    progressBar.BackColor = progressBar.BaseBackColor;
                    break;
                case DownloadingState.Completed:
                    progressBar.BackColor = Color.LightSkyBlue;
                    btnCancel.Visible = false;
                    break;
                case DownloadingState.Failed:
                    progressBar.BackColor = Color.OrangeRed;
                    break;
                case DownloadingState.Canceled:
                    progressBar.BackColor = Color.LightSlateGray;
                    break;
            }
            progressBar.Invalidate();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (Job.State == DownloadingState.Downloading && MessageBox.Show(Localization.T("Are you sure you want to cancel downloading?"), Localization.T("Downloading"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Job.CancellationTokenSource?.Cancel();
                Job.State = DownloadingState.Canceled;
            }
        }
    }
}
