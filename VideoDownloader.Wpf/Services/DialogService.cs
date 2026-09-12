using VideoDownloader.Wpf.ViewModels;
using VideoDownloader.Wpf.Views;

namespace VideoDownloader.Wpf.Services
{
    public class DialogService : IDialogService
    {
        public void ShowAbout()
        {
            var window = new AboutWindow { DataContext = new AboutViewModel() };
            window.ShowDialog();
        }
    }
}
