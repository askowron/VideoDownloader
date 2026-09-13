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

            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                await viewModel.LoadAsync();
            }
            finally
            {
                System.Windows.Input.Mouse.OverrideCursor = null;
            }

            var window = new SourceChooserWindow(viewModel);
            window.Owner = System.Windows.Application.Current.MainWindow;
            bool? result = window.ShowDialog();

            if (result != true || viewModel.SelectedVideo == null || viewModel.SelectedAudio == null)
                return null;

            return (viewModel.SelectedVideo, viewModel.SelectedAudio, viewModel.VideoTitle, viewModel.Duration);
        }

        public async Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ChooseBestQuality(Downloader downloader, (DataVideoSource video, DataAudioSource audio)? preferred)
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            DataSource sources;
            try
            {
                sources = await downloader.FetchSources();
            }
            finally
            {
                System.Windows.Input.Mouse.OverrideCursor = null;
            }

            if (sources == null || sources.VideoSources.Count == 0 || sources.AudioSources.Count == 0)
                throw new Exception(Core.Localization.T("No downloadable video/audio formats found."));

            var video = QualityMatcher.MatchVideo(sources.VideoSources, preferred?.video) ?? sources.VideoSources.LastOrDefault();
            var audio = QualityMatcher.MatchAudio(sources.AudioSources, preferred?.audio) ?? sources.AudioSources.FirstOrDefault();

            if (video == null || audio == null)
                return null;

            return (video, audio, sources.VideoTitle, sources.Duration);
        }
    }
}
