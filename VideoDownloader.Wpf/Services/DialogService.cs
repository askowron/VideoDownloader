using System.Collections.Generic;
using System.Linq;
using VideoDownloader.Core;
using VideoDownloader.Wpf.ViewModels;
using VideoDownloader.Wpf.Views;

namespace VideoDownloader.Wpf.Services
{
    public class DialogService : IDialogService
    {
        public void ShowAbout()
        {
            var window = new AboutWindow { DataContext = new AboutViewModel() };
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        public async Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ShowSourceChooser(Downloader downloader)
        {
            var viewModel = new SourceChooserViewModel(downloader);
            await viewModel.LoadAsync();

            var window = new SourceChooserWindow(viewModel);
            window.Owner = System.Windows.Application.Current.MainWindow;
            bool? result = window.ShowDialog();

            if (result != true || viewModel.SelectedVideo == null || viewModel.SelectedAudio == null)
                return null;

            return (viewModel.SelectedVideo, viewModel.SelectedAudio, viewModel.VideoTitle, viewModel.Duration);
        }

        public async Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ChooseBestQuality(Downloader downloader, (DataVideoSource video, DataAudioSource audio)? preferred)
        {
            var sources = await downloader.FetchSources();
            if (sources == null || sources.VideoSources.Count == 0 || sources.AudioSources.Count == 0)
                throw new Exception(Core.Localization.T("No downloadable video/audio formats found."));

            var video = MatchVideo(sources.VideoSources, preferred?.video) ?? sources.VideoSources.LastOrDefault();
            var audio = MatchAudio(sources.AudioSources, preferred?.audio) ?? sources.AudioSources.FirstOrDefault();

            if (video == null || audio == null)
                return null;

            return (video, audio, sources.VideoTitle, sources.Duration);
        }

        private static DataVideoSource? MatchVideo(List<DataVideoSource> sources, DataVideoSource? preferred)
        {
            if (preferred == null) return null;
            return sources.FirstOrDefault(s => s.Resolution == preferred.Resolution && s.Extension == preferred.Extension)
                ?? sources.FirstOrDefault(s => s.Resolution == preferred.Resolution);
        }

        private static DataAudioSource? MatchAudio(List<DataAudioSource> sources, DataAudioSource? preferred)
        {
            if (preferred == null) return null;
            return sources.FirstOrDefault(s => s.Language == preferred.Language && s.Extension == preferred.Extension)
                ?? sources.FirstOrDefault(s => s.Language == preferred.Language);
        }
    }
}
