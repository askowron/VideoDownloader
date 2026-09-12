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
    }
}
