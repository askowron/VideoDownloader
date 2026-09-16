using System.Windows;
using VideoDownloader.Wpf.Theming;

namespace VideoDownloader.Wpf
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeManager.Initialize();
            base.OnStartup(e);
        }
    }
}
