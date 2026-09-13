using VideoDownloader.Core;

namespace VideoDownloader
{
    public partial class FrmSourceChooser : Form
    {
        private Downloader _downloader;
        public string VideoTitle { get; set; }
        public float Duration { get; set; }

        public FrmSourceChooser()
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;
        }

        public FrmSourceChooser(Downloader downloader) : this()
        {
            _downloader = downloader;
        }

        private async void FrmSourceChooser_Load(object sender, EventArgs e)
        {
            Text = Localization.T("Select sources");
            label1.Text = Localization.T("Video:");
            label2.Text = Localization.T("Audio:");
            btnDownload.Text = Localization.T("Download");
            btnCancel.Text = Localization.T("Cancel");

            try
            {
                if (_downloader != null)
                {
                    Cursor = Cursors.WaitCursor;

                    var sources = await _downloader.FetchSources();

                    if (sources == null)
                    {
                        MessageBox.Show(Localization.T("Failed to retrieve sources."), Localization.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        DialogResult = DialogResult.Cancel;
                        return;
                    }

                    VideoTitle = sources.VideoTitle;
                    Duration = sources.Duration;

                    if (sources.VideoSources.Count > 0)
                    {
                        cbVideo.Items.AddRange(sources.VideoSources.ToArray());
                        cbVideo.SelectedIndex = cbVideo.Items.Count - 1;
                    }
                    if (sources.AudioSources.Count > 0)
                    {
                        cbAudio.Items.AddRange(sources.AudioSources.ToArray());
                        cbAudio.SelectedIndex = 0;
                    }

                    btnDownload.Enabled = sources.VideoSources.Count > 0 && sources.AudioSources.Count > 0;
                    Cursor = Cursors.Default;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Localization.T("An error occurred while loading sources:")} {ex.Message}", Localization.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Picks video/audio sources automatically. When <paramref name="preferredVideo"/>/<paramref name="preferredAudio"/>
        /// are given (the sources chosen for an earlier video in the same batch), tries to match their
        /// resolution/language first so a whole batch downloads at a consistent quality; falls back to the
        /// usual best-quality defaults when no match is found.
        /// </summary>
        public async Task<Tuple<DataVideoSource, DataAudioSource>> ChooseBestQuality(DataVideoSource preferredVideo = null, DataAudioSource preferredAudio = null)
        {
            if (_downloader == null)
                throw new Exception(Localization.T("No downloader assigned."));

            Cursor = Cursors.WaitCursor;
            try
            {
                var sources = await _downloader.FetchSources();
                if (sources == null || sources.VideoSources.Count == 0 || sources.AudioSources.Count == 0)
                    throw new Exception(Localization.T("No downloadable video/audio formats found."));

                VideoTitle = sources.VideoTitle;
                Duration = sources.Duration;

                var video = QualityMatcher.MatchVideo(sources.VideoSources, preferredVideo) ?? sources.VideoSources.LastOrDefault();
                var audio = QualityMatcher.MatchAudio(sources.AudioSources, preferredAudio) ?? sources.AudioSources.FirstOrDefault();

                return new Tuple<DataVideoSource, DataAudioSource>(video, audio);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        public (DataVideoSource, DataAudioSource) SelectedSource
        {
            get
            {
                DataVideoSource videoSource = cbVideo.SelectedItem as DataVideoSource;
                DataAudioSource audioSource = cbAudio.SelectedItem as DataAudioSource;
                return (videoSource, audioSource);
            }
        }
    }
}
