using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VideoDownloader.Wpf.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public string Version => $"{Core.Localization.T("Version:")} {GetVersion()}";
        public string BuildDate => $"{Core.Localization.T("Build date:")} {GetBuildDate():yyyy-MM-dd HH:mm}";
        public string Author => $"{Core.Localization.T("Author:")} Adam Skowroński";

        private static string GetVersion() =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        private static DateTime GetBuildDate() =>
            File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);

        [RelayCommand]
        private void OpenEmail() => OpenLink("mailto:info@appit.pl");

        [RelayCommand]
        private void OpenFFmpeg() => OpenLink("https://ffmpeg.org");

        [RelayCommand]
        private void OpenYtDlp() => OpenLink("https://github.com/yt-dlp/yt-dlp");

        private static void OpenLink(string url) =>
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
}
