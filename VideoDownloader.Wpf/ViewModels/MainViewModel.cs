using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoDownloader.Core;
using VideoDownloader.Core.Tools;
using VideoDownloader.Core.Windows;
using VideoDownloader.Wpf.Services;

namespace VideoDownloader.Wpf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly DownloadQueueManager _queue = new();
        private readonly Notifications _notifications = new();

        [ObservableProperty]
        private string _sourceUrl = string.Empty;

        [ObservableProperty]
        private string _destinationPath = string.Empty;

        [ObservableProperty]
        private int _maxConcurrentDownloads = 3;

        [ObservableProperty]
        private string _statusText = string.Empty;

        public ObservableCollection<DownloadJob> Jobs => _queue.Jobs;
        public ObservableCollection<NotificationItem> Notifications => _notifications.Items;

        public string MaxConcurrentLabel => string.Format(Core.Localization.T("Simultaneous downloads: {0}"), MaxConcurrentDownloads);

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            DestinationPath = Registry.GetLastDestinationPath();
            MaxConcurrentDownloads = Math.Clamp(Registry.GetMaxConcurrentDownloads(), 1, 20);
            _queue.MaxConcurrentDownloads = MaxConcurrentDownloads;

            _queue.CountsChanged += (s, e) => System.Windows.Application.Current.Dispatcher.Invoke(UpdateStatusText);
            UpdateStatusText();

            // Notifications.AddMessage's auto-dismiss timer removes items from a thread-pool
            // thread (see Notifications.cs / plan note on ObservableCollection thread affinity).
            // WinForms marshals this in its CollectionChanged subscriber via Control.Invoke;
            // this is the WPF-side equivalent so the bound ItemsControl doesn't throw
            // NotSupportedException when a notification auto-dismisses off the UI thread.
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_notifications.Items, new object());
        }

        partial void OnMaxConcurrentDownloadsChanged(int value)
        {
            Registry.SetMaxConcurrentDownloads(value);
            _queue.MaxConcurrentDownloads = value;
            OnPropertyChanged(nameof(MaxConcurrentLabel));
        }

        private void UpdateStatusText()
        {
            StatusText = _queue.TotalCount > 0
                ? string.Format(Core.Localization.T("Jobs: {0} | Active: {1} | Completed: {2}"), _queue.TotalCount, _queue.ActiveCount, _queue.CompletedCount)
                : string.Format(Core.Localization.T("Jobs: {0} | Completed: {0}"), _queue.TotalCount);
        }

        [RelayCommand]
        private void Browse()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Destination Folder",
                AutoUpgradeEnabled = true,
                ShowNewFolderButton = true,
                SelectedPath = Directory.Exists(DestinationPath) ? DestinationPath : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DestinationPath = dialog.SelectedPath;
                Registry.SetLastDestinationPath(DestinationPath);
            }
        }

        [RelayCommand]
        private void OpenFolder()
        {
            if (DestinationPath.Length > 0 && Path.Exists(DestinationPath))
            {
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{DestinationPath}\"", UseShellExecute = true });
            }
        }

        [RelayCommand]
        private void About() => _dialogService.ShowAbout();

        [RelayCommand]
        private void BuyCoffee() =>
            Process.Start(new ProcessStartInfo { FileName = "https://buycoffee.to/rico", UseShellExecute = true });

        [RelayCommand]
        private void CancelJob(DownloadJob? job)
        {
            if (job == null) return;

            if (job.State == DownloadingState.Downloading &&
                System.Windows.MessageBox.Show(Core.Localization.T("Are you sure you want to cancel downloading?"), Core.Localization.T("Downloading"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // MessageBox.Show is modal but still pumps the dispatcher, so the download can
                // finish (and dispose its CancellationTokenSource) while the confirmation is up.
                // Re-check the state before touching the possibly-disposed token source.
                if (job.State == DownloadingState.Downloading)
                {
                    job.CancellationTokenSource?.Cancel();
                    job.State = DownloadingState.Canceled;
                }
            }
        }

        [RelayCommand]
        private async Task Download()
        {
            _notifications.ClearOldNotifications();

            if (DestinationPath.Length == 0 || !Directory.Exists(DestinationPath))
            {
                _notifications.Warning(Core.Localization.T("Please select a valid destination path before downloading."));
                return;
            }

            var downloader = new Downloader(_notifications)
            {
                SourceURL = SourceUrl,
                DestinationPath = DestinationPath
            };

            try
            {
                var picked = await _dialogService.ShowSourceChooser(downloader);
                if (picked == null)
                {
                    downloader.Dispose();
                    return;
                }

                var job = new DownloadJob
                {
                    Title = picked.Value.videoTitle,
                    Url = downloader.SourceURL,
                    Resolution = picked.Value.video.Resolution,
                    Format = picked.Value.video.Extension,
                    Duration = Time.FromSeconds(picked.Value.duration)
                };

                _queue.Enqueue(downloader, (picked.Value.video, picked.Value.audio), job);
                SourceUrl = string.Empty;
            }
            catch (Exception ex)
            {
                _notifications.Error(Core.Errors.ParseErrorMessage(ex));
                downloader.Dispose();
            }
        }

        internal DownloadQueueManager Queue => _queue;
        internal Notifications NotificationsService => _notifications;
    }
}
