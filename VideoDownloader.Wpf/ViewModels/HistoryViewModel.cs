using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoDownloader.Core.History;
using VideoDownloader.Core.Tools;
using VideoDownloader.Wpf.Services;

namespace VideoDownloader.Wpf.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly HistoryRepository _history = new();
        private readonly IDialogService _dialogService;
        private readonly Func<string, Task> _onRedownload;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<HistoryRowViewModel> Entries { get; } = new();

        /// <summary>Filtered view over <see cref="Entries"/> bound by the window's ListView.</summary>
        public ICollectionView View { get; }

        public HistoryViewModel(IDialogService dialogService, Func<string, Task> onRedownload)
        {
            _dialogService = dialogService;
            _onRedownload = onRedownload;

            View = CollectionViewSource.GetDefaultView(Entries);
            View.Filter = FilterEntry;
        }

        public async Task LoadAsync()
        {
            Entries.Clear();
            var entries = await _history.GetAllAsync();
            foreach (var entry in entries)
                Entries.Add(new HistoryRowViewModel(entry));
        }

        partial void OnSearchTextChanged(string value) => View.Refresh();

        private bool FilterEntry(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var row = (HistoryRowViewModel)obj;
            return row.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   row.Url.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        [RelayCommand]
        private async Task Redownload(HistoryRowViewModel? row)
        {
            if (row == null) return;
            await _onRedownload(row.Url);
        }

        [RelayCommand]
        private void OpenInBrowser(HistoryRowViewModel? row)
        {
            if (row == null) return;
            Process.Start(new ProcessStartInfo { FileName = row.Url, UseShellExecute = true });
        }

        [RelayCommand]
        private async Task ClearHistory()
        {
            if (Entries.Count == 0) return;

            if (!_dialogService.Confirm(
                Core.Localization.T("Are you sure you want to clear the entire download history?"),
                Core.Localization.T("Clear history")))
                return;

            await _history.ClearAllAsync();
            Entries.Clear();
        }
    }

    /// <summary>Pre-formatted, display-ready view of one <see cref="DownloadHistoryEntry"/> row.</summary>
    public class HistoryRowViewModel
    {
        public HistoryRowViewModel(DownloadHistoryEntry entry)
        {
            Date = entry.StartedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            Title = entry.Title;
            Url = entry.Url;
            Duration = Time.FromSeconds((float)entry.DurationSeconds);
            Size = entry.FileSizeBytes.HasValue ? DataSize.FromBytes(entry.FileSizeBytes.Value) : "-";
            Speed = entry.AverageSpeedBytesPerSec.HasValue ? DataSize.FromBytes(entry.AverageSpeedBytesPerSec.Value) + "/s" : "-";
            Status = Core.Localization.T(entry.Status);
        }

        public string Date { get; }
        public string Title { get; }
        public string Url { get; }
        public string Duration { get; }
        public string Size { get; }
        public string Speed { get; }
        public string Status { get; }
    }
}
