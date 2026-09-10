using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using VideoDownloader.Controls;
using VideoDownloader.Core;
using VideoDownloader.Core.Tools;
using VideoDownloader.Core.Windows;

namespace VideoDownloader
{
    public partial class FrmMain : Form
    {
        private Notifications _Notifications;

        private const int SB_HORZ = 0;

        private sealed class PendingDownload
        {
            public required Downloader Downloader { get; init; }
            public required (DataVideoSource video, DataAudioSource audio) Sources { get; init; }
            public required DownloadJobListBoxItem Job { get; init; }
        }

        private readonly Queue<PendingDownload> _downloadQueue = new();

        private int MaxConcurrentDownloads => tbMaxConcurrent.Value;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        #region Jobs Information
        private DownloadJobListBoxItem[] DownloadJobListBoxItems => flpJobs.Controls.Cast<DownloadJobListBoxItem>().ToArray();
        protected int DownloadJobActiveCount => DownloadJobListBoxItems.Count(item => item.State == DownloadJobListBoxItem.DownloadingState.Downloading);
        protected int DownloadJobCompletedCount => DownloadJobListBoxItems.Count(item => item.State == DownloadJobListBoxItem.DownloadingState.Completed);
        protected int DownloadJobCount => DownloadJobListBoxItems.Length;
        #endregion
        #region Events
        protected event EventHandler DownloadJobCountChanged;
        #endregion
        
        public FrmMain()
        {
            InitializeComponent();

            _Notifications = new Notifications();
            _Notifications.Items.CollectionChanged += Notifications_CollectionChanged;

            DownloadJobCountChanged += FrmMain_DownloadJobCountChanged;

            tbMaxConcurrent.Value = Math.Clamp(Registry.GetMaxConcurrentDownloads(), tbMaxConcurrent.Minimum, tbMaxConcurrent.Maximum);

            cbLanguage.SelectedIndex = (int)Localization.Language;
            Localization.LanguageChanged += (s, e) => ApplyLocalization();
            ApplyLocalization();
        }

        /// <summary>
        /// Re-applies the current language to every static piece of UI text on this form.
        /// Called on startup and whenever the language selector changes.
        /// </summary>
        private void ApplyLocalization()
        {
            label1.Text = Localization.T("Source (link)");
            label2.Text = Localization.T("Destination (path)");
            btnDownload.Text = Localization.T("Download");
            btnBrowse.Text = Localization.T("Browse");
            lBuyMeCoffee.Text = Localization.T("☕ Buy me a coffee");
            lAbout.Text = Localization.T("ℹ️ About");

            UpdateMaxConcurrentLabel();
            FrmMain_DownloadJobCountChanged(this, EventArgs.Empty);
        }

        private void cbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            Localization.Language = (AppLanguage)cbLanguage.SelectedIndex;
        }

        #region Download Queue

        /// <summary>
        /// Adds a job to the visible job list and to the pending-download queue, then attempts to start it
        /// straight away if the simultaneous-download limit allows it.
        /// </summary>
        private void EnqueueDownload(Downloader downloader, (DataVideoSource video, DataAudioSource audio) sources, DownloadJobListBoxItem job)
        {
            flpJobs.Controls.Add(job);
            _downloadQueue.Enqueue(new PendingDownload { Downloader = downloader, Sources = sources, Job = job });
            TryStartQueuedDownloads();
        }

        /// <summary>
        /// Starts as many queued downloads as the current simultaneous-download limit allows.
        /// Called whenever the limit changes and whenever a download finishes, so raising the slider
        /// immediately wakes up the queue and lowering it simply stops new downloads from starting.
        /// </summary>
        private void TryStartQueuedDownloads()
        {
            bool started = false;
            while (DownloadJobActiveCount < MaxConcurrentDownloads && _downloadQueue.Count > 0)
            {
                var pending = _downloadQueue.Dequeue();
                _ = RunQueuedDownloadAsync(pending);
                started = true;
            }

            if (started)
                FrmMain_DownloadJobCountChanged(this, EventArgs.Empty);
        }

