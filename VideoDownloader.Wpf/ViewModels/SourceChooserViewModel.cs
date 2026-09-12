using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoDownloader.Core;

namespace VideoDownloader.Wpf.ViewModels
{
    public partial class SourceChooserViewModel : ObservableObject
    {
        private readonly Downloader _downloader;

        public ObservableCollection<DataVideoSource> VideoSources { get; } = new();
        public ObservableCollection<DataAudioSource> AudioSources { get; } = new();

        [ObservableProperty]
        private DataVideoSource? _selectedVideo;

        [ObservableProperty]
        private DataAudioSource? _selectedAudio;

        [ObservableProperty]
        private bool _canDownload;

        public string VideoTitle { get; private set; } = string.Empty;
        public float Duration { get; private set; }

        public bool? DialogResult { get; private set; }

        public event EventHandler? RequestClose;

        public SourceChooserViewModel(Downloader downloader)
        {
            _downloader = downloader;
        }

        public async Task LoadAsync()
        {
            var sources = await _downloader.FetchSources();
            if (sources == null)
                throw new Exception(Core.Localization.T("Failed to retrieve sources."));

            VideoTitle = sources.VideoTitle;
            Duration = sources.Duration;

            foreach (var v in sources.VideoSources) VideoSources.Add(v);
            foreach (var a in sources.AudioSources) AudioSources.Add(a);

            SelectedVideo = VideoSources.LastOrDefault();
            SelectedAudio = AudioSources.FirstOrDefault();
            CanDownload = VideoSources.Count > 0 && AudioSources.Count > 0;
        }

        [RelayCommand]
        private void Download()
        {
            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