        private async Task RunQueuedDownloadAsync(PendingDownload pending)
        {
            try
            {
                await pending.Downloader.Download(pending.Sources, pending.Job);
            }
            finally
            {
                pending.Downloader.Dispose();
                FrmMain_DownloadJobCountChanged(this, EventArgs.Empty);
                TryStartQueuedDownloads();
            }
        }

        private void UpdateMaxConcurrentLabel()
        {
            lMaxConcurrent.Text = string.Format(Localization.T("Simultaneous downloads: {0}"), tbMaxConcurrent.Value);
        }

        private void tbMaxConcurrent_ValueChanged(object sender, EventArgs e)
        {
            UpdateMaxConcurrentLabel();
            Registry.SetMaxConcurrentDownloads(tbMaxConcurrent.Value);
            TryStartQueuedDownloads();
        }

        #endregion

        /// <summary>
        /// Checking if the destination path is valid.
        /// </summary>
        /// <returns></returns>
        private bool CheckDestination()
        {
            if (tbDestinationPath.Text.Length == 0 ||
               !Directory.Exists(tbDestinationPath.Text))
            {
                _Notifications.AddMessage(Notifications.NotificationType.Warning, Localization.T("Please select a valid destination path before downloading."));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Downloads the URL currently in <c>tbLink</c> as part of a batch. On the first call of a batch
        /// (<paramref name="interactive"/> = true) the source-chooser dialog is shown so the user can pick
        /// video/audio quality; the picked sources are returned so the caller can reuse them as
        /// <paramref name="preferredSources"/> for the rest of the batch, which then picks quality
        /// automatically without prompting again.
        /// </summary>
        private async Task<(DataVideoSource video, DataAudioSource audio)?> BatchDownload(bool interactive, (DataVideoSource video, DataAudioSource audio)? preferredSources)
        {
            _Notifications.ClearOldNotifications();

            if (!CheckDestination()) return null;

            Downloader downloader = new Downloader(_Notifications);
            try
            {
                downloader.SourceURL = tbLink.Text;
                downloader.DestinationPath = tbDestinationPath.Text;

                using (FrmSourceChooser scForm = new FrmSourceChooser(downloader))
                {
                    Cursor = Cursors.WaitCursor;

                    Tuple<DataVideoSource, DataAudioSource> sources;
                    if (interactive)
                    {
                        if (scForm.ShowDialog(this) != DialogResult.OK)
                            return null;

                        sources = new Tuple<DataVideoSource, DataAudioSource>(scForm.SelectedSource.Item1, scForm.SelectedSource.Item2);
                    }
                    else
                    {
                        sources = await scForm.ChooseBestQuality(preferredSources?.video, preferredSources?.audio);
                    }

                    if (sources != null)
                    {
                        DownloadJobListBoxItem item = new DownloadJobListBoxItem();
                        item.VideoTitle = scForm.VideoTitle;
                        item.URL = downloader.SourceURL;
                        item.VideoResolution = sources.Item1.Resolution;
                        item.VideoFormat = sources.Item1.Extension;
                        item.VideoDuration = Time.FromSeconds(scForm.Duration);

                        EnqueueDownload(downloader, (sources.Item1, sources.Item2), item);
                        downloader = null;
                        tbLink.Clear();

                        return (sources.Item1, sources.Item2);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                _Notifications.AddMessage(Notifications.NotificationType.Warning, Errors.ParseErrorMessage(ex));
                tbLink.Clear();
                return null;
            }
            finally
            {
                downloader?.Dispose();
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Handles the <c>Load</c> event of the main form, initializing form controls and loading persisted settings.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void FrmMain_Load(object sender, EventArgs e)
        {
            tbDestinationPath.Text = Registry.GetLastDestinationPath();

            _ = CheckForYtDlpUpdateAsync();
        }

        /// <summary>
        /// Runs yt-dlp's self-update on startup and reports the outcome via the notifications panel.
        /// Runs in the background so it never blocks the UI from loading.
        /// </summary>
        private async Task CheckForYtDlpUpdateAsync()
        {
            try
            {
                string result = await YtDlpUpdater.UpdateAsync();
                if (result.Length == 0)
                    return;

                if (result.Contains("up to date", StringComparison.OrdinalIgnoreCase))
                    _Notifications.Info($"yt-dlp: {result}");
                else
                    _Notifications.Success($"yt-dlp: {result}");
            }
            catch (Exception ex)
            {
                _Notifications.Warning($"{Localization.T("Could not update yt-dlp:")} {Errors.ParseErrorMessage(ex)}");
            }
        }
        private void FrmMain_Activated(object sender, EventArgs e)
        {
            PasteClipboardURL();
        }
        private void FrmMain_DownloadJobCountChanged(object? sender, EventArgs e)
        {
            if (DownloadJobCount > 0)
                lStatus.Text = string.Format(Localization.T("Jobs: {0} | Active: {1} | Completed: {2}"), DownloadJobCount, DownloadJobActiveCount, DownloadJobCompletedCount);
            else
                lStatus.Text = string.Format(Localization.T("Jobs: {0} | Completed: {0}"), DownloadJobCount);
        }

        #region Notifications Functions
        private void Notifications_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => Notifications_CollectionChanged(sender, e));
                return;
            }

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                foreach (Control c in flpNotifications.Controls.OfType<NotificationControl>().ToArray())
                    c.Dispose();
                return;
            }

            if (e.OldItems != null)
            {
                foreach (NotificationItem removed in e.OldItems)
                {
                    var control = flpNotifications.Controls.OfType<NotificationControl>()
                        .FirstOrDefault(c => c.Tag == removed);
                    control?.Dispose();
                }
            }

            if (e.NewItems != null)
            {
                foreach (NotificationItem added in e.NewItems)
                {
                    var control = new NotificationControl(added) { Tag = added };
                    flpNotifications.Controls.Add(control);
                }
            }
        }

        private void flpNotifications_ControlAdded(object sender, ControlEventArgs e)
        {
            int height = e.Control.Height + e.Control.Margin.Top + e.Control.Margin.Bottom;
            this.flpNotifications.Size = new Size(this.flpNotifications.Size.Width, this.flpNotifications.Size.Height + height);
        }

        private void flpNotifications_ControlRemoved(object sender, ControlEventArgs e)
        {
            int height = e.Control.Height + e.Control.Margin.Top + e.Control.Margin.Bottom;
            this.flpNotifications.Size = new Size(this.flpNotifications.Size.Width, this.flpNotifications.Size.Height - height);
        }
        #endregion
#region Downloading Jobs Functions
        private void flpJobs_ControlAdded(object sender, ControlEventArgs e)
        {
            DownloadJobCountChanged?.Invoke(this, EventArgs.Empty);
        }

        private void flpJobs_ControlRemoved(object sender, ControlEventArgs e)
        {
            DownloadJobCountChanged?.Invoke(this, EventArgs.Empty);
        }
        
        private void flp_Resize(object sender, EventArgs e)
        {
            FlowLayoutPanel container = sender as FlowLayoutPanel;
            if (container == null) return;

            foreach (Control control in container.Controls)
            {
                control.Width = container.ClientSize.Width
                                - control.Margin.Horizontal;
            }

            ShowScrollBar(container.Handle, SB_HORZ, false);
        }

        private void flpJobs_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.OemText) || e.Data.GetDataPresent(DataFormats.Text))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private async void flpJobs_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.OemText) || e.Data.GetDataPresent(DataFormats.Text))
            {
                string[] lines = (e.Data.GetData(DataFormats.OemText) ?? e.Data.GetData(DataFormats.Text)).ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                LoadMultipleItemsAsync(lines);
            }
        }
        #endregion

        #region Buttons Actions
        private async void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Text file (*.txt)|*.txt";
                ofd.CheckFileExists = true;
                ofd.Multiselect = false;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var lines = File.ReadAllLines(ofd.FileName);
                    LoadMultipleItemsAsync(lines);
                }
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            _Notifications.ClearOldNotifications();

            // Check destination path before proceeding
            if (!CheckDestination()) return;

            Downloader downloader = new Downloader(_Notifications);
            try
            {
                downloader.SourceURL = tbLink.Text;
                downloader.DestinationPath = tbDestinationPath.Text;

                using (FrmSourceChooser scForm = new FrmSourceChooser(downloader))
                {
                    if (scForm.ShowDialog() == DialogResult.OK)
                    {
                        Cursor = Cursors.WaitCursor;

                        DownloadJobListBoxItem item = new DownloadJobListBoxItem();
                        item.VideoTitle = scForm.VideoTitle;
                        item.URL = downloader.SourceURL;
                        item.VideoResolution = scForm.SelectedSource.Item1.Resolution;
                        item.VideoFormat = scForm.SelectedSource.Item1.Extension;
                        item.VideoDuration = Time.FromSeconds(scForm.Duration);

                        EnqueueDownload(downloader, scForm.SelectedSource, item);
                        downloader = null;
                        tbLink.Clear();

                        Cursor = Cursors.Default;
                    }
                }
            }
            finally
            {
                downloader?.Dispose();
            }
        }

        private void lBuyMeCoffee_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://buycoffee.to/rico",
                UseShellExecute = true
            });
        }

        private void lAbout_Click(object sender, EventArgs e)
        {
            using (FrmAbout frm = new FrmAbout())
            {
                frm.ShowDialog(this);
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (tbDestinationPath.Text.Length > 0 && Path.Exists(tbDestinationPath.Text))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{tbDestinationPath.Text}\"",
                    UseShellExecute = true
                });
            }
        }
        /// <summary>
        /// Handles the Click event of the Browse button to allow the user to select a destination folder.
        /// </summary>
        /// <remarks>Displays a folder browser dialog initialized to the current destination path if it
        /// exists, or to the user's Videos folder otherwise.  Updates the destination path text box with the folder
        /// selected by the user.</remarks>
        /// <param name="sender">The source of the event, typically the Browse button.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.AutoUpgradeEnabled = true;
                folderBrowserDialog.Description = "Select Destination Folder";

                string path = tbDestinationPath.Text;
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    tbDestinationPath.Text = folderBrowserDialog.SelectedPath;

                    Registry.SetLastDestinationPath(tbDestinationPath.Text);

                    _Notifications.ClearOldNotifications();
                }
            }
        }

        #endregion

        private async void LoadMultipleItemsAsync(string[] lines)
        {
            if (lines.Length < 2 || MessageBox.Show(
                string.Format(Localization.T("Found {0} urls! Are you sure you want to download them all?"), lines.Length),
                Localization.T("Download"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Ask for video/audio quality only for the first URL; reuse that choice for the rest of the batch.
                (DataVideoSource video, DataAudioSource audio)? preferredSources = null;
                bool isFirst = true;

                foreach (string line in lines)
                {
                    tbLink.Text = line;
                    var result = await BatchDownload(isFirst, preferredSources);

                    if (isFirst)
                    {
                        if (result == null)
                            break;

                        preferredSources = result;
                        isFirst = false;
                    }
                }
            }
        }

        private void tbLink_Enter(object sender, EventArgs e)
        {
            PasteClipboardURL();
        }

        private void PasteClipboardURL()
        {
            string ClipboardText = Clipboard.GetText();
            if (tbLink.Text.Length == 0 && !string.IsNullOrEmpty(ClipboardText))
            {
                if (Helper.URL.Verify(ClipboardText))
                    tbLink.Text = ClipboardText;
                else if (ClipboardText.Split('\n').Length > 1)
                {
                    var lines = ClipboardText.Split('\n').Where(line => Helper.URL.Verify(line)).ToArray();
                    LoadMultipleItemsAsync(lines);
                }
            }
        }
    }
}
